public class ResourceAppointment : Wrapper<Appointment>
{
    private ResourceAppointment(Appointment appt) : base(appt)
    {
        if (appt.SelectedAppointmentResources.Count == 0)
            throw new Exception($"{nameof(ResourceAppointment)} requires one or more {nameof(SelectedAppointmentResource)}");
    }

    public IReadOnlyCollection<SelectedAppointmentResource> SelectedAppointmentResources => Entity.SelectedAppointmentResources;
    public DateTime BookedStart => Entity.BookedStart;
    public DateTime BookedEnd => Entity.BookedEnd;

    public static ResourceAppointment From(Appointment appt) => new(appt);

    public Result<ParentResourceAppointment> AsParentResource(Guid parentResourceId)
        => ParentResourceAppointment.From(this, parentResourceId);

    public Result<ChildResourceAppointment> AsChildResource(Guid parentResourceId)
        => ChildResourceAppointment.From(this, parentResourceId);
}
