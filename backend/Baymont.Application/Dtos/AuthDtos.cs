namespace Baymont.Application.Dtos;

public record LoginRequest(string UserName, string Password);

public record UserDto(
    string Id,
    string UserName,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    bool MustChangePassword);

public record CreateUserRequest(
    string UserName,
    string DisplayName,
    string Password,
    string Role);

public record UpdateUserRequest(
    string DisplayName,
    string Role,
    bool IsActive,
    string? NewPassword);
