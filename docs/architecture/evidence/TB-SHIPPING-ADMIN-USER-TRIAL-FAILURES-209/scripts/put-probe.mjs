// TB-209 reproduction probe: sends ONE real editor-document PUT to the live local Host.
// Used with the operator's own draft to prove the ACTUAL server rejection of the screenshot-shaped
// input (fa name + country destination + one fee row + human timezone text). A rejected PUT writes
// nothing; a lawful one is the real save used as evidence.
// Usage: node put-probe.mjs <draftId> <timeZoneId> <expectedRevision>
const [, , draftId, timeZoneId, revisionArg] = process.argv;
const BASE = "http://127.0.0.1:5088";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const FA = "01a06a53-4139-7000-bb9b-3078a0d2e5c4";
const COUNTRY = "e3451206-e503-51a8-91ad-afa9ab70e330";

const days = [1, 2, 3, 4, 5, 6, 7].map((weekday) => ({
  weekday,
  isWorking: weekday <= 5,
  startTime: weekday <= 5 ? "09:00:00" : null,
  endTime: weekday <= 5 ? "17:00:00" : null,
}));

const body = {
  expectedRevision: Number(revisionArg),
  regionGroups: [
    {
      groupId: null,
      areas: [
        { kind: "Country", countryId: COUNTRY, administrativeAreaId: null, cityId: null, serviceAreaId: null },
      ],
      rows: [
        {
          rateRowId: null,
          minWeightGrams: 1000,
          maxWeightGrams: 2000,
          minItemCount: null,
          maxItemCount: null,
          minMerchandiseAmount: null,
          maxMerchandiseAmount: null,
          freightAmount: 120000,
          packagingAmount: 5000,
          minDeliveryDays: 1,
          maxDeliveryDays: 3,
        },
      ],
      titles: [],
    },
  ],
  methodTranslations: [{ languageId: FA, name: "پیشتاز", description: null }],
  weeklySchedule: { timeZoneId, days },
  methodSettings: { freightPaymentMode: "Prepaid", calculationMode: "Manual" },
};

const headers = {
  "Content-Type": "application/json",
  Accept: "application/json",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
};

const response = await fetch(`${BASE}/v1/admin/shipping-services/authoring-drafts/${draftId}/editor-document`, {
  method: "PUT",
  headers,
  body: JSON.stringify(body),
});
const text = await response.text();
console.log(JSON.stringify({ status: response.status, body: text.slice(0, 1200) }, null, 2));
