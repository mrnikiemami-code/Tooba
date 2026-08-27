import assert from "node:assert/strict";
import test from "node:test";
import {
  CustomerPreferencesApiError,
  customerPreferencesErrorMessage,
  mapCustomerPreferences,
} from "./customer-preferences-api.ts";

test("mapCustomerPreferences accepts camel and Pascal locale", () => {
  assert.equal(mapCustomerPreferences({ locale: "en" })?.locale, "en");
  assert.equal(mapCustomerPreferences({ Locale: "fa-IR" })?.locale, "fa");
  assert.equal(mapCustomerPreferences({}), null);
  assert.equal(mapCustomerPreferences(null), null);
});

test("customer preferences error mapping covers unauthorized", () => {
  assert.match(
    customerPreferencesErrorMessage(new CustomerPreferencesApiError(401, "Unauthorized")),
    /نشست/,
  );
});
