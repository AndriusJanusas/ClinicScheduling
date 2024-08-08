using Clinic.Scheduling.Domain.Enums;
using Clinic.Scheduling.Domain.Exceptions;
using Clinic.Scheduling.Domain.Models;
using Clinic.Scheduling.Persistence.Interfaces;
using Clinic.Scheduling.Interfaces;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Clinic.Scheduling.Test;

[TestFixture]
public class AppointmentSchedulerTests : TestBase
{
    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IAppointmentRepository>();
        _systemClockMock = new Mock<ISystemClock>();
        _loggerMock = new Mock<ILogger<AppointmentScheduler>>();
        _config = Options.Create(new AppointmentSchedulerConfig
        {
            ClinicOpeningTime = new TimeSpan(9, 0, 0),
            ClinicClosingTime = new TimeSpan(17, 0, 0),
            AppointmentStartMinutes = [0, 30],
            BookingLeadTime = TimeSpan.FromHours(2)
        });

        _scheduler =
            new AppointmentScheduler(_repositoryMock.Object, _systemClockMock.Object, _loggerMock.Object, _config);
    }

    private Mock<IAppointmentRepository> _repositoryMock;
    private Mock<ISystemClock> _systemClockMock;
    private Mock<ILogger<AppointmentScheduler>> _loggerMock;
    private IOptions<AppointmentSchedulerConfig> _config;
    private IAppointmentScheduler _scheduler;

    public static IEnumerable<TestCaseData> GetAvailableAppointmentTimes_TestCases()
    {
        yield return new TestCaseData(
            AppointmentType.InitialConsultation,
            new List<DateTimeOffset>
            {
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 12, 0, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 14, 30, 0, Tomorrow.Offset)
            }).SetArgDisplayNames(
            $"Appointment type: {AppointmentType.InitialConsultation}; available times: 12:00, 14:30");

        yield return new TestCaseData(
            AppointmentType.Standard,
            new List<DateTimeOffset>
            {
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 12, 0, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 13, 0, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 14, 30, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 15, 30, 0, Tomorrow.Offset)
            }).SetArgDisplayNames(
            $"Appointment type: {AppointmentType.Standard}; available times: 12:00, 13:00, 14:30, 15:30");

        yield return new TestCaseData(
            AppointmentType.CheckIn,
            new List<DateTimeOffset>
            {
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 10, 30, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 12, 0, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 12, 30, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 13, 0, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 13, 30, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 14, 30, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 15, 0, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 15, 30, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 16, 0, 0, Tomorrow.Offset),
                new(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day, 16, 30, 0, Tomorrow.Offset)
            }).SetArgDisplayNames(
            $"Appointment type: {AppointmentType.CheckIn}; available times: 10:30, 12:00, 12:30, 13:00, 13:30, 14:30, 15:00, 15:30, 16:00, 16:30");
    }

    [TestCaseSource(nameof(GetAvailableAppointmentTimes_TestCases))]
    public async Task GetAvailableAppointmentTimes_ShouldReturnAvailableSlots_WhenThereAreGapsBetweenAppointments(
        AppointmentType type, List<DateTimeOffset> expectedTimes)
    {
        var date = Tomorrow;

        // Available time slots between scheduled appointments: 10:30-11 (30 mins), 12:00-14:00 (120 mins), 14:30-17:00 (150 mins)   
        var scheduledAppointments = GetScheduledAppointmentsForDate(date);

        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(date)).ReturnsAsync(scheduledAppointments);

        var result = await _scheduler.GetAvailableAppointmentTimes(date, type);

        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Is.EquivalentTo(expectedTimes));
    }

    [TestCase(AppointmentType.InitialConsultation, 5)]
    [TestCase(AppointmentType.Standard, 8)]
    [TestCase(AppointmentType.CheckIn, 16)]
    public async Task GetAvailableAppointmentTimes_ShouldReturnAvailableSlots_WhenNoAppointmentsExist(
        AppointmentType type, int expectedAppointmentCount)
    {
        var date = Tomorrow;
        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(date)).ReturnsAsync(new List<Appointment>());

        var result = await _scheduler.GetAvailableAppointmentTimes(date, type);

        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Exactly(expectedAppointmentCount).Items);
    }

    [TestCase(AppointmentType.InitialConsultation)]
    [TestCase(AppointmentType.Standard)]
    [TestCase(AppointmentType.CheckIn)]
    public async Task GetAvailableAppointmentTimes_ShouldReturnEmptyList_WhenAllTimeSlotsAreTaken(AppointmentType type)
    {
        var date = Tomorrow;
        var scheduledAppointments = GetFullyScheduledDayAppointmentsForDate(date);

        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(date)).ReturnsAsync(scheduledAppointments);

        var result = await _scheduler.GetAvailableAppointmentTimes(date, type);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAvailableAppointmentTimes_ShouldExcludeTimeSlotsBeforeDeadline()
    {
        var date = new DateTimeOffset(Today.Year, Today.Month, Today.Day, 13, 15, 0, TimeSpan.FromHours(-7));
        _systemClockMock.Setup(clock => clock.UtcNow).Returns(date);

        // Available time slots between scheduled appointments: 10:30-11 (30 mins), 12:00-14:00 (120 mins), 14:30-17:00 (150 mins)   
        var scheduledAppointments = GetScheduledAppointmentsForDate(date);

        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(date)).ReturnsAsync(scheduledAppointments);

        var result = await _scheduler.GetAvailableAppointmentTimes(date, AppointmentType.Standard);

        var expectedTimes = new List<DateTimeOffset> { new(date.Year, date.Month, date.Day, 15, 30, 0, date.Offset) };

        Assert.That(result, Is.EquivalentTo(expectedTimes));
    }

    [TestCase(9, 0, AppointmentType.CheckIn, Description = "Fits at the start of the working day")]
    [TestCase(10, 30, AppointmentType.Standard, Description = "Fits between two appointments")]
    [TestCase(16, 30, AppointmentType.CheckIn, Description = "Fits at the end of the working day")]
    public async Task BookAnAppointment_ShouldSaveAppointment_WhenValid(int hour, int minute, AppointmentType type)
    {
        var appointmentToBook = GetSingleAppointmentForDate(Tomorrow, hour, minute, type);

        var existingAppointments = GetMultipleAppointmentsForDate(Tomorrow);

        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(appointmentToBook.Start))
            .ReturnsAsync(existingAppointments);
        _repositoryMock.Setup(r => r.SaveAppointment(appointmentToBook)).ReturnsAsync(true);

        var result = await _scheduler.BookAnAppointment(appointmentToBook);

        Assert.That(result, Is.True);
    }

    [Test]
    public void BookAnAppointment_ShouldThrowException_WhenAppointmentStartsAtInvalidTime()
    {
        var appointment = GetSingleAppointmentForDate(Tomorrow, minute: 15);

        var ex = Assert.ThrowsAsync<AppointmentStartTimeException>(() => _scheduler.BookAnAppointment(appointment));

        Assert.That(ex, Is.InstanceOf<AppointmentStartTimeException>());
    }

    [Test]
    public void BookAnAppointment_ShouldThrowException_WhenAppointmentIsOutsideWorkingHours()
    {
        var appointment = GetSingleAppointmentForDate(Tomorrow, 8);

        var ex = Assert.ThrowsAsync<AppointmentOutsideWorkingHoursException>(() =>
            _scheduler.BookAnAppointment(appointment));

        Assert.That(ex, Is.InstanceOf<AppointmentOutsideWorkingHoursException>());
    }

    [Test]
    public void BookAnAppointment_ShouldThrowException_WhenAppointmentIsPastBookingDeadline()
    {
        var date = new DateTimeOffset(Today.Year, Today.Month, Today.Day, 10, 0, 0, TimeSpan.FromHours(-7));
        _systemClockMock.Setup(clock => clock.UtcNow).Returns(date);

        var appointment = GetSingleAppointmentForDate(date, date.Hour + 1);

        var ex = Assert.ThrowsAsync<AppointmentDeadlineException>(() => _scheduler.BookAnAppointment(appointment));

        Assert.That(ex, Is.InstanceOf<AppointmentDeadlineException>());
    }

    [TestCase(9, 0, AppointmentType.Standard, Description = "Overlapping with first appointment of the day")]
    [TestCase(16, 0, AppointmentType.Standard, Description = "Overlapping with last appointment of the day")]
    [TestCase(15, 0, AppointmentType.InitialConsultation, Description = "Overlapping with two appointments")]
    public void BookAnAppointment_ShouldThrowException_WhenAppointmentOverlaps(int hour, int minute,
        AppointmentType type)
    {
        _systemClockMock.Setup(clock => clock.UtcNow).Returns(Today);

        var appointmentToBook = GetSingleAppointmentForDate(Tomorrow, hour, minute, type);
        var existingAppointments = GetMultipleAppointmentsForDate(Tomorrow);

        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(appointmentToBook.Start))
            .ReturnsAsync(existingAppointments);
        _repositoryMock.Setup(r => r.SaveAppointment(appointmentToBook)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<AppointmentOverlapException>(() => _scheduler.BookAnAppointment(appointmentToBook));

        Assert.That(ex, Is.InstanceOf<AppointmentOverlapException>());
    }

    [Test]
    public async Task GetDailyAppointments_ShouldReturnAppointments()
    {
        var date = Today;
        var appointments = new List<Appointment> { GetSingleAppointmentForToday() };

        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(date)).ReturnsAsync(appointments);

        var result = await _scheduler.GetDailyAppointments(date);

        Assert.That(result, Is.EquivalentTo(appointments));
    }

    [Test]
    public async Task GetDailyAppointments_ShouldReturnMultipleAppointments_WhenMultipleExist()
    {
        var date = Today;
        var appointments = GetMultipleAppointmentsForToday();

        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(date)).ReturnsAsync(appointments);

        var result = await _scheduler.GetDailyAppointments(date);

        Assert.That(result, Is.EquivalentTo(appointments));
    }

    [Test]
    public async Task GetDailyAppointments_ShouldReturnEmptyList_WhenNoAppointmentsExist()
    {
        var date = Today;
        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(date)).ReturnsAsync(new List<Appointment>());

        var result = await _scheduler.GetDailyAppointments(date);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetDailyAppointments_ShouldReturnCorrectAppointments_ForSpecificDate()
    {
        var todayAppointments = GetMultipleAppointmentsForToday();
        var tomorrowAppointments = GetMultipleAppointmentsForDate(Tomorrow);

        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(Today)).ReturnsAsync(todayAppointments);
        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(Tomorrow)).ReturnsAsync(tomorrowAppointments);

        var todayResult = await _scheduler.GetDailyAppointments(Today);
        var tomorrowResult = await _scheduler.GetDailyAppointments(Tomorrow);

        Assert.That(todayResult, Is.EquivalentTo(todayAppointments));
        Assert.That(tomorrowResult, Is.EquivalentTo(tomorrowAppointments));
    }

    [Test]
    public async Task GetDailyAppointments_ShouldHandleNoAppointmentsGracefully()
    {
        var date = Today;
        _repositoryMock.Setup(r => r.GetScheduledAppointmentsByDate(date)).ReturnsAsync((List<Appointment>)null!);

        var result = await _scheduler.GetDailyAppointments(date);

        Assert.That(result, Is.Empty);
    }
}