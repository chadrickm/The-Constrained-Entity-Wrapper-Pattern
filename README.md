# The Constrained Entity Wrapper Pattern: A Step-by-Step Tutorial

Welcome to the **Constrained Entity Wrapper Pattern**! This lightweight, type-safe design pattern helps you wrap raw database entities with domain-specific constraints and behaviors. It’s most useful if you’re starting with database tables but want to add business logic without diving into full Domain-Driven Design (DDD). In this tutorial, we’ll take a simple `Appointment` entity with an `Id` and a collection of child `SelectedAppointmentResource` objects, wrap it, tackle primitive obsession with value types, enforce business rules, guide it through state transitions, and ensure only a fully validated state with resources can be persisted—all step by step.

## Step 1: Start with a Simple Entity
Let’s begin with a basic `Appointment` entity, straight from a database, with no rules or constraints:

```csharp
public class Appointment
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public DateTime BookedStart { get; set; }
    public DateTime BookedEnd { get; set; }
    public List<SelectedAppointmentResource> SelectedAppointmentResources { get; set; } = new();
}

public class SelectedAppointmentResource
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
}
```

This is a raw, database-first model:
- **Ids**: `Appointment` and `SelectedAppointmentResource` each have a `Guid Id`.
- **Collection**: `SelectedAppointmentResources` is a list of child objects, initially empty.
- **No Constraints**: You can set anything—or nothing—and save it directly if it weren’t wrapped.

Our goal is to wrap it and restrict persistence to a specific state.

## Step 2: Wrap It with the Base Wrapper
The `Wrapper<T>` class encapsulates the entity, with `Persist` protected to prevent direct access:

```csharp
public abstract class Wrapper<T>(T entity) where T : class
{
    private readonly T _entity = entity ?? throw new ArgumentNullException(nameof(entity));

    protected TProperty GetProperty<TProperty>(Func<T, TProperty> selector)
        => selector(_entity);

    protected IReadOnlyCollection<TItem> GetCollection<TItem>(Func<T, IEnumerable<TItem>> selector)
        => selector(_entity).ToList().AsReadOnly();

    protected static Result<U> Validate<U>(Func<bool> condition, string failureMessage, Func<U> create)
        => condition() ? Result.Failure<U>(failureMessage) : Result.Success(create());

    protected void ModifyEntity(Action<T> modification)
    {
        modification(_entity);
    }

    protected void Persist(Action<T> saveAction) => saveAction(_entity);

    protected internal T UnwrapEntity() => _entity;
}
```

Create a basic wrapper:

```csharp
public class BasicAppointment : Wrapper<Appointment>
{
    private BasicAppointment(Appointment appt) : base(appt) { }

    public AppointmentId Id => AppointmentId.From(GetProperty(e => e.Id));
    public LocationId LocationId => LocationId.From(GetProperty(e => e.LocationId));
    public DateTime BookedStart => GetProperty(e => e.BookedStart);
    public DateTime BookedEnd => GetProperty(e => e.BookedEnd);
    public IReadOnlyCollection<SelectedAppointmentResource> SelectedResources 
        => GetCollection(e => e.SelectedAppointmentResources);

    public static BasicAppointment From(Appointment appt) => new(appt);
}
```

### What’s Happening?
- **Encapsulation**: The entity is private, with read-only access via `GetProperty` and `GetCollection`.
- **Persistence Hidden**: `Persist` is `protected`, so users can’t call it directly on `BasicAppointment`.
- **No Rules Yet**: It’s a shell, exposing data without constraints or persistence.

## Step 3: Tackle Primitive Obsession with Value Types
The raw `Guid` properties (`Id`, `LocationId`, `ResourceId`) lack validation and meaning—this is **primitive obsession**. Let’s replace them with value types:

```csharp
public record AppointmentId
{
    public Guid Value { get; }
    private AppointmentId(Guid value) => Value = value;
    public static AppointmentId From(Guid value) =>
        value == Guid.Empty ? throw new Exception("AppointmentId cannot be empty") : new AppointmentId(value);
}

public record LocationId
{
    public Guid Value { get; }
    private LocationId(Guid value) => Value = value;
    public static LocationId From(Guid value) =>
        value == Guid.Empty ? throw new Exception("LocationId cannot be empty") : new LocationId(value);
}

public record ResourceId
{
    public Guid Value { get; }
    private ResourceId(Guid value) => Value = value;
    public static ResourceId From(Guid value) =>
        value == Guid.Empty ? throw new Exception("ResourceId cannot be empty") : new ResourceId(value);
}
```

Update the wrapper:

```csharp
public class BasicAppointment : Wrapper<Appointment>
{
    private BasicAppointment(Appointment appt) : base(appt) { }

    public AppointmentId Id => AppointmentId.From(GetProperty(e => e.Id));
    public LocationId LocationId => LocationId.From(GetProperty(e => e.LocationId));
    public DateTime BookedStart => GetProperty(e => e.BookedStart);
    public DateTime BookedEnd => GetProperty(e => e.BookedEnd);
    public IReadOnlyCollection<SelectedAppointmentResource> SelectedResources 
        => GetCollection(e => e.SelectedAppointmentResources);

    public static BasicAppointment From(Appointment appt) => new(appt);
}
```

### Why Value Types?
- **Validation**: Ensures no empty `Guid`s, adding basic integrity.
- **Clarity**: Distinguishes `AppointmentId`, `LocationId`, and `ResourceId`.
- **No Persistence Yet**: Users still can’t save it—`Persist` is protected.

## Step 4: Add Basic Business Rules
Let’s enforce initial rules: a valid location and times. Rename to `LocationAppointment`:

```csharp
public class LocationAppointment : Wrapper<Appointment>
{
    private LocationAppointment(Appointment appt) : base(appt)
    {
        if (appt.LocationId == Guid.Empty)
            throw new Exception("Appointment must have a valid LocationId");
        if (appt.BookedStart == DateTime.MinValue)
            throw new Exception("Appointment must have a valid BookedStart date");
        if (appt.BookedEnd == DateTime.MinValue)
            throw new Exception("Appointment must have a valid BookedEnd date");
        if (appt.BookedEnd <= appt.BookedStart)
            throw new Exception("BookedEnd must be after BookedStart");
    }

    public AppointmentId Id => AppointmentId.From(GetProperty(e => e.Id));
    public LocationId LocationId => LocationId.From(GetProperty(e => e.LocationId));
    public DateTime BookedStart => GetProperty(e => e.BookedStart);
    public DateTime BookedEnd => GetProperty(e => e.BookedEnd);
    public IReadOnlyCollection<SelectedAppointmentResource> SelectedResources 
        => GetCollection(e => e.SelectedAppointmentResources);

    public static LocationAppointment From(Appointment appt) => new(appt);
}
```

### What’s New?
- **Constraints**: Ensures basic validity, but persistence remains blocked.
- **Naming**: `LocationAppointment` reflects a location-bound state.
- **Progression Needed**: Still can’t persist—resources are required next.

## Step 5: Add Domain Behavior
The business needs resources assigned. Add an `AddResource` method:

```csharp
public class LocationAppointment : Wrapper<Appointment>
{
    private LocationAppointment(Appointment appt) : base(appt)
    {
        if (appt.LocationId == Guid.Empty)
            throw new Exception("Appointment must have a valid LocationId");
        if (appt.BookedStart == DateTime.MinValue)
            throw new Exception("Appointment must have a valid BookedStart date");
        if (appt.BookedEnd == DateTime.MinValue)
            throw new Exception("Appointment must have a valid BookedEnd date");
        if (appt.BookedEnd <= appt.BookedStart)
            throw new Exception("BookedEnd must be after BookedStart");
    }

    public AppointmentId Id => AppointmentId.From(GetProperty(e => e.Id));
    public LocationId LocationId => LocationId.From(GetProperty(e => e.LocationId));
    public DateTime BookedStart => GetProperty(e => e.BookedStart);
    public DateTime BookedEnd => GetProperty(e => e.BookedEnd);
    public IReadOnlyCollection<SelectedAppointmentResource> SelectedResources 
        => GetCollection(e => e.SelectedAppointmentResources);

    public void AddResource(ResourceId resourceId)
    {
        ModifyEntity(e => e.SelectedAppointmentResources.Add(new SelectedAppointmentResource 
        { 
            Id = Guid.NewGuid(), 
            ResourceId = resourceId.Value 
        }));
    }

    public static LocationAppointment From(Appointment appt) => new(appt);
}
```

### What’s Happening?
- **Behavior**: `AddResource` updates the collection safely.
- **Persistence Blocked**: `Persist` is still protected, inaccessible here.

## Step 6: Introduce State Progression with Persistence
An appointment with resources becomes a `ResourceAppointment`, the only state where persistence is allowed:

```csharp
public class ResourceAppointment : Wrapper<Appointment>
{
    private ResourceAppointment(LocationAppointment locationAppointment) : base(locationAppointment.UnwrapEntity())
    {
        if (locationAppointment.SelectedResources.Count == 0)
            throw new Exception("ResourceAppointment requires at least one selected resource");
    }

    public AppointmentId Id => AppointmentId.From(GetProperty(e => e.Id));
    public LocationId LocationId => LocationId.From(GetProperty(e => e.LocationId));
    public DateTime BookedStart => GetProperty(e => e.BookedStart);
    public DateTime BookedEnd => GetProperty(e => e.BookedEnd);
    public IReadOnlyCollection<SelectedAppointmentResource> SelectedResources 
        => GetCollection(e => e.SelectedAppointmentResources);

    public static ResourceAppointment From(LocationAppointment locationAppointment) 
        => new(locationAppointment);

    public void Persist(Action<T> saveAction) => base.Persist(saveAction); // Publicly expose persistence
}
```

Update `LocationAppointment` to enable the transition:

```csharp
public class LocationAppointment : Wrapper<Appointment>
{
    private LocationAppointment(Appointment appt) : base(appt)
    {
        if (appt.LocationId == Guid.Empty)
            throw new Exception("Appointment must have a valid LocationId");
        if (appt.BookedStart == DateTime.MinValue)
            throw new Exception("Appointment must have a valid BookedStart date");
        if (appt.BookedEnd == DateTime.MinValue)
            throw new Exception("Appointment must have a valid BookedEnd date");
        if (appt.BookedEnd <= appt.BookedStart)
            throw new Exception("BookedEnd must be after BookedStart");
    }

    public AppointmentId Id => AppointmentId.From(GetProperty(e => e.Id));
    public LocationId LocationId => LocationId.From(GetProperty(e => e.LocationId));
    public DateTime BookedStart => GetProperty(e => e.BookedStart);
    public DateTime BookedEnd => GetProperty(e => e.BookedEnd);
    public IReadOnlyCollection<SelectedAppointmentResource> SelectedResources 
        => GetCollection(e => e.SelectedAppointmentResources);

    public void AddResource(ResourceId resourceId)
    {
        ModifyEntity(e => e.SelectedAppointmentResources.Add(new SelectedAppointmentResource 
        { 
            Id = Guid.NewGuid(), 
            ResourceId = resourceId.Value 
        }));
    }

    public static LocationAppointment From(Appointment appt) => new(appt);

    public ResourceAppointment ToResourceAppointment() => ResourceAppointment.From(this);
}
```

### How It Works
- **Transition**: `ToResourceAppointment` requires resources, moving to a persistable state.
- **Persistence**: Only `ResourceAppointment` exposes a public `Persist`; earlier states keep it protected.
- **Workflow**: Forces progression—location, resources, then persist.

## Step 7: Use It in Practice
Let’s test the workflow:

```csharp
// Create a raw appointment
var rawAppointment = new Appointment
{
    Id = Guid.NewGuid(),
    LocationId = Guid.NewGuid(),
    BookedStart = new DateTime(2025, 2, 15, 9, 0, 0),
    BookedEnd = new DateTime(2025, 2, 15, 9, 30, 0)
};

// Wrap it
var basicAppt = BasicAppointment.From(rawAppointment);
// basicAppt.Persist(...) // Compile error: Persist is inaccessible

var locationAppt = LocationAppointment.From(rawAppointment);
// locationAppt.Persist(...) // Compile error: Persist is inaccessible

// Add a resource and transition
var resourceId = ResourceId.From(Guid.NewGuid());
locationAppt.AddResource(resourceId);
var resourceAppt = locationAppt.ToResourceAppointment();

// Persist (succeeds)
resourceAppt.Persist(appt => Console.WriteLine($"Saved appointment {appt.Id} with {appt.SelectedAppointmentResources.Count} resources"));

// Output: Saved appointment <guid> with 1 resources
```

### What’s Happening?
- **Blocked Early**: `BasicAppointment` and `LocationAppointment` can’t expose `Persist`—it’s protected.
- **Valid State**: Only `ResourceAppointment` makes `Persist` public, ensuring resources are present.
- **Process**: Enforces a strict flow—persistence is impossible until the final state.

## Benefits
- **Strict Workflow**: Only fully validated states can persist, guiding the process tightly.
- **Type Safety**: Value types (`AppointmentId`, `LocationId`, `ResourceId`) prevent errors.
- **Encapsulation**: Private entity with controlled access keeps it secure.
- **Business Focus**: Rules and transitions match domain needs.

## Limitations
- **Fixed Progression**: Transitions are predefined, not dynamic.
- **Collection Tracking**: Managing `SelectedResources` changes needs extra effort beyond read-only access.
- **State Reversion**: Not built-in; add it separately if needed (e.g., with snapshots).

## Conclusion
The Constrained Entity Wrapper Pattern transforms raw entities into domain-rich objects, step by step. From a simple `Appointment`, we added value types to fix primitive obsession, enforced rules, and built a progression where only a `ResourceAppointment` can persist—locking earlier states out of persistence entirely. It’s a practical way to evolve database-first code into a meaningful domain model, ensuring only complete, valid states reach the database.

---
