public class ParentResourceAppointment : Wrapper<Appointment>
{
    private ParentResourceAppointment(ResourceAppointment resourceAppt, Guid parentResourceId) : base(resourceAppt.Entity)
    {
        if (resourceAppt.SelectedAppointmentResources.All(r => r.Resource.Id != parentResourceId))
            throw new Exception($"At least one resource must have Id {parentResourceId}");
    }

    public IReadOnlyCollection<SelectedAppointmentResource> SelectedAppointmentResources => Entity.SelectedAppointmentResources;
    public DateTime BookedStart => Entity.BookedStart;
    public DateTime BookedEnd => Entity.BookedEnd;

    public static ParentResourceAppointment From(ResourceAppointment resourceAppt, Guid parentResourceId)
        => new ParentResourceAppointment(resourceAppt, parentResourceId);

    public ResourceAppointment AsResourceAppointment()
        => ResourceAppointment.From(LocationAppointment.From(Entity));
}
