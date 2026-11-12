using Application.Contracts;
using Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TO2.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/tenant")]
    public class TenantController : ControllerBase
    {
        private readonly ITenantInviteService _tenantInviteService;

        public TenantController(ITenantInviteService tenantInviteService)
        {
            _tenantInviteService = tenantInviteService;
        }

        // POST: api/tenant/invite-code
        [HttpPost("invite-code")]
        public async Task<ActionResult<InviteCodeResponse>> CreateInviteCode()
        {
            return Ok(await _tenantInviteService.CreateInviteCodeAsync());
        }
    }
}
