public class EmployeeAppointment : Wrapper<Appointment>
{
    private EmployeeAppointment(LocationAppointment locationAppointment) : base(locationAppointment.Entity)
    {
        if (locationAppointment.SelectedAppointmentEmployees.Count == 0)
            throw new Exception(
                $"{nameof(EmployeeAppointment)} requires one or more {nameof(Appointment.SelectedAppointmentEmployees)}");
    }

    public DateTime BookedStart => Entity.BookedStart;
    public DateTime BookedEnd => Entity.BookedEnd;

    public IReadOnlyCollection<SelectedAppointmentEmployee> SelectedAppointmentEmployees 
        => Entity.SelectedAppointmentEmployees;

    public static EmployeeAppointment From(LocationAppointment locationAppointment) => new(locationAppointment);
}
