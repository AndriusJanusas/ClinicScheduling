using Clinic.Scheduling.Domain.Enums;
using Clinic.Scheduling.Domain.Models;

namespace Clinic.Scheduling.Interfaces;

public interface IAppointmentScheduler
{
    Task<IReadOnlyCollection<DateTimeOffset>> GetAvailableAppointmentTimes(DateTimeOffset date, AppointmentType type);
    Task<bool> BookAnAppointment(Appointment appointment);
    Task<IReadOnlyCollection<Appointment>> GetDailyAppointments(DateTimeOffset date);
}