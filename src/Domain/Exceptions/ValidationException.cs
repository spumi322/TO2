namespace Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when business validation rules are violated.
    /// Maps to HTTP 400 Bad Request status code.
    /// </summary>
    public sealed class ValidationException : DomainException
    {
        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
