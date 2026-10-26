using System.Security.Claims;
using Baymont.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Baymont.Infrastructure.Identity;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? UserName => User?.Identity?.Name;
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public bool IsInRole(string role) => User?.IsInRole(role) == true;
}
