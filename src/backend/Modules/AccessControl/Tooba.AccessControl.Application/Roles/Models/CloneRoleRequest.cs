namespace Tooba.AccessControl.Application.Roles.Models;

/// <summary>
/// بدنهٔ ترابری کلون نقش (ورودی HTTP). فرمان CQRS متناظر <c>CloneRoleCommand</c> است؛
/// این نوع فقط شکل ترابری را حمل می‌کند و هرگز به‌عنوان درخواست MediatR استفاده نمی‌شود.
/// </summary>
public sealed record CloneRoleRequest(string Name, string Code, string? Description);
