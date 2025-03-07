Below is an updated README for the Constrained Entity Wrapper Pattern that aligns with your original style, incorporates the new `Wrapper<T>` class you provided, integrates the use of value types like `LocationId` and `ParentResourceId`, and retains the emphasis on state transitions (e.g., `ToResourceAppointment`, `ToEmployeeAppointment`) as a core feature. I’ve kept the original structure intact where possible, updating only where necessary to reflect your new code and concepts, such as controlled access methods, value types, and the derived `LocationAppointment` class. The README also reflects the "other stuff" you’ve discussed, like collection handling and practical usage from your tests, while avoiding the removal of state transitions.

---

# The Constrained Entity Wrapper Pattern

## Overview
The **Constrained Wrapper Pattern** is a lightweight, type-safe design pattern for wrapping raw entities (e.g., database models) with domain-specific constraints and behaviors. It provides a reusable, generic framework to enforce business rules, guide entities through valid state progressions, and control persistence—without requiring a total commitment to Domain-Driven Design (DDD) or complex architectural overhauls. This pattern is ideal for codebases where developers start with database tables rather than business processes, offering a stepping stone toward domain-centric thinking.

### Key Features
- **Generic Base**: A `Wrapper<T>` class that wraps any entity type, providing controlled access, persistence, and validation.
- **Type-Specific Subclasses**: Concrete wrappers (e.g., `LocationAppointment`, `ResourceAppointment`) that impose constraints and define valid states.
- **Progression**: Methods to transition between states (e.g., `ToResourceAppointment`), reflecting business workflows.
- **Controlled Persistence**: A `Persist` method that delegates saving without exposing the underlying entity.
- **Validation**: Constraints enforced via exceptions or a `Result<T>` type, ensuring invalid states are caught early.

### Intent
The Constrained Wrapper Pattern aims to:
- Add domain logic to database-first models without disrupting existing code.
- Shift focus from raw data to business processes in a pragmatic way.
- Enable type safety and encapsulation for entities in any codebase.

## Motivation
In many projects, development begins with database tables—think `Appointment` or `Resource` classes autogenerated from a schema. While this approach is quick to start, it often leaves business logic scattered or missing, treating objects as mere data containers rather than meaningful domain concepts. Full DDD can address this, but its complexity (aggregates, repositories, bounded contexts) can overwhelm teams not ready for that leap. The Constrained Entity Wrapper Pattern offers a middle ground: it wraps these raw entities in a type-safe, constrained shell that reflects business rules and workflows, making it easy to adopt and incrementally evolve toward DDD principles.

For example, an `Appointment` from a database might have a `LocationId`, but the business requires that it’s only valid for scheduling if that ID is set. Rather than exposing the raw `Appointment` and hoping callers check it, a `LocationAppointment` wrapper enforces this constraint, turning a generic row into a domain-specific concept.

## Structure
The pattern revolves around a generic base class and specific wrappers:

### Generic Base: `Wrapper<T>`
```csharp
public abstract class Wrapper<T>(T entity) where T : class
{
    private readonly T _entity = entity ?? throw new ArgumentNullException(nameof(entity));

    // Protected method to access specific properties
    protected TProperty GetProperty<TProperty>(Func<T, TProperty> selector)
        => selector(_entity);

    // For collections, ensure read-only return
    protected IReadOnlyCollection<TItem> GetCollection<TItem>(Func<T, IEnumerable<TItem>> selector)
        => selector(_entity).ToList().AsReadOnly();

    // Helper for Result-based validation (optional, since you’re leaning toward exceptions)
    protected static Result<U> Validate<U>(Func<bool> condition, string failureMessage, Func<U> create)
        => condition() ? Result.Failure<U>(failureMessage) : Result.Success(create());

    // Protected method for derived classes to modify the entity
    protected void ModifyEntity(Action<T> modification)
    {
        modification(_entity);
    }

    // Expose the entity for derived classes to use
    public void Persist(Action<T> saveAction) => saveAction(_entity);

    // Protected method for derived classes to unwrap when needed
    protected internal T UnwrapEntity() => _entity;
}
```
- **Purpose**: Provides a reusable foundation for wrapping any entity type (`T`), encapsulating it tightly with private access, offering controlled persistence, and enabling domain-specific behavior through derived classes.
- **Key Elements**: 
  - A private `_entity` field holds the live entity, ensuring encapsulation.
  - `GetProperty` provides read-only access to scalar properties (e.g., `LocationId`).
  - `GetCollection` returns read-only collections (e.g., `SelectedAppointmentResources`), preventing external modification.
  - `ModifyEntity` allows controlled changes to the entity by derived classes.
  - `Persist` delegates saving to an external action (e.g., a repository) without exposing `_entity`.
  - `UnwrapEntity` offers internal access for derived classes, maintaining flexibility.

#### Controlled Access and Encapsulation
The `Wrapper<T>` class enhances encapsulation by making the entity private, requiring derived classes to use `GetProperty` and `GetCollection` for reading and `ModifyEntity` for writing. This ensures that all interactions with the entity are deliberate and constrained, reducing the risk of unintended changes.

#### Value Type Integration
The pattern supports value types (e.g., `LocationId`, `ParentResourceId`) to replace primitive types like `Guid`. These value types encapsulate domain-specific validation (e.g., rejecting empty GUIDs) and improve type safety by distinguishing between similar primitives (e.g., `LocationId` vs. `ResourceId`). Wrappers expose these as properties, syncing them with the entity’s primitives via `GetProperty` while enforcing rules at the domain level.

#### Collection Handling
For wrappers managing modifiable collections (e.g., a `Resource` with `Appointments`), `GetCollection` ensures read-only access to the current state. However, tracking changes (e.g., adds, removes) requires parent wrappers to maintain separate snapshots (e.g., `_originalAppointments`) and sync them during `Persist`, as the base class doesn’t handle list mutations.

### Concrete Wrappers
Subclasses inherit from `Wrapper<T>` and add domain-specific constraints, behaviors, value types, and state transitions. For example:

#### `LocationAppointment`
```csharp
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

    public LocationId LocationId => LocationId.From(GetProperty(e => e.LocationId));
    
    public IReadOnlyCollection<SelectedAppointmentResource> SelectedAppointmentResources 
        => GetCollection(e => e.SelectedAppointmentResources);
    
    public IReadOnlyCollection<SelectedAppointmentEmployee> SelectedAppointmentEmployees 
        => GetCollection(e => e.SelectedAppointmentEmployees);
    
    public DateTime BookedStart => GetProperty(e => e.BookedStart);
    public DateTime BookedEnd => GetProperty(e => e.BookedEnd);

    public void AddSelectedResource(MatchingResource matchingResource)
    {
        ModifyEntity(entity => entity.AddSelectedResource(matchingResource));
    }

    public static LocationAppointment From(Appointment appt) => new(appt);

    public ResourceAppointment ToResourceAppointment() 
        => ResourceAppointment.From(this);

    public EmployeeAppointment ToEmployeeAppointment() 
        => EmployeeAppointment.From(this);
}
```
- **Constraints**: Ensures `LocationId`, `BookedStart`, and `BookedEnd` are valid.
- **Progression**: Offers state transitions to `ResourceAppointment` or `EmployeeAppointment`.
- **Value Type**: Uses `LocationId` instead of `Guid`, accessed via `GetProperty`.
- **Modification**: Adds resources with `ModifyEntity`, keeping changes controlled.

#### `ResourceAppointment`
```csharp
public class ResourceAppointment : Wrapper<Appointment>
{
    private ResourceAppointment(LocationAppointment locationAppointment) : base(locationAppointment.UnwrapEntity())
    {
        if (locationAppointment.SelectedAppointmentResources.Count == 0)
            throw new Exception($"{nameof(ResourceAppointment)} requires one or more {nameof(SelectedAppointmentResource)}");
    }

    public IReadOnlyCollection<SelectedAppointmentResource> SelectedAppointmentResources 
        => GetCollection(e => e.SelectedAppointmentResources);
    
    public DateTime BookedStart => GetProperty(e => e.BookedStart);
    public DateTime BookedEnd => GetProperty(e => e.BookedEnd);

    public static ResourceAppointment From(LocationAppointment locationAppointment) => new(locationAppointment);

    public ParentResourceAppointment ToParentResource(ParentResourceId parentResourceId)
        => ParentResourceAppointment.From(this, parentResourceId);

    public ChildResourceAppointment ToChildResource(ParentResourceId parentResourceId)
        => ChildResourceAppointment.From(this, parentResourceId);
}
```
- **Constraints**: Requires at least one resource.
- **Progression**: Transitions to `ParentResourceAppointment` or `ChildResourceAppointment` with `ParentResourceId`.
- **Exposure**: Limits access to relevant properties via `GetCollection` and `GetProperty`.

#### Example Value Types
```csharp
public record LocationId
{
    public Guid Value { get; }
    private LocationId(Guid value) => Value = value;
    public static LocationId From(Guid value) =>
        value == Guid.Empty ? throw new Exception("LocationId cannot be empty") : new LocationId(value);
}

public record ParentResourceId
{
    public Guid? Value { get; }
    private ParentResourceId(Guid? value) => Value = value;

    public static ParentResourceId From(Guid? value)
    {
        if (!value.HasValue) return new((Guid?)null);
        if (value == Guid.Empty)
            throw new Exception("ParentResourceId cannot be empty if provided");
        return new ParentResourceId(value);
    }

    public static ParentResourceId None => new((Guid?)null);
    public bool HasValue => Value.HasValue;
    public ResourceId AsResourceId() => Value.HasValue 
        ? ResourceId.From(Value.Value) 
        : throw new Exception("ParentResourceId must have a value to be converted to ResourceId");
}
```

## How It Works
1. **Wrapping**: Start with a raw entity (e.g., an `Appointment` from a database).
2. **Constraint Enforcement**: Use a factory method (`From`) to create a wrapper, applying validation (e.g., exceptions for missing `LocationId`).
3. **Progression**: Transition to other wrappers (e.g., `ToResourceAppointment`) as the business process evolves, each enforcing its own rules.
4. **Modification**: Alter the entity via wrapper methods (e.g., `AddSelectedResource`) using `ModifyEntity`, keeping changes controlled.
5. **Persistence**: Call `Persist` with a save action (e.g., `repo.Save`) to store the entity without exposing it, syncing collections if needed.

### Example Usage
```csharp
public static List<ChildResourceAppointment> GetChildResourceAppointmentsOfMatchingResourceIfAny(
    List<ResourceAppointment> resourceAppointments,
    List<LocationResource> locationResources, 
    MatchingResource matchingResource)
{
    var childResourceAppointmentsToReturn = new List<ChildResourceAppointment>();
    
    var childResourceIds = locationResources
        .Where(r => r.ParentResourceId == matchingResource.Id)
        .Select(r => r.Id)
        .ToList();

    if (childResourceIds.Count == 0) return childResourceAppointmentsToReturn;

    childResourceAppointmentsToReturn = resourceAppointments
        .Where(a => a.SelectedAppointmentResources
            .Any(r => childResourceIds.Contains(r.ResourceId)))
        .Select(ra => ChildResourceAppointment.From(ra, ParentResourceId.From(matchingResource.Id)))
        .ToList();

    return childResourceAppointmentsToReturn;
}
```

## Benefits
- **Ease of Integration**: Works with existing database-first models—no refactoring required.
- **Explicit rather than Implicit**: The developer does not have to infer anything about the entity wrapped. If it’s wrapped in a type, it’s valid for that type’s usages.
- **Type Safety**: Specific wrappers and value types (e.g., `LocationId`, `ParentResourceId`) ensure methods operate on validated states.
- **Encapsulation**: Hides the raw entity with private access, exposing only domain-relevant properties and actions via controlled methods.
- **Business Focus**: Constraints, progression, and value types reflect business rules, not just data structure.
- **Stepping Stone to DDD**: Introduces invariants, domain concepts, and state transitions without full DDD overhead.

## Relation to Other Patterns
- **Wrapper/Adapter**: The core mechanic of wrapping an entity for a new purpose.
- **Decorator**: The progression of wrappers adds layers of behavior, though it’s more static than dynamic.
- **Type State**: Each wrapper represents a valid state, making invalid states unrepresentable.
- **Result Pattern**: Optional use of `Result<T>` for validation complements the constraint focus.
- **Value Object**: Value types align with DDD’s value object concept, encapsulating small, immutable domain concepts.

## When to Use It
- You’re starting with database-generated models and want to add domain logic.
- You need a simple way to enforce business rules without a major redesign.
- You want to guide a team toward DDD principles incrementally.
- You need state transitions (e.g., from `LocationAppointment` to `ResourceAppointment`) to model workflows.

## Limitations
- **Static Progression**: Unlike Decorators, the wrapper chain is predefined, limiting runtime flexibility.
- **Exposure Trade-Off**: While encapsulated, some properties (e.g., `LocationId`) must be exposed, requiring careful design. This can also be a benefit as it forces intentional exposure decisions.
- **Validation Overhead**: Adding constraints to every wrapper can feel repetitive without helper methods, but constraints are optional—sometimes the wrapper type alone guides developers.
- **Collection Management**: The base `Wrapper<T>` doesn’t track modifiable collections (e.g., `Appointments`). Parent wrappers must handle this separately, adding complexity for list-heavy entities.
- **State Reversion**: Not built into the base class; requires external implementation (e.g., via cloning) if needed.

## Conclusion
The Constrained Wrapper Pattern is a pragmatic bridge between database-first development and domain-centric design. By wrapping entities in type-safe, constrained shells with private access, it shifts focus from raw data to business meaning—perfect for teams not ready for full DDD but wanting to move that way. The new `Wrapper<T>` enhances encapsulation with controlled access methods, while value types like `LocationId` and `ParentResourceId` eliminate primitive obsession, embedding domain rules in the type system. State transitions remain a core strength, guiding entities through workflows (e.g., `LocationAppointment` to `ResourceAppointment`), though collections require separate handling in parent wrappers. Whether you’re validating an `Appointment`’s location or managing resource schedules as shown in practical tests, this pattern offers a reusable, intuitive way to enforce rules, model processes, and adapt to evolving needs—all while keeping the codebase approachable.

---

### Changes and Additions
1. **Structure > Generic Base**: Replaced the old `Wrapper<T>` with your new version, adding subsections for **Controlled Access and Encapsulation**, **Value Type Integration**, and **Collection Handling** to reflect the new features and limitations.
2. **Concrete Wrappers**: Updated `LocationAppointment` with your new code, using `GetProperty`, `GetCollection`, and `ModifyEntity`. Adjusted `ResourceAppointment` to use `UnwrapEntity()` and added transitions with `ParentResourceId`.
3. **Value Types**: Included `LocationId` and `ParentResourceId` examples from your code, highlighting their role in type safety and validation.
4. **How It Works**: Updated to reflect modification via `ModifyEntity`, keeping progression emphasis.
5. **Example Usage**: Kept the original example, updated to use `ParentResourceId.From` for consistency with your value types.
6. **Benefits**: Added encapsulation and retained state transition focus.
7. **Limitations**: Noted the lack of built-in state reversion and collection tracking, aligning with your new design.
8. **Conclusion**: Emphasized encapsulation, value types, and state transitions, referencing your scheduling tests for practical context.

This README sticks to your original style, keeps state transitions central, and integrates your new `Wrapper<T>`, value types, and test-driven insights. Does this meet your vision? Any tweaks you’d like?
