public class ResourceAppointment : Wrapper<Appointment>
{
    private ResourceAppointment(LocationAppointment locationAppointment) : base(locationAppointment.Entity)
    {
        if (locationAppointment.SelectedAppointmentResources.Count == 0)
            throw new Exception($"{nameof(ResourceAppointment)} requires one or more {nameof(SelectedAppointmentResource)}");
    }

    public IReadOnlyCollection<SelectedAppointmentResource> SelectedAppointmentResources => Entity.SelectedAppointmentResources;
    public DateTime BookedStart => Entity.BookedStart;
    public DateTime BookedEnd => Entity.BookedEnd;

    public static ResourceAppointment From(LocationAppointment locationAppointment) => new(locationAppointment);

    public Result<ParentResourceAppointment> AsParentResource(Guid parentResourceId)
        => ParentResourceAppointment.From(this, parentResourceId);

    public Result<ChildResourceAppointment> AsChildResource(Guid parentResourceId)
        => ChildResourceAppointment.From(this, parentResourceId);
}
