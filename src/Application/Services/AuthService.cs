using Application.Contracts;
using Application.DTOs.Auth;
using Application.Utilities;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IIdentityService _identityService;
        private readonly ITokenService _tokenService;
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IRepository<RefreshToken> _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IIdentityService identityService,
            ITokenService tokenService,
            IRepository<Tenant> tenantRepository,
            IRepository<RefreshToken> refreshTokenRepository,
            IUnitOfWork unitOfWork,
            ILogger<AuthService> logger)
        {
            _identityService = identityService;
            _tokenService = tokenService;
            _tenantRepository = tenantRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if user already exists
            if (await _identityService.EmailExistsAsync(request.Email))
                throw new ConflictException("User with this email already exists");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var tenant = await FindTenantByNameAsync(request.TenantName);
                var hasInviteCode = !string.IsNullOrWhiteSpace(request.InviteCode);

                // New name: create the organization. Existing name: join only with its valid invite code
                if (tenant == null)
                {
                    if (hasInviteCode)
                        throw new ValidationException("Organization not found. Check the name, or leave the invite code empty to create it.");

                    tenant = new Tenant
                    {
                        Name = request.TenantName,
                        IsActive = true
                    };
                    await _tenantRepository.AddAsync(tenant);
                    try
                    {
                        await _unitOfWork.SaveChangesAsync();
                    }
                    catch (ConflictException)
                    {
                        // Unique NormalizedName index: another registration created this name a moment ago
                        throw new ConflictException("This organization name was just taken. Enter an invite code to join it, or choose another name.");
                    }

                    _logger.LogInformation(
                        "Created new tenant '{TenantName}' for user {Email}",
                        tenant.Name, request.Email
                    );
                }
                else
                {
                    if (!hasInviteCode)
                        throw new ConflictException("This organization already exists. Enter an invite code from one of its members to join.");

                    if (!tenant.IsInviteCodeValid(InviteCode.Hash(request.InviteCode!), DateTime.UtcNow))
                    {
                        _logger.LogWarning("Invalid invite code from {Email} for tenant '{TenantName}' (Id: {TenantId})",
                            request.Email, tenant.Name, tenant.Id);
                        throw new ValidationException("Invalid or expired invite code.");
                    }

                    _logger.LogInformation(
                        "User {Email} joining existing tenant '{TenantName}' (Id: {TenantId}) with an invite code",
                        request.Email, tenant.Name, tenant.Id
                    );
                }

                // Validate tenant is active
                if (!tenant.IsActive)
                {
                    throw new ValidationException(
                        "This organization is currently inactive. Please contact support."
                    );
                }

                // Create user with existing or new tenant
                var user = await _identityService.CreateUserAsync(request.UserName, request.Email, request.Password, tenant.Id);

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("User {Email} registered successfully with tenant {TenantId}",
                    request.Email, tenant.Id);

                // Generate tokens and return response
                return await GenerateAuthResponseAsync(user, tenant);
            }
            catch
            {
                if (_unitOfWork.HasActiveTransaction)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
                throw;
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _identityService.ValidateCredentialsAsync(request.Email, request.Password);
            if (user == null)
            {
                _logger.LogWarning("Failed login for {Email}", request.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null)
                throw new UnauthorizedException("Invalid email or password");

            if (!tenant.IsActive)
                throw new ForbiddenException("Tenant account is inactive");

            _logger.LogInformation("User {Email} logged in successfully", request.Email);

            return await GenerateAuthResponseAsync(user, tenant);
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var refreshToken = await FindRefreshTokenAsync(request.RefreshToken);

            if (refreshToken == null || !refreshToken.IsActive)
                throw new UnauthorizedException("Invalid or expired refresh token");

            var user = await _identityService.FindByIdAsync(refreshToken.UserId);
            if (user == null)
                throw new UnauthorizedException("Invalid or expired refresh token");

            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null)
                throw new UnauthorizedException("Invalid or expired refresh token");

            // Revoke old refresh token
            refreshToken.Revoked = DateTime.UtcNow;

            // Generate new tokens
            var response = await GenerateAuthResponseAsync(user, tenant);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Refresh token used for user {UserId}", user.Id);

            return response;
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            var token = await FindRefreshTokenAsync(refreshToken);

            if (token == null)
                throw new UnauthorizedException("Invalid refresh token");

            if (!token.IsActive)
                throw new ValidationException("Token is already revoked");

            token.Revoked = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Refresh token revoked for user {UserId}", token.UserId);
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            var token = await FindRefreshTokenAsync(refreshToken);

            return token != null && token.IsActive;
        }

        public async Task<OrganizationExistsResponse> OrganizationExistsAsync(string name)
        {
            return new OrganizationExistsResponse(await FindTenantByNameAsync(name) != null);
        }

        private Task<Tenant?> FindTenantByNameAsync(string name)
        {
            var normalizedName = Tenant.NormalizeName(name);
            return _tenantRepository.FindAsync(t => t.NormalizedName == normalizedName);
        }

        private Task<RefreshToken?> FindRefreshTokenAsync(string refreshToken)
        {
            var tokenHash = _tokenService.HashRefreshToken(refreshToken);
            return _refreshTokenRepository.FindAsync(rt => rt.TokenHash == tokenHash);
        }

        private async Task<AuthResponse> GenerateAuthResponseAsync(AuthUser user, Tenant tenant)
        {
            var accessToken = _tokenService.CreateAccessToken(user, tenant);

            var refreshToken = _tokenService.CreateRefreshToken(user.Id);
            await _refreshTokenRepository.AddAsync(refreshToken.Entity);
            await _unitOfWork.SaveChangesAsync();

            return new AuthResponse(
                UserId: user.Id,
                UserName: user.UserName,
                Email: user.Email,
                TenantId: tenant.Id,
                TenantName: tenant.Name,
                AccessToken: accessToken.Token,
                RefreshToken: refreshToken.Token,
                ExpiresAt: accessToken.ExpiresAt
            );
        }
    }
}
