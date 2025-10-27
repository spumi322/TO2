namespace Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an operation is not permitted due to authorization or business rules.
    /// Examples: accessing closed tournaments, modifying finalized results, unauthorized state transitions.
    /// Maps to HTTP 403 Forbidden status code.
    /// </summary>
    public sealed class ForbiddenException : DomainException
    {
        public ForbiddenException(string message) : base(message)
        {
        }

        public ForbiddenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
