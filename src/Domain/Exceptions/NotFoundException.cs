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

        public NotFoundException(string entityName, long id)
            : base($"{entityName} with Id: {id} was not found")
        {
        }

        public NotFoundException(string entityName, string propertyName, object propertyValue)
            : base($"{entityName} with {propertyName}: '{propertyValue}' was not found")
        {
        }
    }
}
