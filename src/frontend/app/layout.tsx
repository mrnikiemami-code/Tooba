import type { Metadata } from "next";
import type { ReactNode } from "react";
import { cookies, headers } from "next/headers";
import { AppProviders } from "./providers";
import { LocaleProvider } from "../lib/i18n/locale-context";
import {
  DEFAULT_LOCALE,
  LOCALE_COOKIE_NAME,
  dirForLocale,
  isLocale,
  langForLocale,
  parseLocale,
} from "../lib/i18n/locale";
import { LOCALE_HEADER_NAME } from "../lib/i18n/routing";
import { loadStorefrontAppearance } from "./storefront/storefront-appearance-api.ts";
import { StorefrontProductCardSkinProvider } from "../lib/storefront-appearance/product-card-skin-context.tsx";
import { StorefrontAppearanceProvider } from "../lib/storefront-appearance/storefront-appearance-context.tsx";
import { resolveProductCardSkin } from "../lib/storefront-appearance/product-card-skin.ts";
import { resolveBackgroundStyle } from "../lib/storefront-appearance/background-style.ts";
import {
  THEME_BOOTSTRAP_SCRIPT,
  USER_COLOR_SCHEME_COOKIE,
  parseUserColorScheme,
  resolveEffectiveColorScheme,
  resolveThemeMode,
} from "../lib/storefront-appearance/theme-mode.ts";
import "./globals.css";

/**
 * فرادادهٔ پوسته. Design System مالک تجربه است نه ماژول بک‌اند.
 */
export const metadata: Metadata = {
  title: "Tooba",
  description: "Tooba experience shell",
};

/**
 * لایهٔ ریشه. lang/dir از prefix URL (x-tooba-locale) یا کوکی ترجیحی.
 */
export default async function RootLayout({ children }: { children: ReactNode }) {
  const headerStore = await headers();
  const headerLocale = headerStore.get(LOCALE_HEADER_NAME);
  const jar = await cookies();
  const locale =
    headerLocale && isLocale(headerLocale)
      ? headerLocale
      : parseLocale(jar.get(LOCALE_COOKIE_NAME)?.value) ?? DEFAULT_LOCALE;
  const appearance = await loadStorefrontAppearance();
  const themeMode = resolveThemeMode(appearance.themeMode);
  const userScheme = parseUserColorScheme(jar.get(USER_COLOR_SCHEME_COOKIE)?.value);
  const colorScheme = resolveEffectiveColorScheme(themeMode, userScheme, false);
  const productCardSkin = resolveProductCardSkin(appearance.productCardSkin);
  const backgroundStyle = resolveBackgroundStyle(appearance.backgroundStyle);

  // Appearance CSS vars must NOT land on <html> — that leaked storefront purple into Admin/Seller.
  // Storefront + customer canvases consume tokens via StorefrontAppearanceProvider.
  return (
    <html
      lang={langForLocale(locale)}
      dir={dirForLocale(locale)}
      className={colorScheme === "dark" ? "dark" : undefined}
      suppressHydrationWarning
      data-storefront-palette={appearance.paletteKey}
      data-storefront-scope={appearance.storeScope}
      data-storefront-theme-mode={themeMode}
      data-storefront-color-scheme={colorScheme}
      data-storefront-product-card-skin={productCardSkin}
      data-storefront-background-style={backgroundStyle}
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: THEME_BOOTSTRAP_SCRIPT }} />
      </head>
      <body className="bg-background text-foreground">
        <AppProviders>
          <StorefrontAppearanceProvider appearance={appearance}>
            <StorefrontProductCardSkinProvider skin={productCardSkin}>
              <LocaleProvider locale={locale}>{children}</LocaleProvider>
            </StorefrontProductCardSkinProvider>
          </StorefrontAppearanceProvider>
        </AppProviders>
      </body>
    </html>
  );
}
