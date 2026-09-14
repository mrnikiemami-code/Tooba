import type { ElementType, HTMLAttributes, ReactNode } from "react";
import {
  surfaceRoleClass,
  type StorefrontAllowedSurface,
} from "../../lib/storefront-appearance/surface-role.ts";

type StorefrontSurfaceProps = {
  surface: StorefrontAllowedSurface;
  as?: ElementType;
  children?: ReactNode;
} & Omit<HTMLAttributes<HTMLElement>, "role">;

/**
 * Canonical storefront surface primitive. Roles are typed; raw color parameters are not accepted.
 */
export function StorefrontSurface({
  surface,
  as: Tag = "div",
  className = "",
  children,
  ...rest
}: StorefrontSurfaceProps) {
  return (
    <Tag
      className={`${surfaceRoleClass(surface)}${className ? ` ${className}` : ""}`}
      data-storefront-surface-role={surface}
      {...rest}
    >
      {children}
    </Tag>
  );
}

export function StorefrontPageSurface(props: Omit<StorefrontSurfaceProps, "surface">) {
  return <StorefrontSurface surface="page" {...props} />;
}

export function StorefrontSectionSurface({
  surface = "section",
  ...props
}: Omit<StorefrontSurfaceProps, "surface"> & { surface?: Extract<StorefrontAllowedSurface, "section" | "alternate" | "accent"> }) {
  return <StorefrontSurface surface={surface} {...props} />;
}

export function StorefrontPanelSurface({
  surface = "card",
  ...props
}: Omit<StorefrontSurfaceProps, "surface"> & { surface?: Extract<StorefrontAllowedSurface, "card" | "elevated" | "overlay"> }) {
  return <StorefrontSurface surface={surface} {...props} />;
}
