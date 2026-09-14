"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { menuChildren, type StorefrontMenuItem } from "./storefront-menu-api.ts";

export function StorefrontMenuLinks({
  items,
  title,
}: {
  items: StorefrontMenuItem[];
  title?: string;
}) {
  const roots = menuChildren(items, null);
  if (roots.length === 0) return null;
  return (
    <nav className="px-4 py-6" data-testid="storefront-navigation-menu" aria-label={title || "فهرست پیوندها"}>
      {title ? <h2 className="mb-3 text-xl font-black">{title}</h2> : null}
      <ul className="space-y-2">
        {roots.map((item) => (
          <MenuBranch key={item.menuItemId} item={item} items={items} />
        ))}
      </ul>
    </nav>
  );
}

function MenuBranch({ item, items }: { item: StorefrontMenuItem; items: StorefrontMenuItem[] }) {
  const children = menuChildren(items, item.menuItemId);
  return (
    <li>
      {item.href ? (
        <Link href={item.href} className="font-bold text-gray-800 hover:text-primary">{item.label}</Link>
      ) : (
        <span className="font-bold text-gray-800">{item.label}</span>
      )}
      {children.length > 0 ? (
        <ul className="mt-1 mr-4 space-y-1 border-r border-gray-100 pr-3">
          {children.map((child) => (
            <MenuBranch key={child.menuItemId} item={child} items={items} />
          ))}
        </ul>
      ) : null}
    </li>
  );
}
