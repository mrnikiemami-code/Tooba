# Storefront integration

GET /v1/storefront/header-menu returns usesFallback + items.
Unset/invalid HeaderMenuId => existing mega-menu/category fallback.
Selected enabled menu => one tree in Header (desktop + mobile) and NavigationMenu landing section via StorefrontMenuLinks.
Palette/Theme inherit from Store; no menu-local theme.
