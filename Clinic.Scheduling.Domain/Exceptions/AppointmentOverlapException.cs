namespace Clinic.Scheduling.Domain.Exceptions;

public class AppointmentOverlapException : AppointmentException
{
    public AppointmentOverlapException() : base(
        "The requested appointment overlaps with already scheduled appointment.")
    {
    }

    public AppointmentOverlapException(string message) : base(message)
    {
    }

    public AppointmentOverlapException(string message, Exception innerException) : base(message, innerException)
    {
    }
}