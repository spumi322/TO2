using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Base
{
    /// <summary>
    /// Base class for unit tests with common mocking utilities.
    /// </summary>
    public abstract class UnitTestBase
    {
        /// <summary>
        /// Creates a mock logger for the specified type.
        /// </summary>
        protected Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Creates a mock logger and returns both the mock and the logger instance.
        /// Useful when you need to verify logger calls.
        /// </summary>
        protected (Mock<ILogger<T>> Mock, ILogger<T> Logger) CreateLogger<T>()
        {
            var mock = new Mock<ILogger<T>>();
            return (mock, mock.Object);
        }
    }
}
