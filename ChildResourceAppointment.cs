public class ChildResourceAppointment : Wrapper<Appointment>
{
    private ChildResourceAppointment(ResourceAppointment resourceAppt, Guid parentResourceId) : base(resourceAppt.Entity)
    {
        if (parentResourceId == Guid.NewGuid())
            throw new Exception(
                $"Cannot create a {nameof(ChildResourceAppointment)} with a {parentResourceId} of {Guid.NewGuid()}");

        if (resourceAppt.SelectedAppointmentResources.Count == 0)
            throw new Exception(
                $"A {nameof(ChildResourceAppointment)} requires {nameof(resourceAppt.SelectedAppointmentResources)}");

        if (resourceAppt.SelectedAppointmentResources.Any(sar => sar.Resource == null))
            throw new Exception(
                $"A {nameof(ChildResourceAppointment)} requires all of the " +
                $"{nameof(resourceAppt.SelectedAppointmentResources)} to have a {nameof(Resource)}");
        
        if (resourceAppt.SelectedAppointmentResources
            .All(r => r.Resource.ParentResourceId != parentResourceId))
            throw new Exception(
                $"A {nameof(ChildResourceAppointment)} requires at least one of the " +
                $"{nameof(resourceAppt.SelectedAppointmentResources)} to have a {nameof(Resource.ParentResourceId)} " +
                $"of {parentResourceId}");
    }
    
    public DateTime BookedStart => Entity.BookedStart;
    public DateTime BookedEnd => Entity.BookedEnd;

    public IReadOnlyCollection<SelectedAppointmentResource> SelectedAppointmentResources 
        => this.Entity.SelectedAppointmentResources;

    public static ChildResourceAppointment From(LocationAppointment locationAppt, Guid parentResourceId)
        => From(locationAppt.ToResourceAppointment(), parentResourceId);

    public static ChildResourceAppointment From(ResourceAppointment resourceAppt, Guid parentResourceId)
        =>  new ChildResourceAppointment(resourceAppt, parentResourceId);

    public ResourceAppointment AsResourceAppointment() 
        => ResourceAppointment.From(LocationAppointment.From(this.Entity));
}
