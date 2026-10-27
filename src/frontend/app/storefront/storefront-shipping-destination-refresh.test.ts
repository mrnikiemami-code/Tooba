import assert from "node:assert/strict";
import test from "node:test";

import {
  beginDestinationRefresh,
  canRetryDestinationRefresh,
  completeDestinationRefresh,
  createDestinationRefreshSnapshot,
  createRequestGeneration,
  destinationIdentityKey,
  failDestinationRefresh,
  planDestinationRefresh,
  resolveDestinationPreviewMode,
  type DestinationRefreshSnapshot,
} from "./storefront-shipping-destination-refresh.ts";

const destination = {
  countryId: "01a17000-0000-7000-8000-0000000000aa",
  administrativeAreaId: "01a17000-0000-7000-8000-0000000000bb" as string | null,
  cityId: "01a17000-0000-7000-8000-0000000000cc" as string | null,
};
const deeper = { ...destination, cityId: "01a17000-0000-7000-8000-0000000000dd" };

/** فراخوانی همان تصمیم صفحه: پلن برای هویت فعلی با وضعیت فعلی هماهنگ‌کننده. */
function plan(
  snapshot: DestinationRefreshSnapshot,
  key: string,
  current: typeof destination | null = destination,
  capturedActive = true,
) {
  return planDestinationRefresh({
    capturedActive,
    snapshot,
    destinationKey: key,
    destination: current,
  });
}

test("destination transition requests exactly the verified current identifiers", () => {
  const empty = createDestinationRefreshSnapshot();
  const first = plan(empty, destinationIdentityKey(destination));
  assert.equal(first.action, "request");
  assert.equal(first.key, destinationIdentityKey(destination));
  assert.deepEqual(first.destination, destination);
  assert.equal(first.destination?.cityId, destination.cityId);
  // تازه‌سازی مقصد هرگز ورودی تایپ‌شدهٔ گیرنده/آدرس را بازنویسی نمی‌کند.
  assert.equal(first.preserveTypedAddress, true);

  // A deeper verification (city arrives after the area) is a NEW identity → its own request.
  const afterFirst = completeDestinationRefresh(
    beginDestinationRefresh(empty, destinationIdentityKey(destination)),
    destinationIdentityKey(destination),
  );
  const second = plan(afterFirst, destinationIdentityKey(deeper), deeper);
  assert.equal(second.action, "request");
  assert.equal(second.destination?.cityId, deeper.cityId);
});

test("unrelated renders and legacy OFF never issue a request", () => {
  const key = destinationIdentityKey(destination);
  const confirmed = completeDestinationRefresh(beginDestinationRefresh(createDestinationRefreshSnapshot(), key), key);
  assert.equal(plan(confirmed, key).action, "skip");
  // Nothing pending, same key, still no request on any later render.
  assert.equal(plan(confirmed, key).action, "skip");
  // Legacy (captured preview OFF) is untouched even when the destination changes.
  assert.equal(plan(createDestinationRefreshSnapshot(), key, destination, false).action, "skip");
});

test("clearing a parent clears the lower identity and removes the stale price", () => {
  assert.equal(destinationIdentityKey(null), "");
  // Parent area cleared: the city disappears with it, so the identity really is country-only.
  const cleared = { countryId: destination.countryId, administrativeAreaId: null, cityId: null };
  assert.equal(destinationIdentityKey(cleared), `${destination.countryId}||`);
  assert.notEqual(destinationIdentityKey(cleared), destinationIdentityKey(destination));

  const priced = completeDestinationRefresh(
    beginDestinationRefresh(createDestinationRefreshSnapshot(), destinationIdentityKey(destination)),
    destinationIdentityKey(destination),
  );
  // قیمت موجود برای هویت قدیمی «جاری» است تا وقتی مقصد عوض شود…
  assert.equal(resolveDestinationPreviewMode(priced, destinationIdentityKey(destination)), "current");

  const clearedPlan = plan(priced, destinationIdentityKey(cleared), cleared);
  assert.equal(clearedPlan.action, "request");
  assert.equal(clearedPlan.destination?.administrativeAreaId, null);
  assert.equal(clearedPlan.destination?.cityId, null);

  // …و به‌محض تغییر هویت، قیمت قبلی جاری نیست (در انتظار پاسخ تازه، پنهان).
  const pending = beginDestinationRefresh(priced, clearedPlan.key);
  assert.equal(resolveDestinationPreviewMode(pending, destinationIdentityKey(cleared)), "pending");

  // Everything cleared → one request without any destination identity (هویت قبلی قیمت‌دار هنوز در
  // وضعیت تأییدشده است، پس پاک‌شدن کامل یک درخواست تازه و بدون مقصد می‌سازد).
  const empty = plan(priced, "", null);
  assert.equal(empty.action, "request");
  assert.equal(empty.destination, null);

  // Repeated clear (already confirmed) does not loop.
  const confirmedEmpty = completeDestinationRefresh(
    beginDestinationRefresh(createDestinationRefreshSnapshot(), ""),
    "",
  );
  assert.equal(plan(confirmedEmpty, "", null).action, "skip");
});

test("a failed refresh for a NEW destination never lets the old price look current", () => {
  // 1) مقصد قبلی قیمت‌دار و تأییدشده.
  const pricedKey = destinationIdentityKey(destination);
  const priced = completeDestinationRefresh(
    beginDestinationRefresh(createDestinationRefreshSnapshot(), pricedKey),
    pricedKey,
  );
  assert.equal(resolveDestinationPreviewMode(priced, pricedKey), "current");

  // 2) تغییر مقصد → درخواست → شکست.
  const newKey = destinationIdentityKey(deeper);
  const requested = beginDestinationRefresh(priced, newKey);
  assert.equal(resolveDestinationPreviewMode(requested, newKey), "pending");

  const failed = failDestinationRefresh(requested, newKey);
  assert.equal(failed.inFlight, false);
  assert.equal(failed.requestedKey, "");
  assert.equal(failed.confirmedKey, pricedKey);
  assert.equal(failed.failedKey, newKey);
  // هویت فعلی همان کلید شکست‌خورده است: قیمت قدیمی هرگز «جاری» نمی‌شود.
  assert.equal(resolveDestinationPreviewMode(failed, newKey), "failed");

  // 3) رندر نامرتبط پس از شکست، هیچ درخواست خودکاری نمی‌سازد.
  assert.equal(plan(failed, newKey, deeper).action, "skip");
  assert.equal(canRetryDestinationRefresh(failed, newKey), true);
});

test("one explicit retry of the SAME failed key succeeds and clears the failed latch", () => {
  const pricedKey = destinationIdentityKey(destination);
  const newKey = destinationIdentityKey(deeper);
  const failed = failDestinationRefresh(
    beginDestinationRefresh(
      completeDestinationRefresh(
        beginDestinationRefresh(createDestinationRefreshSnapshot(), pricedKey),
        pricedKey,
      ),
      newKey,
    ),
    newKey,
  );

  // Retry فقط برای همان کلید مجاز است (نه برای هویت دیگر).
  assert.equal(canRetryDestinationRefresh(failed, pricedKey), false);
  assert.equal(canRetryDestinationRefresh(failed, newKey), true);

  const retried = beginDestinationRefresh(failed, newKey);
  // قفل شکست همان کلید در همین لحظه برداشته می‌شود تا کلیک/رندر دوم درخواست تکراری نسازد.
  assert.equal(retried.failedKey, "");
  assert.equal(retried.inFlight, true);
  assert.equal(canRetryDestinationRefresh(retried, newKey), false);
  assert.equal(plan(retried, newKey, deeper).action, "skip");

  const succeeded = completeDestinationRefresh(retried, newKey);
  assert.equal(succeeded.confirmedKey, newKey);
  assert.equal(succeeded.failedKey, "");
  assert.equal(succeeded.inFlight, false);
  assert.equal(resolveDestinationPreviewMode(succeeded, newKey), "current");
  // و بازگشت به مقصد قبلی هنوز همان پاسخ تأییدشدهٔ خودش را دارد.
  assert.equal(resolveDestinationPreviewMode(succeeded, pricedKey), "pending");
});

test("stale success and stale failure can never mutate the current request", () => {
  const pricedKey = destinationIdentityKey(destination);
  const newKey = destinationIdentityKey(deeper);
  const inFlight = beginDestinationRefresh(
    completeDestinationRefresh(
      beginDestinationRefresh(createDestinationRefreshSnapshot(), pricedKey),
      pricedKey,
    ),
    newKey,
  );

  // خطای دیررسِ درخواست قبلی: هیچ اثری روی درخواست جاری ندارد.
  const staleFailure = failDestinationRefresh(inFlight, pricedKey);
  assert.equal(staleFailure, inFlight);
  assert.equal(staleFailure.inFlight, true);
  assert.equal(staleFailure.failedKey, "");
  assert.equal(resolveDestinationPreviewMode(staleFailure, newKey), "pending");

  // موفقیت دیررس درخواست قبلی هم هویت تأییدشده را جابه‌جا نمی‌کند.
  const staleSuccess = completeDestinationRefresh(inFlight, pricedKey);
  assert.equal(staleSuccess, inFlight);
  assert.equal(staleSuccess.confirmedKey, pricedKey);
  assert.equal(staleSuccess.inFlight, true);

  // فقط پاسخِ همان درخواست معتبر است.
  const fresh = completeDestinationRefresh(inFlight, newKey);
  assert.equal(fresh.confirmedKey, newKey);
  assert.equal(resolveDestinationPreviewMode(fresh, newKey), "current");
});

test("stale/out-of-order completions are ignored by the request generation", () => {
  const generation = createRequestGeneration();
  const first = generation.next();
  const second = generation.next();
  assert.equal(generation.isCurrent(first), false);
  assert.equal(generation.isCurrent(second), true);
  // The stale one cannot clear the pending state or replace the current projection.
  assert.equal(generation.isCurrent(first), false);
});
