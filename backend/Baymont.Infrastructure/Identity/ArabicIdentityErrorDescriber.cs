using Microsoft.AspNetCore.Identity;

namespace Baymont.Infrastructure.Identity;

/// <summary>Identity's built-in messages are English; the whole system speaks Arabic.</summary>
public class ArabicIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = "حدث خطأ غير معروف." };
    public override IdentityError ConcurrencyFailure() => new() { Code = nameof(ConcurrencyFailure), Description = "تم تعديل البيانات بواسطة مستخدم آخر. أعد المحاولة." };
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "كلمة المرور غير صحيحة." };
    public override IdentityError InvalidToken() => new() { Code = nameof(InvalidToken), Description = "الرمز غير صالح." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = "اسم المستخدم غير صالح. استخدم حروفاً إنجليزية وأرقاماً فقط." };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = "البريد الإلكتروني غير صالح." };
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = $"اسم المستخدم «{userName}» مستخدم بالفعل." };
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = "هذا البريد مستخدم بالفعل." };
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"كلمة المرور قصيرة. الحد الأدنى {length} أحرف." };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "كلمة المرور يجب أن تحتوي على رمز واحد على الأقل مثل @ أو #." };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "كلمة المرور يجب أن تحتوي على رقم واحد على الأقل." };
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "كلمة المرور يجب أن تحتوي على حرف إنجليزي صغير واحد على الأقل (a-z)." };
    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "كلمة المرور يجب أن تحتوي على حرف إنجليزي كبير واحد على الأقل (A-Z)." };
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"كلمة المرور يجب أن تحتوي على {uniqueChars} أحرف مختلفة على الأقل." };
    public override IdentityError UserAlreadyHasPassword() => new() { Code = nameof(UserAlreadyHasPassword), Description = "المستخدم لديه كلمة مرور بالفعل." };
    public override IdentityError UserLockoutNotEnabled() => new() { Code = nameof(UserLockoutNotEnabled), Description = "قفل الحساب غير مفعّل لهذا المستخدم." };
    public override IdentityError UserAlreadyInRole(string role) => new() { Code = nameof(UserAlreadyInRole), Description = "المستخدم لديه هذا الدور بالفعل." };
    public override IdentityError UserNotInRole(string role) => new() { Code = nameof(UserNotInRole), Description = "المستخدم ليس لديه هذا الدور." };
    public override IdentityError InvalidRoleName(string? role) => new() { Code = nameof(InvalidRoleName), Description = "اسم الدور غير صالح." };
    public override IdentityError DuplicateRoleName(string role) => new() { Code = nameof(DuplicateRoleName), Description = "اسم الدور مستخدم بالفعل." };
}
