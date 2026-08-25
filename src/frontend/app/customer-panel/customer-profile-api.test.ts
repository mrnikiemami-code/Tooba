import assert from "node:assert/strict";
import test from "node:test";
import { mapCustomerProfile } from "./customer-api.ts";
import {
  customerProfileErrorMessage,
  CustomerProfileApiError,
  profileFormToWrite,
  profileNameFromDisplay,
} from "./customer-profile-api.ts";

test("profile mapper exposes editable profile fields and identity read-only flags", () => {
  const page = mapCustomerProfile({
    actorUserId: "actor-1",
    displayName: "علی رضایی",
    firstName: "علی",
    lastName: "رضایی",
    email: "ali@example.com",
    contactMobile: "09120000000",
    birthDate: "1403/06/04",
    bio: "بیو",
    editable: true,
    emailEditable: false,
    mobileEditable: false,
    avatarUploadAvailable: false,
    nationalCodeEditable: false,
    addressEditable: false,
  });
  assert.equal(page?.displayName, "علی رضایی");
  assert.equal(page?.email, "ali@example.com");
  assert.equal(page?.editable, true);
  assert.equal(page?.emailEditable, false);
});

test("profile write payload excludes identity fields", () => {
  const payload = profileFormToWrite({
    name: "سارا احمدی",
    birthDate: "1402/01/01",
    bio: "توضیح",
  });
  assert.equal(payload.displayName, "سارا احمدی");
  assert.equal(payload.firstName, "سارا");
  assert.equal(payload.lastName, "احمدی");
  assert.equal("email" in payload, false);
  assert.equal("contactMobile" in payload, false);
});

test("profile name splitter handles single token", () => {
  const payload = profileNameFromDisplay("توبا");
  assert.equal(payload.displayName, "توبا");
  assert.equal(payload.firstName, "توبا");
});

test("profile error mapping covers unauthorized", () => {
  assert.match(
    customerProfileErrorMessage(new CustomerProfileApiError(401, "Unauthorized")),
    /نشست/,
  );
});
