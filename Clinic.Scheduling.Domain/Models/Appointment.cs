using Clinic.Scheduling.Domain.Enums;
using Clinic.Scheduling.Domain.Extensions;

namespace Clinic.Scheduling.Domain.Models;

public class Appointment(DateTimeOffset start, AppointmentType type)
{
    public DateTimeOffset Start { get; } = start;
    public DateTimeOffset End { get; } = start.AddMinutes(type.GetDuration());
    public AppointmentType Type { get; } = type;
}