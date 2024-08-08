using Clinic.Scheduling.Domain.Enums;
using Clinic.Scheduling.Domain.Exceptions;
using Clinic.Scheduling.Domain.Extensions;
using Clinic.Scheduling.Domain.Models;
using Clinic.Scheduling.Persistence.Interfaces;
using Clinic.Scheduling.Interfaces;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clinic.Scheduling;

public class AppointmentScheduler(
    IAppointmentRepository repository,
    ISystemClock systemClock,
    ILogger<AppointmentScheduler> logger,
    IOptions<AppointmentSchedulerConfig> config)
    : IAppointmentScheduler
{
    public async Task<IReadOnlyCollection<DateTimeOffset>> GetAvailableAppointmentTimes(DateTimeOffset date,
        AppointmentType type)
    {
        var requestedAppointmentDuration = type.GetDuration();
        var availableAppointmentTimes = new List<DateTimeOffset>();
        var existingAppointments = (await repository.GetScheduledAppointmentsByDate(date)).ToList();

        // Dummy appointment at the end of clinic closing time so end of day time slots are returned correctly
        var closingTimeDummyAppointment = new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day,
            config.Value.ClinicClosingTime.Hours,
            config.Value.ClinicClosingTime.Minutes, 0, date.Offset), AppointmentType.Standard);
        existingAppointments.Add(closingTimeDummyAppointment);
        var existingOrderedAppointments = existingAppointments.OrderBy(a => a.Start);

        // Helper variable; starting with clinic opening time
        var previousEnd = new DateTimeOffset(date.Year, date.Month, date.Day, config.Value.ClinicOpeningTime.Hours,
            config.Value.ClinicOpeningTime.Minutes, 0, date.Offset);

        foreach (var appointment in existingOrderedAppointments)
        {
            var availableTimeGap = appointment.Start - previousEnd;

            if (availableTimeGap.TotalMinutes >= requestedAppointmentDuration)
            {
                // Check how many appointments can fit in available time gap
                var possibleAppointmentTimesInAvailableGap =
                    (int)(availableTimeGap.TotalMinutes / requestedAppointmentDuration);

                for (var i = possibleAppointmentTimesInAvailableGap - 1; i >= 0; i--)
                {
                    var availableStartTime = previousEnd.AddMinutes(requestedAppointmentDuration * i);
                    var earliestBookingTime = systemClock.UtcNow.Add(config.Value.BookingLeadTime);

                    // Check if the start time is not past booking deadline
                    if (availableStartTime >= earliestBookingTime)
                        availableAppointmentTimes.Add(availableStartTime);
                }
            }

            previousEnd = appointment.End;
        }

        logger.LogInformation(
            $"Found {availableAppointmentTimes.Count} appointment times for date {date.DateTime.ToShortDateString()}");

        return availableAppointmentTimes;
    }

    public async Task<bool> BookAnAppointment(Appointment appointment)
    {
        await ValidateAppointment(appointment);
        return await repository.SaveAppointment(appointment);
    }

    public async Task<IReadOnlyCollection<Appointment>> GetDailyAppointments(DateTimeOffset date)
    {
        var appointments = await repository.GetScheduledAppointmentsByDate(date);
        logger.LogInformation($"Found {appointments?.Count} appointments on {date.DateTime.ToShortDateString()}");
        return appointments ?? Array.Empty<Appointment>();
    }

    private async Task ValidateAppointment(Appointment appointment)
    {
        ValidateStartTime(appointment);
        ValidateClinicWorkingHours(appointment);
        ValidateBookingDeadline(appointment);
        await ValidateOverlappingAppointments(appointment);
    }

    private void ValidateStartTime(Appointment appointment)
    {
        if (config.Value.AppointmentStartMinutes.Contains(appointment.Start.Minute)) return;
        logger.LogWarning("The requested appointment doesn't start on required time.");
        throw new AppointmentStartTimeException();
    }

    private void ValidateClinicWorkingHours(Appointment appointment)
    {
        if (appointment.Start.TimeOfDay >= config.Value.ClinicOpeningTime &&
            appointment.End.TimeOfDay <= config.Value.ClinicClosingTime) return;
        logger.LogWarning("The requested appointment is outside of clinic working hours.");
        throw new AppointmentOutsideWorkingHoursException();
    }

    private void ValidateBookingDeadline(Appointment appointment)
    {
        var earliestBookingTime = systemClock.UtcNow.Add(config.Value.BookingLeadTime);
        if (appointment.Start >= earliestBookingTime) return;
        logger.LogWarning("The requested appointment is past booking deadline.");
        throw new AppointmentDeadlineException();
    }

    private async Task ValidateOverlappingAppointments(Appointment appointment)
    {
        var scheduledAppointments = await repository.GetScheduledAppointmentsByDate(appointment.Start.DateTime);
        if (scheduledAppointments.Any(scheduledAppointment => scheduledAppointment.Start < appointment.End &&
                                                              scheduledAppointment.End > appointment.Start))
        {
            logger.LogWarning("The requested appointment overlaps with already scheduled appointment.");
            throw new AppointmentOverlapException();
        }
    }
}