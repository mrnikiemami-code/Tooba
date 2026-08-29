/**
 * قوانین فعال‌بودن آیتم ناوبری Admin — بدون وابستگی به React.
 */

export type AdminNavMatchItem = {
  id?: string;
  href: string;
  exact?: boolean;
};

/**
 * فقط برگ واقعی ناوبری فعال است؛ queryهای sibling (مثل create=1) اولویت دارند
 * و sibling قبلی با prefix مشترک فعال نمی‌ماند.
 */
export function isActiveAdminNavItem(
  pathname: string,
  search: string,
  item: AdminNavMatchItem,
  siblings: AdminNavMatchItem[] = [],
): boolean {
  const hrefPath = item.href.split("?")[0] ?? item.href;
  const hrefQuery = item.href.includes("?") ? (item.href.split("?")[1] ?? "") : "";
  const current = new URLSearchParams(search.startsWith("?") ? search.slice(1) : search);

  if (hrefQuery) {
    const required = new URLSearchParams(hrefQuery);
    for (const [key, value] of required.entries()) {
      if (current.get(key) !== value) return false;
    }
    return pathname === hrefPath;
  }

  const pathMatches = item.exact || hrefPath === "/admin"
    ? pathname === hrefPath
    : pathname === hrefPath || pathname.startsWith(`${hrefPath}/`);
  if (!pathMatches) return false;

  for (const sibling of siblings) {
    if (sibling.href === item.href) continue;
    const siblingPath = sibling.href.split("?")[0] ?? sibling.href;
    if (siblingPath !== hrefPath) continue;
    if (!sibling.href.includes("?")) continue;
    if (isActiveAdminNavItem(pathname, search, sibling, [])) return false;
  }

  for (const sibling of siblings) {
    if (sibling.href === item.href) continue;
    const siblingPath = sibling.href.split("?")[0] ?? sibling.href;
    if (siblingPath === hrefPath) continue;
    if (!siblingPath.startsWith(`${hrefPath}/`)) continue;
    const siblingMatches = sibling.exact
      ? pathname === siblingPath
      : pathname === siblingPath || pathname.startsWith(`${siblingPath}/`);
    if (siblingMatches) return false;
  }

  return true;
}
