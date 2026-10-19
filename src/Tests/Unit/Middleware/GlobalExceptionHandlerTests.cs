using Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using webAPI.Middleware;

namespace Tests.Unit.Middleware
{
    /// <summary>
    /// GlobalExceptionHandler: status mapping, and only domain exception messages reach the client.
    /// </summary>
    public class GlobalExceptionHandlerTests
    {
        private readonly GlobalExceptionHandler _handler = new(NullLogger<GlobalExceptionHandler>.Instance);

        private async Task<(int Status, JsonElement Body)> HandleAsync(Exception exception)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await _handler.TryHandleAsync(context, exception, CancellationToken.None);

            context.Response.Body.Position = 0;
            var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
            return (context.Response.StatusCode, body);
        }

        [Fact]
        public async Task ServerError_HidesExceptionMessage_AndReturnsTraceId()
        {
            var (status, body) = await HandleAsync(
                new InvalidOperationException("42703: column t.InviteCodeExpiresAt does not exist"));

            status.Should().Be(500);
            var detail = body.GetProperty("detail").GetString();
            detail.Should().NotContain("42703").And.NotContain("InviteCodeExpiresAt");
            detail.Should().Contain("unexpected error");
            body.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task UnauthorizedAccess_HidesExceptionMessage()
        {
            var (status, body) = await HandleAsync(new UnauthorizedAccessException("Access to the path 'C:\\secret' is denied."));

            status.Should().Be(401);
            body.GetProperty("detail").GetString().Should().Be("Authentication required.");
        }

        public static TheoryData<DomainException, int> DomainExceptions => new()
        {
            { new NotFoundException("Tournament not found."), 404 },
            { new ValidationException("Invalid or expired invite code."), 400 },
            { new ConflictException("This organization already exists."), 409 },
            { new ForbiddenException("Tenant account is inactive"), 403 },
            { new UnauthorizedException("Invalid email or password"), 401 }
        };

        [Theory]
        [MemberData(nameof(DomainExceptions))]
        public async Task DomainException_KeepsItsMessage(DomainException exception, int expectedStatus)
        {
            var (status, body) = await HandleAsync(exception);

            status.Should().Be(expectedStatus);
            body.GetProperty("detail").GetString().Should().Be(exception.Message);
        }
    }
}
