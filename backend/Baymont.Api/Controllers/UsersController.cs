using Baymont.Application.Dtos;
using Baymont.Domain.Constants;
using Baymont.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/users")]
public class UsersController(UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List()
    {
        var users = await userManager.Users.OrderBy(u => u.UserName).ToListAsync();
        var result = new List<UserDto>();
        foreach (var user in users)
            result.Add(await ToDto(user));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
    {
        if (!AppRoles.All.Contains(request.Role))
            return BadRequest(new { message = "الدور غير صالح." });

        var user = new AppUser
        {
            UserName = request.UserName.Trim(),
            DisplayName = request.DisplayName.Trim(),
            IsActive = true,
            MustChangePassword = true,
            EmailConfirmed = true
        };

        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
            return BadRequest(new { message = string.Join(" ", create.Errors.Select(e => e.Description)) });

        await userManager.AddToRoleAsync(user, request.Role);
        return Ok(await ToDto(user));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(string id, [FromBody] UpdateUserRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound(new { message = "المستخدم غير موجود." });

        if (!AppRoles.All.Contains(request.Role))
            return BadRequest(new { message = "الدور غير صالح." });

        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        await userManager.UpdateAsync(user);

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, request.Role);

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!reset.Succeeded)
                return BadRequest(new { message = string.Join(" ", reset.Errors.Select(e => e.Description)) });
            user.MustChangePassword = true;
            await userManager.UpdateAsync(user);
        }

        return Ok(await ToDto(user));
    }

    private async Task<UserDto> ToDto(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.DisplayName,
            roles.ToList(),
            user.IsActive,
            user.MustChangePassword);
    }
}
