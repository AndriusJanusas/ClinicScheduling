namespace Clinic.Scheduling;

public class AppointmentSchedulerConfig
{
    public TimeSpan ClinicOpeningTime { get; set; }
    public TimeSpan ClinicClosingTime { get; set; }
    public TimeSpan BookingLeadTime { get; set; }
    public required List<int> AppointmentStartMinutes { get; set; }
}