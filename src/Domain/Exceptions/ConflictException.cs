namespace Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an operation conflicts with the current state of the system.
    /// Examples: duplicate entries, concurrent modifications, state transition conflicts.
    /// Maps to HTTP 409 Conflict status code.
    /// </summary>
    public sealed class ConflictException : DomainException
    {
        public ConflictException(string message) : base(message)
        {
        }

        public ConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
