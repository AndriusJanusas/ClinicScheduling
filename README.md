# Clinic Scheduling Application

## Overview
This project implements a core scheduling system for a physiotherapy clinic, adhering to specific business rules.

## Business Rules
- Clinic hours: 9am to 5pm
- Appointment types: 90 minutes (initial consultation), 60 minutes (standard), 30 minutes (check-in)
- No overlapping appointments
- Appointments start on the hour or half-hour
- Bookings start and end within clinic hours
- No bookings within 2 hours of appointment start time

## Project Structure
- `Clinic.Scheduling`: Core library with scheduling logic
- `Clinic.Scheduling.Domain`: Library with domain models, enums and exceptions
- `Clinic.Scheduling.Persistence`: Project is responsible for data access
- `Clinic.Scheduling.Test`: Unit tests using NUnit

## Getting Started
### Prerequisites
- .NET 8 SDK

### Setup
1. To build the solution, navigate to the root directory and run:
    ```
    dotnet restore
    dotnet build
    ```
2. To run the tests, navigate to the root directory and execute:
    ```
    dotnet test
    ```
   
## Assumptions
- Appointments are made in the same time zone as the clinic.
- Appointments can be scheduled back-to-back (e.g. `12:00-12:30` and `12:30-13:30`).
- If there is a wider available gap in the schedule, available appointments are calculated starting first available time slot. 
E.g. Given available time slot is `12:00-14:30` then available appointment times for a `Standard` type appointment will be `12:00, 13:00`.
And not `12:00, 12:30, 13:00, 13:30` (I've checked couple real clinics that use JaneApp, and it seems to have the same logic).
- The solution focuses on core scheduling logic without a UI or database integration.

## Implementation Details
- `AppointmentScheduler` class handles scheduling logic and manages appointments.
- `Appointment` class represents an appointment with a start time, end time, and type.
- Comprehensive unit tests ensure the correctness of the implementation.
- The application uses a configuration file (appsettings.json) for settings related to clinic working hours, appointment start times and booking lead time (deadline). Here is an example configuration:
```json
{
  "AppointmentSchedulerConfig": {
    "ClinicOpeningTime": "09:00",
    "ClinicClosingTime": "17:00",
    "AppointmentStartMinutes": [0, 30],
    "BookingLeadTime": "02:00:00"
  }
}
```

## Conclusion
This solution provides a robust and efficient scheduling system for the clinic, adhering to all specified business rules.