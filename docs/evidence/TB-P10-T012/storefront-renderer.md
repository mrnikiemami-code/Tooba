# Storefront renderer

`StorefrontLandingSections` is the one dispatcher.
Unknown types render nothing.
Inherits PaletteKey / ThemeMode / ProductCardSkin from Store (no page-local theme).
Home `/` uses selected Published page when HomePageId is eligible; otherwise canonical Home.
