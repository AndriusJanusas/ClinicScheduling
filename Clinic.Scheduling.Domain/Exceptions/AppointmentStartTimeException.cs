namespace Clinic.Scheduling.Domain.Exceptions;

public class AppointmentStartTimeException : AppointmentException
{
    public AppointmentStartTimeException() : base("The requested appointment doesn't start on required time.")
    {
    }

    public AppointmentStartTimeException(string message) : base(message)
    {
    }

    public AppointmentStartTimeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}