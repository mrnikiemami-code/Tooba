# 18 — Effective access

- Shows permission title (localized), source role name, scope type + `ScopeDisplayName` / resource name.
- Example shape: «مدیریت سفارش · نقش: … · محدوده: دسته: «موبایل»».
- Does not present `CategoryId = UUID` as primary label (unnamed scoped → «منبع بدون نام»).

Proof: `data-testid=effective-access`.
