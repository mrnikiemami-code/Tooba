// Synthetic, task-owned fixtures matching the ACTUAL current wire contract.
export const IDS = {
  draft: "0a1b2c3d-1111-4222-8333-444455556666",
  type: "9f9e9d9c-2222-4333-8444-555566667777",
  fa: "aaaaaaaa-0000-4111-8222-333344445555",
  en: "bbbbbbbb-0000-4111-8222-333344445556",
  country: "11111111-1111-4111-8111-111111111111",
  country2: "11111111-1111-4111-8111-222222222222",
  area: "22222222-2222-4222-8222-111111111111",
  area2: "22222222-2222-4222-8222-222222222222",
  city: "33333333-3333-4333-8333-111111111111",
  cityNull: "44444444-4444-4444-8444-111111111111",
  service: "55555555-5555-4555-8555-111111111111",
  group1: "77777777-7777-4777-8777-111111111111",
  group2: "77777777-7777-4777-8777-222222222222",
  row1: "88888888-8888-4888-8888-111111111111",
  row2: "88888888-8888-4888-8888-222222222222"
};

const I = IDS;

// Raw JSON text: money must stay exact number tokens (0.50 / 1850000.00).
export const editorDocument = `{
  "draftId": "${I.draft}",
  "owner": { "kind": "Store", "sellerPartyId": null },
  "serviceCode": "SHIP-STD",
  "optionCode": null,
  "revision": 7,
  "createdAt": "2026-10-01T08:00:00Z",
  "updatedAt": "2026-10-20T12:30:00Z",
  "type": {
    "draftId": "${I.draft}",
    "revision": 7,
    "resolution": "Active",
    "shippingTypeId": "${I.type}",
    "shippingType": {
      "shippingTypeId": "${I.type}",
      "code": "STANDARD",
      "sortOrder": 1,
      "revision": 3,
      "translations": [
        { "languageId": "${I.fa}", "name": "ارسال استاندارد", "description": "ارسال عادی" },
        { "languageId": "${I.en}", "name": "Standard shipping", "description": null }
      ]
    }
  },
  "settings": { "freightPaymentMode": "Prepaid", "calculationMode": "Manual" },
  "presentation": {
    "contentLanguages": [
      { "languageId": "${I.fa}", "code": "fa", "culture": "fa-IR", "urlPrefix": "fa", "direction": "rtl", "displayName": "فارسی", "nativeName": "فارسی", "isDefault": true, "sortOrder": 1 },
      { "languageId": "${I.en}", "code": "en", "culture": "en-US", "urlPrefix": "en", "direction": "ltr", "displayName": "انگلیسی", "nativeName": "English", "isDefault": false, "sortOrder": 2 }
    ],
    "defaultContentLanguageId": "${I.fa}",
    "methodTranslations": [
      { "languageId": "${I.fa}", "name": "ارسال استاندارد", "description": "ارسال عادی" },
      { "languageId": "${I.en}", "name": "Standard shipping", "description": null }
    ],
    "groupTitles": [
      { "languageId": "${I.fa}", "title": "گروه تهران" },
      { "languageId": "${I.en}", "title": "Tehran group" }
    ],
    "hasDefaultLanguageName": true
  },
  "weeklySchedule": {
    "timeZoneId": "Asia/Tehran",
    "days": [
      { "weekday": 1, "isWorking": true, "startTime": "09:00:00.500", "endTime": "17:30:00" },
      { "weekday": 2, "isWorking": true, "startTime": "09:00:00", "endTime": "17:00:00" },
      { "weekday": 3, "isWorking": true, "startTime": "09:00:00", "endTime": "17:00:00" },
      { "weekday": 4, "isWorking": true, "startTime": "09:00:00", "endTime": "17:00:00" },
      { "weekday": 5, "isWorking": false, "startTime": null, "endTime": null },
      { "weekday": 6, "isWorking": false, "startTime": null, "endTime": null },
      { "weekday": 7, "isWorking": true, "startTime": "10:00:00", "endTime": "14:00:00" }
    ]
  },
  "regionGroups": [
    {
      "groupId": "${I.group1}",
      "order": 0,
      "currencyCode": "IRR",
      "areas": [
        { "kind": "Country", "id": "${I.country}" },
        { "kind": "AdministrativeArea", "id": "${I.area}" },
        { "kind": "City", "id": "${I.city}" },
        { "kind": "ServiceArea", "id": "${I.service}" }
      ],
      "rows": [
        {
          "rateRowId": "${I.row1}",
          "minWeightGrams": 0,
          "maxWeightGrams": 1000,
          "minItemCount": null,
          "maxItemCount": null,
          "minMerchandiseAmount": 0.50,
          "maxMerchandiseAmount": null,
          "freightAmount": 0.50,
          "packagingAmount": 1850000.00,
          "minDeliveryDays": 1,
          "maxDeliveryDays": 3
        }
      ],
      "titles": [
        { "languageId": "${I.fa}", "title": "گروه تهران" },
        { "languageId": "${I.en}", "title": "Tehran group" }
      ]
    },
    {
      "groupId": "${I.group2}",
      "order": 1,
      "currencyCode": "IRR",
      "areas": [ { "kind": "City", "id": "${I.cityNull}" } ],
      "rows": [
        {
          "rateRowId": "${I.row2}",
          "minWeightGrams": null,
          "maxWeightGrams": null,
          "minItemCount": 1,
          "maxItemCount": 5,
          "minMerchandiseAmount": null,
          "maxMerchandiseAmount": null,
          "freightAmount": 1850000.00,
          "packagingAmount": 0.50,
          "minDeliveryDays": 2,
          "maxDeliveryDays": 7
        }
      ],
      "titles": [ { "languageId": "${I.fa}", "title": "گروه دوم" } ]
    }
  ],
  "destinationFacts": [
    { "kind": "Country", "id": "${I.country}", "resolution": "Resolved", "area": { "kind": "Country", "countryId": "${I.country}", "administrativeAreaId": null, "cityId": null, "serviceAreaId": null }, "label": "ایران" },
    { "kind": "AdministrativeArea", "id": "${I.area}", "resolution": "Resolved", "area": { "kind": "AdministrativeArea", "countryId": "${I.country}", "administrativeAreaId": "${I.area}", "cityId": null, "serviceAreaId": null }, "label": "تهران" },
    { "kind": "City", "id": "${I.city}", "resolution": "Resolved", "area": { "kind": "City", "countryId": "${I.country}", "administrativeAreaId": "${I.area}", "cityId": "${I.city}", "serviceAreaId": null }, "label": "تهران" },
    { "kind": "ServiceArea", "id": "${I.service}", "resolution": "Resolved", "area": { "kind": "ServiceArea", "countryId": "${I.country}", "administrativeAreaId": "${I.area}", "cityId": "${I.city}", "serviceAreaId": "${I.service}" }, "label": "منطقه ۱" },
    { "kind": "City", "id": "${I.cityNull}", "resolution": "Resolved", "area": { "kind": "City", "countryId": "${I.country}", "administrativeAreaId": null, "cityId": "${I.cityNull}", "serviceAreaId": null }, "label": "قم" }
  ]
}`;

const countryOption = (id, code, label, active) =>
  `{ "id": "${id}", "code": "${code}", "label": "${label}", "parentId": null, "isActive": ${active} }`;
const childOption = (id, code, label, parentId, active) =>
  `{ "id": "${id}", "code": "${code}", "label": "${label}", "parentId": "${parentId}", "isActive": ${active} }`;

export const routes = {
  countries: `[ ${countryOption(I.country, "IR", "ایران", true)}, ${countryOption(I.country2, "DE", "آلمان", false)} ]`,
  areas: `[ ${childOption(I.area, "THR", "تهران", I.country, true)}, ${childOption(I.area2, "ESF", "اصفهان", I.country, false)} ]`,
  cities: `[ ${childOption(I.city, "THR-C", "شهر تهران", I.area, true)}, ${childOption(I.cityNull, "QOM", "قم", null, true)} ]`,
  serviceAreas: `[ ${childOption(I.service, "SA-1", "منطقه ۱", I.city, true)} ]`
};
