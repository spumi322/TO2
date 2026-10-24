namespace Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a requested resource cannot be found.
    /// Maps to HTTP 404 Not Found status code.
    /// </summary>
    public sealed class NotFoundException : DomainException
    {
        public NotFoundException(string message) : base(message)
        {
        }

        // Users read the message, so no ids or property names in it (the request path in the warning log has them)
        public NotFoundException(string entityName, long id)
            : base($"{entityName} not found.")
        {
        }

        public NotFoundException(string entityName, string propertyName, object propertyValue)
            : base($"{entityName} not found.")
        {
        }
    }
}
