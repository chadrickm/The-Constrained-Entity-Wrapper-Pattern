namespace Data.Entities;

[Table("Appointment")]
[EntityTypeConfiguration(typeof(AppointmentConfiguration))]
public class Appointment
{
    private readonly HashSet<SelectedAppointmentResource> _selectedAppointmentResources = [];
    private readonly HashSet<SelectedAppointmentEmployee> _selectedAppointmentEmployees = [];
    
    [Key]
    public Guid Id { get; init; }
    public Guid LocationId { get; init; }
    public Guid BookingTypeDetailId { get; init; }
    public BookingTypeDetail BookingTypeDetail { get; set; }
    public DateTime BookedStart { get; init; }
    public DateTime BookedEnd { get; init; }
    public Guid BookedByUserId { get; init; }

    /// <summary>
    /// For EF Mapping
    /// </summary>
    protected internal Appointment() { }

    public static Appointment CreateFromAppointmentInfo(
        Guid bookedByUserId,
        Guid locationId,
        Guid bookingTypeDetailId,
        AppointmentInfo appointmentInfo,
        IReadOnlyCollection<Resource> selectedResources,
        IReadOnlyCollection<Employee> selectedEmployees)
    {
        if (selectedResources.Count + selectedEmployees.Count == 0)
            throw new Exception("Trying to create an appointment without resources or employees being booked.");
    
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            LocationId = locationId,
            BookedByUserId = bookedByUserId,
            BookingTypeDetailId = bookingTypeDetailId,
            BookedStart = appointmentInfo.RealStartTime,
            BookedEnd = appointmentInfo.RealEndTime
        };
    
        // Add resources directly during creation
        foreach (var resource in selectedResources)
        {
            appointment._selectedAppointmentResources.Add(
                SelectedAppointmentResource.Create(appointment, resource));
        }
    
        // Add employees directly during creation (similar approach)
        foreach (var employee in selectedEmployees)
        {
            appointment._selectedAppointmentEmployees.Add(
                SelectedAppointmentEmployee.Create(appointment, employee));
        }
    
        return appointment;
    }

    public IReadOnlyCollection<SelectedAppointmentResource> SelectedAppointmentResources => _selectedAppointmentResources;

    public IReadOnlyCollection<SelectedAppointmentEmployee> SelectedAppointmentEmployees => _selectedAppointmentEmployees;
    // public virtual List<SelectedAppointmentAddOn> SelectedAppointmentAddOns { get; set; } = [];

    public void AddSelectedResource(MatchingResource matchingResource)
    {
        var selectedResource = new SelectedAppointmentResource
        {
            Appointment = this,
            AppointmentId = this.Id,
            Resource = matchingResource.AsResource(),
            ResourceId = matchingResource.Id
        };
        
        this._selectedAppointmentResources.Add(selectedResource);
    }
}
public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("(newid())");
        
        builder.HasMany(e => e.SelectedAppointmentResources)
            .WithOne(r => r.Appointment)
            .HasForeignKey(e => e.AppointmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasMany(e => e.SelectedAppointmentEmployees)
            .WithOne(e => e.Appointment)
            .HasForeignKey(e => e.AppointmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
        
        // builder.Property(a => a.BookingTypeDetailId)
        //     .IsRequired();
        //
        // builder.HasOne<BookingTypeDetail>() // Configure the relationship without navigation property
        //     .WithMany()
        //     .HasForeignKey(a => a.BookingTypeDetailId)
        //     .OnDelete(DeleteBehavior.NoAction);
    }
}
