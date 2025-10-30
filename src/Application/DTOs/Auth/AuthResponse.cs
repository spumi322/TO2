using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Auth
{
    public record AuthResponse(
        long UserId,
        string UserName,
        string Email,
        long TenantId,
        string TenantName,
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt
    );
}
