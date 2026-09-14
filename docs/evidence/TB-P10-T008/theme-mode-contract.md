# TB-P10-T008 — ThemeMode contract

Canonical modes: `LightOnly`, `DarkOnly`, `System`, `UserChoice`.

Legacy persisted `Light`/`Dark` normalize to LightOnly/DarkOnly. Unknown strings rejected on write (`appearance.theme.invalid`) and fall back to LightOnly on read.

Existing Stores with `Light` stay light. No surprise dark rollout.
