public class LocationAppointment : Wrapper<Appointment>
{
    private LocationAppointment(Appointment appt) : base(appt)
    {
        if (appt.LocationId == Guid.Empty)
            throw new Exception("Appointment must have a valid LocationId");
        if (appt.BookedStart == DateTime.MinValue)
            throw new Exception($"{nameof(LocationAppointment)} must have a valid {nameof(Appointment.BookedStart)} date");
        if (appt.BookedEnd == DateTime.MinValue)
            throw new Exception($"{nameof(LocationAppointment)} must have a valid {nameof(Appointment.BookedEnd)} date");
    }

    public Guid LocationId => Entity.LocationId;
    public IReadOnlyCollection<SelectedAppointmentResource> SelectedAppointmentResources => Entity.SelectedAppointmentResources;
    public IReadOnlyCollection<SelectedAppointmentEmployee> SelectedAppointmentEmployees => Entity.SelectedAppointmentEmployees;
    public DateTime BookedStart => Entity.BookedStart;
    public DateTime BookedEnd => Entity.BookedEnd;

    public void SelectResource(MatchingResource matchingResource)
    {
        this.Entity.AddSelectedResource(matchingResource);
    }

    public static LocationAppointment From(Appointment appt) => new(appt);

    public ResourceAppointment ToResourceAppointment() 
        => ResourceAppointment.From(this); // TODO: See below

    public EmployeeAppointment ToEmployeeAppointment() 
        => EmployeeAppointment.From(this);
}
