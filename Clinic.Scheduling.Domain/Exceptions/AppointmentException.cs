namespace Clinic.Scheduling.Domain.Exceptions;

public class AppointmentException : Exception
{
    public AppointmentException() : base("Appointment related error occured.")
    {
    }

    public AppointmentException(string message) : base(message)
    {
    }

    public AppointmentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}