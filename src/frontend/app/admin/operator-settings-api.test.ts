import assert from "node:assert/strict";
import test from "node:test";
import { mapOperatorPreferences, mapOperatorProfile } from "./operator-settings-api.ts";

test("mapOperatorProfile maps camel and Pascal profile fields", () => {
  const profile = mapOperatorProfile({
    ActorUserId: "admin-1",
    DisplayName: "مدیر نمونه",
    FirstName: "مدیر",
    LastName: "نمونه",
    Bio: "بیو",
    Editable: true,
  });
  assert.ok(profile);
  assert.equal(profile!.actorUserId, "admin-1");
  assert.equal(profile!.displayName, "مدیر نمونه");
  assert.equal(profile!.firstName, "مدیر");
  assert.equal(profile!.bio, "بیو");
  assert.equal(profile!.editable, true);
});

test("mapOperatorPreferences normalizes locale variants", () => {
  assert.equal(mapOperatorPreferences({ locale: "en" })?.locale, "en");
  assert.equal(mapOperatorPreferences({ Locale: "fa_IR" })?.locale, "fa");
  assert.equal(mapOperatorPreferences({}), null);
});

test("mapOperatorProfile accepts Host payload without actorUserId", () => {
  const profile = mapOperatorProfile({ displayName: "اپراتور نمایشی توبا", firstName: "اپراتور", lastName: "نمایشی" });
  assert.ok(profile);
  assert.equal(profile!.displayName, "اپراتور نمایشی توبا");
});

test("mapOperatorProfile rejects empty displayName", () => {
  assert.equal(mapOperatorProfile({ firstName: "x" }), null);
});
