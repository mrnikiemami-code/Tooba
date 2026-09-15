# Shared renderer — TB-P10-T019

- `shared-composition-renderer.tsx` exports resolveSharedVariant, renderSharedHomeSection*, renderSharedLandingSection.
- Home: storefront-home.tsx keeps renderHomeSection + case switches calling shared path.
- Landing: thin case switches → adapter → shared.
- Surface roles via wrapWithSurfaceRole / surfaceRoleClass.
- Height presets via heightPresetHeroClass / heightPresetBannerClass.
