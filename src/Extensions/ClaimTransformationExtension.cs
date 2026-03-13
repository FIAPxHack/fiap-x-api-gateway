using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ApiGateway.Extensions;

public static class ClaimTransformationExtension
{
    public static void TransformClaims(this TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return;

        var claimsToAdd = new List<Claim>();
        var claimsToRemove = new List<Claim>();

        // Mapear role claim
        var roleClaim = identity.FindFirst(c =>
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" ||
            c.Type == ClaimTypes.Role);
        if (roleClaim != null)
        {
            claimsToRemove.Add(roleClaim);
            claimsToAdd.Add(new Claim("role", roleClaim.Value));
        }

        // Mapear email claim
        var emailClaim = identity.FindFirst(c =>
            c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress" ||
            c.Type == ClaimTypes.Email);
        if (emailClaim != null)
        {
            claimsToRemove.Add(emailClaim);
            claimsToAdd.Add(new Claim("email", emailClaim.Value));
        }

        // Mapear sub claim
        var subClaim = identity.FindFirst(c =>
            c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" ||
            c.Type == ClaimTypes.NameIdentifier);
        if (subClaim != null)
        {
            claimsToRemove.Add(subClaim);
            claimsToAdd.Add(new Claim("sub", subClaim.Value));
        }

        // Remover claims antigos e adicionar novos
        foreach (var claim in claimsToRemove)
        {
            identity.RemoveClaim(claim);
        }
        foreach (var claim in claimsToAdd)
        {
            identity.AddClaim(claim);
        }
    }
}