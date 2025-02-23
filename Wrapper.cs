public abstract class Wrapper<T>(T entity)
    where T : class
{
    protected internal readonly T Entity = entity ?? throw new ArgumentNullException(nameof(entity));

    public void Persist(Action<T> saveAction) => saveAction(Entity);

    // Helper for Result-based validation (optional, since you’re leaning toward exceptions)
    protected static Result<U> Validate<U>(Func<bool> condition, string failureMessage, Func<U> create)
        => condition() ? Result.Failure<U>(failureMessage) : Result.Success(create());
}
