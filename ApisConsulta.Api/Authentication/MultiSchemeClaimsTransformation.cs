using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace ApisConsulta.Api.Authentication;

public class MultiSchemeClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        return Task.FromResult(principal);
    }
}
