public class LocationAppointment : Wrapper<Appointment>
{
    private LocationAppointment(Appointment appt) : base(appt)
    {
        if (appt.LocationId == Guid.Empty)
            throw new Exception("Appointment must have a valid LocationId");
    }

    public Guid LocationId => Entity.LocationId;
    public IReadOnlyCollection<SelectedAppointmentResource> SelectedAppointmentResources => Entity.SelectedAppointmentResources;
    public IReadOnlyCollection<SelectedAppointmentEmployee> SelectedAppointmentEmployees => Entity.SelectedAppointmentEmployees;
    public DateTime BookedStart => Entity.BookedStart;
    public DateTime BookedEnd => Entity.BookedEnd;

    public static LocationAppointment From(Appointment appt) => new(appt);

    public ResourceAppointment ToResourceAppointment() 
        => ResourceAppointment.From(Entity); // TODO: See below

    public EmployeeAppointment ToEmployeeAppointment() 
        => EmployeeAppointment.From(Entity);
}
