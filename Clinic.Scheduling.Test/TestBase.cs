using Clinic.Scheduling.Domain.Enums;
using Clinic.Scheduling.Domain.Models;

namespace Clinic.Scheduling.Test;

public class TestBase
{
    protected static readonly DateTimeOffset Today = DateTimeOffset.Now;
    protected static readonly DateTimeOffset Tomorrow = DateTimeOffset.Now.AddDays(1);

    protected static Appointment GetSingleAppointmentForToday()
    {
        return GetSingleAppointmentForDate(Today);
    }

    protected static Appointment GetSingleAppointmentForDate(DateTimeOffset date, int hour = 10, int minute = 0,
        AppointmentType type = AppointmentType.Standard)
    {
        return new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, hour, minute, 0, Today.Offset),
            type);
    }

    protected static List<Appointment> GetMultipleAppointmentsForToday()
    {
        return GetMultipleAppointmentsForDate(Today);
    }

    protected static List<Appointment> GetMultipleAppointmentsForDate(DateTimeOffset date)
    {
        return
        [
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 9, 30, 0, Today.Offset),
                AppointmentType.Standard),

            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 11, 30, 0, Today.Offset),
                AppointmentType.InitialConsultation),

            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 14, 0, 0, Today.Offset),
                AppointmentType.CheckIn),

            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 15, 0, 0, Today.Offset),
                AppointmentType.InitialConsultation)
        ];
    }
    
    protected static List<Appointment> GetScheduledAppointmentsForDate(DateTimeOffset date)
    {
        return
        [
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 9, 0, 0, date.Offset),
                AppointmentType.InitialConsultation),
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 11, 0, 0, date.Offset), AppointmentType.Standard),
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 14, 0, 0, date.Offset), AppointmentType.CheckIn)

        ];
    }

    protected static List<Appointment> GetFullyScheduledDayAppointmentsForDate(DateTimeOffset date)
    {
        return
        [
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 9, 0, 0, date.Offset),
                AppointmentType.Standard),
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 10, 0, 0, date.Offset),
                AppointmentType.Standard),
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 11, 0, 0, date.Offset),
                AppointmentType.Standard),
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 12, 0, 0, date.Offset),
                AppointmentType.Standard),
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 13, 0, 0, date.Offset),
                AppointmentType.Standard),
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 14, 0, 0, date.Offset),
                AppointmentType.Standard),
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 15, 0, 0, date.Offset),
                AppointmentType.Standard),
            new Appointment(new DateTimeOffset(date.Year, date.Month, date.Day, 16, 0, 0, date.Offset),
                AppointmentType.Standard)
        ];
    }
}