using Baymont.Application.Common;
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
        var userName = (request.UserName ?? string.Empty).Trim();
        var displayName = (request.DisplayName ?? string.Empty).Trim();
        Check.Required(userName, "اسم المستخدم");
        if (userName.Length < 3 || userName.Length > 50)
            throw new ValidationException("اسم المستخدم يجب أن يكون من 3 إلى 50 حرفاً.");
        Check.Required(displayName, "الاسم الظاهر");
        Check.MaxLength(displayName, 128, "الاسم الظاهر");
        Check.Required(request.Password, "كلمة المرور");
        if (!AppRoles.All.Contains(request.Role))
            throw new ValidationException("الصلاحية المختارة غير صالحة.");

        var user = new AppUser
        {
            UserName = userName,
            DisplayName = displayName,
            IsActive = true,
            MustChangePassword = true,
            EmailConfirmed = true
        };

        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
            return BadRequest(new { message = Describe(create.Errors) });

        await userManager.AddToRoleAsync(user, request.Role);
        return Ok(await ToDto(user));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(string id, [FromBody] UpdateUserRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound(new { message = "المستخدم غير موجود." });

        var displayName = (request.DisplayName ?? string.Empty).Trim();
        Check.Required(displayName, "الاسم الظاهر");
        Check.MaxLength(displayName, 128, "الاسم الظاهر");
        if (!AppRoles.All.Contains(request.Role))
            throw new ValidationException("الصلاحية المختارة غير صالحة.");

        var isMe = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == user.Id;
        if (isMe && !request.IsActive)
            throw new ValidationException("لا يمكنك إيقاف حسابك بنفسك.");

        // Never leave the system without an active admin: nobody could fix anything afterwards.
        var currentRoles = await userManager.GetRolesAsync(user);
        var staysAdmin = request.Role == AppRoles.Admin && request.IsActive;
        if (currentRoles.Contains(AppRoles.Admin) && !staysAdmin)
        {
            var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
            if (!admins.Any(a => a.Id != user.Id && a.IsActive))
                throw new ValidationException("لا يمكن تغيير صلاحية آخر مدير أو إيقاف حسابه. أنشئ مديراً آخر أولاً.");
        }

        // Check the new password first, so a rejected password never leaves the other changes half applied.
        var newPassword = request.NewPassword;
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            var problems = new List<string>();
            foreach (var validator in userManager.PasswordValidators)
            {
                var check = await validator.ValidateAsync(userManager, user, newPassword);
                problems.AddRange(check.Errors.Select(e => e.Description));
            }
            if (problems.Count > 0) throw new ValidationException(WeakPassword);
        }

        user.DisplayName = displayName;
        user.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(newPassword)) user.MustChangePassword = true;
        var saved = await userManager.UpdateAsync(user);
        if (!saved.Succeeded)
            throw new ValidationException(string.Join(" ", saved.Errors.Select(e => e.Description)));

        // Add the new role before removing the old one, so the user is never left without any role.
        if (!currentRoles.Contains(request.Role))
            await userManager.AddToRoleAsync(user, request.Role);
        var others = currentRoles.Where(r => r != request.Role).ToList();
        if (others.Count > 0) await userManager.RemoveFromRolesAsync(user, others);

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await userManager.ResetPasswordAsync(user, token, newPassword);
            if (!reset.Succeeded)
                throw new ValidationException(Describe(reset.Errors));
        }

        return Ok(await ToDto(user));
    }

    private const string WeakPassword =
        "كلمة المرور ضعيفة. يجب أن تكون 8 أحرف على الأقل، وفيها حرف كبير وحرف صغير ورقم ورمز مثل @.";

    /// <summary>One sentence instead of a list: all the password rules are said once.</summary>
    private static string Describe(IEnumerable<IdentityError> errors)
    {
        var list = errors.ToList();
        return list.Any(e => e.Code.StartsWith("Password")) ? WeakPassword : string.Join(" ", list.Select(e => e.Description));
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
