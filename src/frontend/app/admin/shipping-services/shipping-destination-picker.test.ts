/**
 * TB-SHIPPING-MVP-DESTINATION-066 — آزمون نگهبان «آخرین درخواست» انتخابگر مقصد.
 *
 * عیب واقعی مشاهده‌شده در 065: هنگام تغییر استان، پاسخ دیررسِ نسل قبلی (شهرهای کل کشور یا شهرهای
 * استان قبلی) هنوز روی وضعیت جاری نوشته می‌شد و گزینه‌های کهنه دیده می‌شد. این آزمون همان مکانیزم
 * را قطعی می‌سنجد: نسل قدیمی همیشه باطل است و تنها نسل آخر می‌تواند بنویسد.
 */
import assert from "node:assert/strict";
import test from "node:test";
import { createLatestRequestGuard } from "./shipping-destination-picker-model.ts";

test("only the newest cascade generation may write state", () => {
  const guard = createLatestRequestGuard();

  const first = guard.begin();
  const second = guard.begin();

  assert.equal(first(), false, "پاسخ نسل قدیمی باید باطل باشد");
  assert.equal(second(), true, "پاسخ نسل آخر باید معتبر باشد");
});

test("a stale area response cannot overwrite the cities of a newer area", async () => {
  const guard = createLatestRequestGuard();
  const rendered: string[] = [];

  // درخواست «استان البرز» نسل ۱ را می‌گشاید و کند است.
  const isCurrentForSlowArea = guard.begin();
  const slow = (async () => {
    await new Promise((resolve) => setTimeout(resolve, 20));
    if (isCurrentForSlowArea()) rendered.push("area-1");
  })();

  // کاربر پیش از رسیدن پاسخ، استان را عوض می‌کند: نسل ۲.
  const isCurrentForFastArea = guard.begin();
  if (isCurrentForFastArea()) rendered.push("area-2");

  await slow;

  assert.deepEqual(rendered, ["area-2"], "فقط پاسخ نسل آخر باید نوشته شود");
});

test("independent guards do not invalidate each other across cascade levels", () => {
  const cascade = createLatestRequestGuard();
  const serviceArea = createLatestRequestGuard();

  const cascadeToken = cascade.begin();
  const serviceAreaToken = serviceArea.begin();

  assert.equal(cascadeToken(), true, "نگهبان ناحیهٔ خدمت نباید نسل استان/شهر را باطل کند");
  assert.equal(serviceAreaToken(), true);
});
