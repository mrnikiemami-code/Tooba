import assert from "node:assert/strict";
import test from "node:test";

import {
  buildCapturedSelectionIntent,
  captureGroupConfirmationFingerprint,
  capturedSelectionBlocker,
  capturedSelectionComplete,
  capturedSelectionContextKey,
  fingerprintsForCapturedChoices,
  reconcileCapturedChoicesAgainstPreview,
  resolveCapturedSelectionRevision,
} from "./storefront-shipping-captured-selection.ts";
import { resolveShippingContinueState } from "./storefront-shipping-continue-state.ts";
import type {
  StorefrontCapturedPreview,
  StorefrontCapturedPreviewGroup,
} from "./storefront-shipping-api.ts";

/**
 * TB-237-R1 — قصد ثبت انتخاب capture‌شده فقط از پاسخ موفق سرور.
 *
 * این تست قرارداد **ساخت درخواست** را با نام‌های واقعیِ DTO سرور ثابت می‌کند
 * (`expectedSnapshotId`/`expectedSourceRevision`/`expectedSelectionVersion`/`expectedAreaGroupId`/
 * `expectedRateRowId`/`expectedGoodsAmount`)، نه با نام‌های خواندنی `confirmation*`. مبلغ کالا متن
 * دقیق سرور است، قصد ناقص هرگز ارسال نمی‌شود و گروه غیرقابل‌ارسال کل قصد را مسدود می‌کند.
 */
function method(overrides: Record<string, unknown> = {}) {
  return {
    methodLabel: "پست پیشتاز",
    serviceCode: "post",
    optionCode: "standard",
    freightAmountText: "120000.00",
    packagingAmountText: "5000.00",
    payableAtCheckoutAmountText: "125000.00",
    carrierCollectAmountText: "0.00",
    currency: "IRR",
    earliestEligibleDate: "2026-10-29",
    availableDates: [
      { value: "2026-10-29", label: "پنجشنبه", subLabel: "۸ آبان", intervalText: "09:00-17:00", isEarliest: true },
    ],
    unavailableReason: null,
    confirmationSnapshotId: "01a17d0e-0419-7000-b575-f507d2f89abc",
    confirmationSourceRevision: "4294967296",
    confirmationSelectionVersion: "1",
    confirmationAreaGroupId: "01a17d0e-aaaa-7000-b575-f507d2f89abc",
    confirmationRateRowId: "01a17d0e-bbbb-7000-b575-f507d2f89abc",
    ...overrides,
  };
}

function group(overrides: Partial<StorefrontCapturedPreviewGroup> = {}): StorefrontCapturedPreviewGroup {
  return {
    groupKey: "cap|post|store|heavy",
    typeLabel: "سنگین",
    sellerPartyId: "01a030d1-40cb-7000-8abe-6d31739956c5",
    cartLineIds: ["01a17cfd-c1b1-7000-b10d-8cf21258a1cd"],
    finalGoodsAmountText: "9485771",
    methods: [method()],
    unavailableReason: null,
    ...overrides,
  };
}

const choice = { methodKey: "post\u001fstandard", serviceCode: "post", optionCode: "standard", date: "2026-10-29" };

function preview(overrides: Partial<StorefrontCapturedPreview> = {}): StorefrontCapturedPreview {
  return {
    state: "ok",
    enabled: true,
    currency: "IRR",
    groups: [group()],
    unavailableReason: null,
    selectionRevision: null,
    selectionEnabled: true,
    checkoutEnabled: true,
    ...overrides,
  };
}

test("a complete selection carries exactly the server WRITE keys and the exact goods text", () => {
  const intent = buildCapturedSelectionIntent(preview(), { "cap|post|store|heavy": choice });
  assert.ok(intent);
  assert.equal(intent.length, 1);
  const [only] = intent;
  // نام‌های نوشتاری سرور — نه نام‌های خواندنی.
  assert.deepEqual(Object.keys(only).sort(), [
    "expectedAreaGroupId",
    "expectedGoodsAmount",
    "expectedRateRowId",
    "expectedSelectionVersion",
    "expectedSnapshotId",
    "expectedSourceRevision",
    "groupKey",
    "optionCode",
    "selectedDeliveryDate",
    "selectedSlotEndTicks",
    "selectedSlotStartTicks",
    "serviceCode",
  ]);
  assert.equal(only.expectedGoodsAmount, "9485771");
  assert.equal(only.expectedSnapshotId, "01a17d0e-0419-7000-b575-f507d2f89abc");
  assert.equal(only.expectedSourceRevision, "4294967296");
  assert.equal(only.expectedSelectionVersion, "1");
  assert.equal(only.expectedAreaGroupId, "01a17d0e-aaaa-7000-b575-f507d2f89abc");
  assert.equal(only.expectedRateRowId, "01a17d0e-bbbb-7000-b575-f507d2f89abc");
  assert.equal(only.selectedDeliveryDate, "2026-10-29");
  // روزی که نوبت hourly ندارد: هیچ tick حدس زده نمی‌شود و صریحاً null می‌ماند.
  assert.equal(only.selectedSlotStartTicks, null);
  assert.equal(only.selectedSlotEndTicks, null);
  // هیچ shorthand قدیمی در بدنهٔ گروه capture‌شده نیست.
  assert.equal("shippingMethodCode" in only, false);
  assert.equal("cartLineIds" in only, false);
  assert.equal("selectedDeliveryTimeWindow" in only, false);
});

test("a missing/blank/malformed goods echo blocks the whole intent instead of sending a guess", () => {
  for (const goods of [null, "", "  ", "9,485,771", "+9485771", "9485771.0e3"]) {
    const blocked = preview({ groups: [group({ finalGoodsAmountText: goods as string | null })] });
    assert.equal(
      buildCapturedSelectionIntent(blocked, { "cap|post|store|heavy": choice }),
      null,
      `goods=${String(goods)} must not produce an intent`,
    );
    assert.equal(capturedSelectionComplete(blocked, { "cap|post|store|heavy": choice }), false);
  }
});

test("any missing confirmation token blocks the intent (no partial echo is ever sent)", () => {
  for (const token of [
    "confirmationSnapshotId",
    "confirmationSourceRevision",
    "confirmationSelectionVersion",
    "confirmationAreaGroupId",
    "confirmationRateRowId",
  ]) {
    const blocked = preview({ groups: [group({ methods: [method({ [token]: null })] })] });
    assert.equal(
      buildCapturedSelectionIntent(blocked, { "cap|post|store|heavy": choice }),
      null,
      `${token}=null must block`,
    );
    const blank = preview({ groups: [group({ methods: [method({ [token]: "   " })] })] });
    assert.equal(buildCapturedSelectionIntent(blank, { "cap|post|store|heavy": choice }), null, `${token}=blank must block`);
  }
});

test("explicit zero goods is lawful and stays the literal text zero", () => {
  const intent = buildCapturedSelectionIntent(preview({ groups: [group({ finalGoodsAmountText: "0" })] }), {
    "cap|post|store|heavy": { ...choice },
  });
  assert.ok(intent);
  assert.equal(intent[0].expectedGoodsAmount, "0");
});

test("an unavailable server group BLOCKS the request instead of being dropped from a partial cart", () => {
  const withUnavailable = preview({
    groups: [
      group(),
      group({
        groupKey: "cap|post|seller|heavy",
        sellerPartyId: "01a030d1-40cb-7000-8abe-6d3173995600",
        cartLineIds: ["01a17cfd-c1b1-7000-b10d-8cf21258a1ff"],
        methods: [],
        unavailableReason: "storefront.captured_preview.unassigned",
      }),
    ],
  });
  const choices = { "cap|post|store|heavy": choice };
  assert.equal(capturedSelectionBlocker(withUnavailable, choices), "unavailable_group");
  assert.equal(capturedSelectionComplete(withUnavailable, choices), false);
  assert.equal(buildCapturedSelectionIntent(withUnavailable, choices), null);
});

test("no method chosen, a date outside that method, or no preview at all never builds a request", () => {
  assert.equal(buildCapturedSelectionIntent(preview(), {}), null);
  assert.equal(
    buildCapturedSelectionIntent(preview(), { "cap|post|store|heavy": { ...choice, date: "2026-11-30" } }),
    null,
  );
  assert.equal(buildCapturedSelectionIntent(null, { "cap|post|store|heavy": choice }), null);
  assert.equal(
    buildCapturedSelectionIntent(preview({ state: "untrusted", groups: [] }), { "cap|post|store|heavy": choice }),
    null,
  );
  assert.equal(buildCapturedSelectionIntent(preview({ enabled: false, groups: [] }), {}), null);
});

test("two owners sharing the same binding keep independent choices and goods", () => {
  const other = group({
    groupKey: "cap|post|seller|heavy",
    sellerPartyId: "01a030d1-40cb-7000-8abe-6d3173995600",
    cartLineIds: ["01a17cfd-c1b1-7000-b10d-8cf21258a1ff"],
    finalGoodsAmountText: "198",
  });
  const intent = buildCapturedSelectionIntent(preview({ groups: [group(), other] }), {
    "cap|post|store|heavy": choice,
    "cap|post|seller|heavy": choice,
  });
  assert.ok(intent);
  assert.equal(intent.length, 2);
  assert.deepEqual(intent.map((entry) => entry.expectedGoodsAmount), ["9485771", "198"]);
  assert.deepEqual(intent.map((entry) => entry.groupKey), ["cap|post|store|heavy", "cap|post|seller|heavy"]);
});

test("any destination, coupon, cart-version or confirmed-identity change invalidates confirmations", () => {
  const base = { destinationKey: "d1", couponCode: null, cartVersion: 7, preview: preview() };
  const key = capturedSelectionContextKey(base);
  assert.equal(capturedSelectionContextKey({ ...base }), key);
  assert.notEqual(capturedSelectionContextKey({ ...base, destinationKey: "d2" }), key);
  assert.notEqual(capturedSelectionContextKey({ ...base, couponCode: "SAVE10" }), key);
  assert.notEqual(capturedSelectionContextKey({ ...base, cartVersion: 8 }), key);
  // تغییر بازتاب سرور (مثلاً بازبینی مبدأ) یا مبلغ کالا، حتی با همان snapshot، تأیید را باطل می‌کند.
  assert.notEqual(
    capturedSelectionContextKey({
      ...base,
      preview: preview({ groups: [group({ methods: [method({ confirmationSourceRevision: "3" })] })] }),
    }),
    key,
  );
  assert.notEqual(
    capturedSelectionContextKey({ ...base, preview: preview({ groups: [group({ finalGoodsAmountText: "1" })] }) }),
    key,
  );
  assert.notEqual(capturedSelectionContextKey({ ...base, preview: null }), key);
});

/**
 * TB-237-R1 — نسخهٔ ریشهٔ CAS فقط از سرور.
 *
 * سناریوی واقعی: مشتری به صفحهٔ ارسال برمی‌گردد، نمای قدیمی `draft` غایب است (مسیر بازیابی) ولی
 * ریشهٔ capture‌شده با نسخهٔ `1` ذخیره مانده. فرستادن `0` در آن حالت `storefront.captured_selection.stale`
 * می‌سازد و ذخیرهٔ درست را رد می‌کند؛ پس نسخهٔ `capturedPreview.selectionRevision` همان مقدار
 * اثبات‌شدهٔ سرور است. هیچ نسخه‌ای حدس زده نمی‌شود و «0» تنها یعنی ریشه اثباتاً غایب.
 */
test("the captured root revision comes from the server, never an invented zero", () => {
  assert.equal(resolveCapturedSelectionRevision({ draftRevision: "1", previewRevision: "2" }), "1");
  assert.equal(
    resolveCapturedSelectionRevision({ draftRevision: null, previewRevision: "1" }),
    "1",
    "a missing legacy draft view must not downgrade a stored root to zero",
  );
  assert.equal(resolveCapturedSelectionRevision({ draftRevision: null, previewRevision: "0" }), "0");
  assert.equal(resolveCapturedSelectionRevision({ draftRevision: undefined, previewRevision: "4294967296" }), "4294967296");
  assert.equal(resolveCapturedSelectionRevision({ draftRevision: " 1", previewRevision: null }), null);
  assert.equal(resolveCapturedSelectionRevision({ draftRevision: "-1", previewRevision: "1.0" }), null);
  assert.equal(resolveCapturedSelectionRevision({ draftRevision: null, previewRevision: null }), null);
  assert.equal(resolveCapturedSelectionRevision({}), null);
});


/**
 * TB-241 — نوبت‌های ساعتی ارسال.
 *
 * وقتی نویسنده برای روش نوبت (مثلاً ۶۰ دقیقه) تنظیم کرده باشد، سرور برای همان روز چند نوبت می‌فرستد.
 * آن‌وقت انتخاب «روز» به‌تنهایی کامل نیست: فقط نوبتی که **در همان پاسخ** دیده شده می‌تواند به‌عنوان
 * tick شروع/پایان ارسال شود. هیچ نوبتی ساخته یا گرد نمی‌شود.
 */
const SLOT_DAY = {
  value: "2026-10-29",
  label: "پنجشنبه",
  subLabel: "۸ آبان",
  intervalText: "09:00-11:00",
  isEarliest: true,
  slots: [
    { slotKey: "09:00-10:00", label: "۰۹:۰۰–۱۰:۰۰", startTicks: 324000000000, endTicks: 360000000000 },
    { slotKey: "10:00-11:00", label: "۱۰:۰۰–۱۱:۰۰", startTicks: 360000000000, endTicks: 396000000000 },
  ],
};

const SLOT_CHOICE = { ...choice, slotStartTicks: 360000000000, slotEndTicks: 396000000000 };

function slotPreview(): StorefrontCapturedPreview {
  return preview({ groups: [group({ methods: [method({ availableDates: [SLOT_DAY] })] })] });
}

test("a multi-slot day blocks continuation until one real slot is chosen", () => {
  assert.equal(capturedSelectionComplete(slotPreview(), { "cap|post|store|heavy": choice }), false);
  assert.equal(
    capturedSelectionBlocker(slotPreview(), { "cap|post|store|heavy": choice }),
    "slot_required",
  );
  assert.equal(buildCapturedSelectionIntent(slotPreview(), { "cap|post|store|heavy": choice }), null);
  assert.equal(capturedSelectionComplete(slotPreview(), { "cap|post|store|heavy": SLOT_CHOICE }), true);
});

test("the chosen slot ticks are sent verbatim from the server response", () => {
  const intent = buildCapturedSelectionIntent(slotPreview(), { "cap|post|store|heavy": SLOT_CHOICE });
  assert.ok(intent);
  assert.equal(intent[0].selectedSlotStartTicks, 360000000000);
  assert.equal(intent[0].selectedSlotEndTicks, 396000000000);
});

test("an invented or foreign slot is refused instead of being normalised", () => {
  for (const bad of [
    { slotStartTicks: 0, slotEndTicks: 0 },
    { slotStartTicks: 360000000000, slotEndTicks: 324000000000 },
    { slotStartTicks: null, slotEndTicks: 396000000000 },
  ]) {
    assert.equal(
      buildCapturedSelectionIntent(slotPreview(), { "cap|post|store|heavy": { ...choice, ...bad } }),
      null,
      JSON.stringify(bad),
    );
  }
});

test("changing the offered slots invalidates a previously confirmed selection", () => {
  const base = {
    destinationKey: "d|1",
    couponCode: null,
    cartVersion: 7,
    preview: slotPreview(),
  };
  const key = capturedSelectionContextKey(base);
  const changedSlots = preview({
    groups: [
      group({
        methods: [
          method({
            availableDates: [
              {
                ...SLOT_DAY,
                slots: [{ slotKey: "09:00-10:00", label: "۰۹:۰۰–۱۰:۰۰", startTicks: 324000000000, endTicks: 360000000000 }],
              },
            ],
          }),
        ],
      }),
    ],
  });
  assert.notEqual(capturedSelectionContextKey({ ...base, preview: changedSlots }), key);
});

/**
 * TB-244-R3 — تکمیل فیلدهای گیرنده نباید انتخاب روز/ساعت را پاک کند.
 *
 * ریشهٔ اندازه‌گیری‌شده: تغییر هویت context همهٔ `capturedChoices` را `{}` می‌کرد حتی وقتی همان
 * تاریخ/نوبت هنوز در تقویم تازه بود (مثلاً پس از تکمیل مقصد Geography یا refresh هم‌معنا).
 * گیرنده/موبایل/کدپستی اصلاً در کلید context نیستند؛ Continue فقط blocker می‌دهد و state را عوض نمی‌کند.
 */
const SLOT_CHOICE_FULL = {
  methodKey: "post\u001fstandard",
  serviceCode: "post",
  optionCode: "standard",
  date: "2026-10-29",
  slotStartTicks: 360000000000,
  slotEndTicks: 396000000000,
};

const routing = { destinationKey: "d1", couponCode: null as string | null, cartVersion: 7 };

function reconcile(
  previous: Record<string, typeof SLOT_CHOICE_FULL>,
  nextPreview: StorefrontCapturedPreview,
  opts: {
    previousRouting?: typeof routing | null;
    nextRouting?: typeof routing;
    previousFingerprints?: Record<string, string>;
  } = {},
) {
  const previousFingerprints =
    opts.previousFingerprints
    ?? fingerprintsForCapturedChoices(slotPreview(), previous);
  return reconcileCapturedChoicesAgainstPreview(previous, nextPreview, {
    previousRouting: opts.previousRouting === undefined ? routing : opts.previousRouting,
    nextRouting: opts.nextRouting ?? routing,
    previousFingerprints,
  });
}

test("recipient-form fields are outside the captured context identity", () => {
  const base = { destinationKey: "d1", couponCode: null, cartVersion: 7, preview: slotPreview() };
  const key = capturedSelectionContextKey(base);
  assert.equal(capturedSelectionContextKey({ ...base }), key);
  assert.equal(
    key.includes("firstName") || key.includes("postalCode") || key.includes("notes"),
    false,
  );
});

test("semantically identical re-fetch preserves choices via confirmation fingerprints", () => {
  const previous = { "cap|post|store|heavy": SLOT_CHOICE_FULL };
  const kept = reconcile(previous, slotPreview());
  assert.equal(kept.choices, previous);
  assert.equal(kept.choices["cap|post|store|heavy"].slotStartTicks, 360000000000);
});

test("coupon and cart-version changes clear all choices; destination alone keeps identical fingerprints", () => {
  const previous = { "cap|post|store|heavy": SLOT_CHOICE_FULL };
  for (const nextRouting of [
    { ...routing, couponCode: "SAVE10" },
    { ...routing, cartVersion: 9 },
  ]) {
    const cleared = reconcile(previous, slotPreview(), { nextRouting });
    assert.deepEqual(cleared.choices, {});
    assert.deepEqual(cleared.fingerprints, {});
  }
  const destKept = reconcile(previous, slotPreview(), {
    nextRouting: { ...routing, destinationKey: "d2" },
  });
  assert.equal(destKept.choices["cap|post|store|heavy"]?.date, "2026-10-29");
  assert.equal(destKept.choices["cap|post|store|heavy"]?.slotStartTicks, 360000000000);
});

test("destination revalidation keeps provisional choices while preview is incomplete or empty", () => {
  const previous = { "cap|post|store|heavy": SLOT_CHOICE_FULL };
  const fingerprints = fingerprintsForCapturedChoices(slotPreview(), previous);
  const pending = reconcileCapturedChoicesAgainstPreview(previous, null, {
    previousRouting: routing,
    nextRouting: { ...routing, destinationKey: "d2" },
    previousFingerprints: fingerprints,
  });
  assert.equal(pending.choices, previous);
  assert.deepEqual(pending.fingerprints, fingerprints);

  const emptyOk = preview({ groups: [], unavailableReason: "storefront.captured_preview.destination_required" });
  const mid = reconcileCapturedChoicesAgainstPreview(previous, emptyOk, {
    previousRouting: routing,
    nextRouting: { ...routing, destinationKey: "IR|teh|" },
    previousFingerprints: fingerprints,
  });
  assert.equal(mid.choices["cap|post|store|heavy"]?.date, "2026-10-29");
});

test("snapshot/rate/goods fingerprint change clears only the affected group", () => {
  const otherKey = "cap|post|store|light";
  const previous = {
    "cap|post|store|heavy": SLOT_CHOICE_FULL,
    [otherKey]: { ...SLOT_CHOICE_FULL },
  };
  const twoGroupPreview = preview({
    groups: [
      group({ methods: [method({ availableDates: [SLOT_DAY] })] }),
      group({ groupKey: otherKey, methods: [method({ availableDates: [SLOT_DAY] })] }),
    ],
  });
  const fingerprints = fingerprintsForCapturedChoices(twoGroupPreview, previous);
  const changedHeavy = preview({
    groups: [
      group({
        finalGoodsAmountText: "1",
        methods: [method({ availableDates: [SLOT_DAY] })],
      }),
      group({ groupKey: otherKey, methods: [method({ availableDates: [SLOT_DAY] })] }),
    ],
  });
  const next = reconcileCapturedChoicesAgainstPreview(previous, changedHeavy, {
    previousRouting: routing,
    nextRouting: routing,
    previousFingerprints: fingerprints,
  });
  assert.equal(next.choices["cap|post|store|heavy"], undefined);
  assert.equal(next.choices[otherKey]?.date, "2026-10-29");
});

test("reconcile clears only the group whose slot disappeared", () => {
  const otherKey = "cap|post|store|light";
  const previous = {
    "cap|post|store|heavy": SLOT_CHOICE_FULL,
    [otherKey]: { ...SLOT_CHOICE_FULL, date: "2026-10-30" },
  };
  const basePreview = preview({
    groups: [
      group({ methods: [method({ availableDates: [SLOT_DAY] })] }),
      group({
        groupKey: otherKey,
        methods: [
          method({
            availableDates: [{ ...SLOT_DAY, value: "2026-10-30" }],
          }),
        ],
      }),
    ],
  });
  const fingerprints = fingerprintsForCapturedChoices(basePreview, previous);
  const refreshed = preview({
    groups: [
      group({ methods: [method({ availableDates: [SLOT_DAY] })] }),
      group({
        groupKey: otherKey,
        methods: [method({ availableDates: [SLOT_DAY] })],
      }),
    ],
  });
  const next = reconcileCapturedChoicesAgainstPreview(previous, refreshed, {
    previousRouting: routing,
    nextRouting: routing,
    previousFingerprints: fingerprints,
  });
  assert.equal(next.choices["cap|post|store|heavy"]?.date, "2026-10-29");
  assert.equal(next.choices[otherKey], undefined);
});

test("reconcile drops a slot that left the server calendar", () => {
  const previous = { "cap|post|store|heavy": SLOT_CHOICE_FULL };
  const onlyMorning = preview({
    groups: [
      group({
        methods: [
          method({
            availableDates: [
              {
                ...SLOT_DAY,
                slots: [{ slotKey: "09:00-10:00", label: "۰۹:۰۰–۱۰:۰۰", startTicks: 324000000000, endTicks: 360000000000 }],
              },
            ],
          }),
        ],
      }),
    ],
  });
  assert.deepEqual(reconcile(previous, onlyMorning).choices, {});
});

test("zero-slot day rejects a choice carrying ticks and accepts only a tickless legacy choice", () => {
  const tickless = { ...SLOT_CHOICE_FULL, slotStartTicks: null, slotEndTicks: null };
  const zeroSlotDay = {
    value: "2026-10-29",
    label: "پنجشنبه",
    subLabel: "۸ آبان",
    intervalText: "09:00-17:00",
    isEarliest: true,
    slots: [],
  };
  const zeroPreview = preview({
    groups: [group({ methods: [method({ availableDates: [zeroSlotDay] })] })],
  });
  assert.equal(
    captureGroupConfirmationFingerprint(zeroPreview.groups[0]!, SLOT_CHOICE_FULL),
    null,
  );
  const legacyFp = captureGroupConfirmationFingerprint(zeroPreview.groups[0]!, tickless);
  assert.ok(legacyFp);
  const kept = reconcileCapturedChoicesAgainstPreview(
    { "cap|post|store|heavy": tickless },
    zeroPreview,
    {
      previousRouting: routing,
      nextRouting: routing,
      previousFingerprints: { "cap|post|store|heavy": legacyFp! },
    },
  );
  assert.equal(kept.choices["cap|post|store|heavy"]?.date, "2026-10-29");
  const dropped = reconcile(
    { "cap|post|store|heavy": SLOT_CHOICE_FULL },
    zeroPreview,
    {
      previousFingerprints: fingerprintsForCapturedChoices(slotPreview(), {
        "cap|post|store|heavy": SLOT_CHOICE_FULL,
      }),
    },
  );
  assert.deepEqual(dropped.choices, {});
});

test("Continue blockers for missing recipient fields do not require clearing choices", () => {
  const choices = { "cap|post|store|heavy": SLOT_CHOICE_FULL };
  for (const incomplete of [
    { recipient: false, contactMobile: true, province: true, city: true, postalAddress: true, postalCode: true },
    { recipient: true, contactMobile: false, province: true, city: true, postalAddress: true, postalCode: true },
    { recipient: true, contactMobile: true, province: true, city: true, postalAddress: true, postalCode: false },
  ]) {
    const state = resolveShippingContinueState({
      address: incomplete,
      captured: { active: true, confirmed: true, preview: slotPreview(), choices },
      legacy: { available: false, methodReady: false, scheduleReady: false },
    });
    assert.equal(state.ready, false);
    assert.ok(state.blocker);
    assert.equal(choices["cap|post|store|heavy"].date, "2026-10-29");
    assert.equal(choices["cap|post|store|heavy"].slotStartTicks, 360000000000);
  }
});
