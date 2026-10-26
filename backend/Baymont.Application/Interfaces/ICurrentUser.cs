namespace Baymont.Application.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
