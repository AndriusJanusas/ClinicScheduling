namespace Clinic.Scheduling.Domain.Exceptions;

public class AppointmentDeadlineException : AppointmentException
{
    public AppointmentDeadlineException() : base("The requested appointment is past booking deadline.")
    {
    }

    public AppointmentDeadlineException(string message) : base(message)
    {
    }

    public AppointmentDeadlineException(string message, Exception innerException) : base(message, innerException)
    {
    }
}