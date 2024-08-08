namespace Clinic.Scheduling.Domain.Exceptions;

public class AppointmentOutsideWorkingHoursException : AppointmentException
{
    public AppointmentOutsideWorkingHoursException() : base(
        "The requested appointment is outside of clinic working hours.")
    {
    }

    public AppointmentOutsideWorkingHoursException(string message) : base(message)
    {
    }

    public AppointmentOutsideWorkingHoursException(string message, Exception innerException) : base(message,
        innerException)
    {
    }
}