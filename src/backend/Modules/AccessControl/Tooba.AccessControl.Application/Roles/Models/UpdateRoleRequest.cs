namespace Tooba.AccessControl.Application.Roles.Models;

/// <summary>
/// بدنهٔ ترابری به‌روزرسانی نقش (ورودی HTTP). فرمان CQRS متناظر <c>UpdateRoleCommand</c> است؛
/// این نوع فقط شکل ترابری را حمل می‌کند و هرگز به‌عنوان درخواست MediatR استفاده نمی‌شود.
/// </summary>
public sealed record UpdateRoleRequest(string Name, string Description);
