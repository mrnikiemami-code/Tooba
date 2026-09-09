# Global rounding

One row: `catalog.store_quantity_settings` (`StoreQuantitySettings.SingletonId`).
Modes: Floor / Ceiling / Nearest (FA: رو به پایین / رو به بالا / نزدیک‌ترین مقدار).
Host: `GET/PUT /v1/admin/settings/quantity-rounding`.
Admin Settings tab «مقدار». Not a second settings platform.
Changing mode affects subsequent Normalize only; OrderLine snapshots stay.
