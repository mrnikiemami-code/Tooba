# Source Isolation

Store mode (FASHION_STORE_ORIGIN) never injects FASHION_IMAGES / Template banner assets.
Empty Store banners set previewPlaceholder + empty items.
CompositionBannerGrid uses llowHomeFallback=false in Store preview so MIDDLE_BANNERS cannot leak.

