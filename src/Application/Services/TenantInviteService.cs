using Application.Contracts;
using Application.DTOs.Auth;
using Application.Utilities;
using Domain.AggregateRoots;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class TenantInviteService : ITenantInviteService
    {
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly ITenantService _tenantService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TenantInviteService> _logger;

        public TenantInviteService(
            IRepository<Tenant> tenantRepository,
            ITenantService tenantService,
            IUnitOfWork unitOfWork,
            ILogger<TenantInviteService> logger)
        {
            _tenantRepository = tenantRepository;
            _tenantService = tenantService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<InviteCodeResponse> CreateInviteCodeAsync()
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var tenant = await _tenantRepository.GetByIdAsync(tenantId)
                ?? throw new NotFoundException("Organization", tenantId);

            if (!tenant.IsActive)
                throw new ForbiddenException("This organization is inactive.");

            var code = InviteCode.Generate();
            var expiresAt = DateTime.UtcNow.Add(InviteCode.Lifetime);
            tenant.SetInviteCode(InviteCode.Hash(code), expiresAt);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Invite code created for tenant {TenantId} by {UserName}",
                tenant.Id, _tenantService.GetCurrentUserName());

            return new InviteCodeResponse(code, expiresAt);
        }
    }
}
