import assert from "node:assert/strict";
import test from "node:test";
import {
  addressBookEmptyMessage,
  canShowCheckoutSavedAddresses,
  CustomerAddressApiError,
  createCustomerAddress,
  deleteCustomerAddress,
  listCheckoutSavedAddresses,
  listCustomerAddresses,
  mapCustomerAddress,
  mapCustomerAddressList,
  setDefaultCustomerAddress,
  shippingFromCustomerAddress,
  toCustomerAddressWritePayload,
  updateCustomerAddress,
} from "./customer-address-api.ts";

const sample = {
  addressId: "addr-1",
  recipientName: "علی رضایی",
  contactMobile: "09120000000",
  country: "IR",
  provinceName: "تهران",
  cityName: "تهران",
  postalCode: "1234567890",
  postalAddress: "خیابان ولیعصر",
  buildingUnit: "۵",
  label: "خانه",
  isDefault: true,
};

test("address list mapping accepts camel and PascalCase envelopes", () => {
  const camel = mapCustomerAddressList([sample]);
  assert.equal(camel[0]?.addressId, "addr-1");
  assert.equal(camel[0]?.isDefault, true);

  const pascal = mapCustomerAddressList({
    Items: [{
      AddressId: "addr-2",
      RecipientName: "سارا",
      ContactMobile: "09121111111",
      Country: "IR",
      ProvinceName: "اصفهان",
      CityName: "اصفهان",
      PostalCode: "9876543210",
      PostalAddress: "چهارباغ",
      IsDefault: false,
    }],
  });
  assert.equal(pascal[0]?.addressId, "addr-2");
  assert.equal(pascal[0]?.cityName, "اصفهان");
  assert.equal(pascal[0]?.label, null);
});

test("address mapper ignores rows without AddressId", () => {
  assert.equal(mapCustomerAddress({ recipientName: "x" }), null);
  assert.deepEqual(mapCustomerAddressList([{ recipientName: "x" }]), []);
});

test("address empty helper only describes an actual empty collection", () => {
  assert.equal(addressBookEmptyMessage(0), "هیچ آدرسی یافت نشد");
  assert.equal(addressBookEmptyMessage(1), null);
});

test("create and edit payloads omit empty optional fields and keep isDefault", () => {
  const created = toCustomerAddressWritePayload({
    recipientName: " علی ",
    contactMobile: "09120000000",
    country: "IR",
    provinceName: "تهران",
    cityName: "تهران",
    postalCode: "1234567890",
    postalAddress: "ولیعصر",
    buildingUnit: "  ",
    label: "خانه",
    isDefault: true,
  });
  assert.equal(created.recipientName, "علی");
  assert.equal(created.label, "خانه");
  assert.equal("buildingUnit" in created, false);
  assert.equal(created.isDefault, true);

  const edited = toCustomerAddressWritePayload({
    recipientName: "علی",
    contactMobile: "09120000000",
    country: "IR",
    provinceName: "تهران",
    cityName: "تهران",
    postalCode: "1234567890",
    postalAddress: "ولیعصر",
    isDefault: false,
  });
  assert.equal(edited.isDefault, false);
});

test("saved address fills checkout shipping snapshot fields", () => {
  const mapped = mapCustomerAddress(sample);
  const shipping = shippingFromCustomerAddress(mapped!);
  assert.equal(shipping.recipientName, "علی رضایی");
  assert.equal(shipping.postalAddress, "خیابان ولیعصر");
  assert.equal("savedAddressId" in shipping, false);
});

test("address mutations call Host paths and surface 401", async () => {
  const originalFetch = globalThis.fetch;
  const calls: { url: string; method?: string; body?: string }[] = [];
  globalThis.fetch = (async (input: string | URL | Request, init?: RequestInit) => {
    const url = String(input);
    calls.push({ url, method: init?.method, body: typeof init?.body === "string" ? init.body : undefined });
    if (url.endsWith("/default")) return new Response(null, { status: 204 });
    if (init?.method === "DELETE") return new Response(null, { status: 401 });
    if (init?.method === "POST" && url.endsWith("/addresses")) {
      return new Response(JSON.stringify({ ...sample, addressId: "addr-new" }), { status: 201 });
    }
    if (init?.method === "PUT") return new Response(JSON.stringify(sample), { status: 200 });
    return new Response(null, { status: 401 });
  }) as typeof fetch;
  try {
    await assert.rejects(
      () => listCustomerAddresses(),
      (error: unknown) => error instanceof CustomerAddressApiError && error.status === 401,
    );

    const created = await createCustomerAddress({
      recipientName: "علی",
      contactMobile: "09120000000",
      country: "IR",
      provinceName: "تهران",
      cityName: "تهران",
      postalCode: "1234567890",
      postalAddress: "ولیعصر",
      label: "خانه",
      isDefault: true,
    });
    assert.equal(created.addressId, "addr-new");
    assert.match(calls[1]?.body ?? "", /recipientName/);
    assert.equal(calls[1]?.url, "/api/customer/addresses");

    const updated = await updateCustomerAddress("addr-1", {
      recipientName: "علی",
      contactMobile: "09120000000",
      country: "IR",
      provinceName: "تهران",
      cityName: "تهران",
      postalCode: "1234567890",
      postalAddress: "ولیعصر",
      isDefault: false,
    });
    assert.equal(updated.addressId, "addr-1");
    assert.equal(calls[2]?.url, "/api/customer/addresses/addr-1");
    assert.match(calls[2]?.body ?? "", /"isDefault":false/);

    await setDefaultCustomerAddress("addr-1");
    assert.equal(calls[3]?.url, "/api/customer/addresses/addr-1/default");
    assert.equal(calls[3]?.method, "POST");

    await assert.rejects(
      () => deleteCustomerAddress("addr-1"),
      (error: unknown) => error instanceof CustomerAddressApiError && error.status === 401,
    );
    assert.equal(calls[4]?.url, "/api/customer/addresses/addr-1");
    assert.equal(calls[4]?.method, "DELETE");
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("checkout saved-address visibility hides 401 guest path", () => {
  assert.equal(canShowCheckoutSavedAddresses(200), true);
  assert.equal(canShowCheckoutSavedAddresses(401), false);
  assert.equal(canShowCheckoutSavedAddresses(403), false);
});

test("checkout saved list returns null on 401 and mapped rows on 200", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async () => new Response(null, { status: 401 })) as typeof fetch;
  try {
    const hidden = await listCheckoutSavedAddresses();
    assert.equal(hidden.addresses, null);
    assert.equal(hidden.status, 401);
  } finally {
    globalThis.fetch = originalFetch;
  }
  globalThis.fetch = (async () =>
    new Response(JSON.stringify([sample]), { status: 200 })) as typeof fetch;
  try {
    const shown = await listCheckoutSavedAddresses();
    assert.equal(shown.addresses?.[0]?.addressId, "addr-1");
  } finally {
    globalThis.fetch = originalFetch;
  }
});
