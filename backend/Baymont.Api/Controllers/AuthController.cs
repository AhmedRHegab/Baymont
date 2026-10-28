using Baymont.Application.Dtos;
using Baymont.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "اكتب اسم المستخدم وكلمة المرور." });

        var user = await userManager.FindByNameAsync(request.UserName.Trim());
        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "اسم المستخدم أو كلمة المرور غير صحيحة." });

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: true, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Unauthorized(new { message = "تم إيقاف الحساب مؤقتاً بسبب محاولات فاشلة." });

        if (!result.Succeeded)
            return Unauthorized(new { message = "اسم المستخدم أو كلمة المرور غير صحيحة." });

        return Ok(await ToDto(user));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(new { message = "تم تسجيل الخروج." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null || !user.IsActive)
            return Unauthorized();

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
