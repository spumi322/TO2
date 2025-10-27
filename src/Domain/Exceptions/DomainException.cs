namespace Domain.Exceptions
{
    /// <summary>
    /// Abstract base class for all domain-specific exceptions.
    /// Provides a consistent foundation for custom exception types used throughout the domain layer.
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message)
        {
        }

        protected DomainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
