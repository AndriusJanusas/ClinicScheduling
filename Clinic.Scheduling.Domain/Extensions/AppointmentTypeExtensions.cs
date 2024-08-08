using Clinic.Scheduling.Domain.Enums;

namespace Clinic.Scheduling.Domain.Extensions;

public static class AppointmentTypeExtensions
{
    public static int GetDuration(this AppointmentType type)
    {
        return (int)type;
    }
}