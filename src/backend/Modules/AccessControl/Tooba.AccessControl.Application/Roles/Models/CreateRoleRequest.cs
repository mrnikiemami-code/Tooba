namespace Tooba.AccessControl.Application.Roles.Models;

/// <summary>
/// بدنهٔ ترابری ایجاد نقش (ورودی HTTP). فرمان CQRS متناظر <c>CreateRoleCommand</c> است؛
/// این نوع فقط شکل ترابری را حمل می‌کند و هرگز به‌عنوان درخواست MediatR استفاده نمی‌شود.
/// </summary>
public sealed record CreateRoleRequest(string Name, string Code, string Description);
