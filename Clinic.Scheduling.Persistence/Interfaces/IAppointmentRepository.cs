using Clinic.Scheduling.Domain.Models;

namespace Clinic.Scheduling.Persistence.Interfaces;

public interface IAppointmentRepository
{
    Task<IReadOnlyCollection<Appointment>> GetScheduledAppointmentsByDate(DateTimeOffset date);
    Task<bool> SaveAppointment(Appointment appointment);
}