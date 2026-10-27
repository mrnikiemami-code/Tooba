import assert from "node:assert/strict";
import test from "node:test";

import {
  createRequestGeneration,
  destinationIdentityKey,
  planDestinationRefresh,
} from "./storefront-shipping-destination-refresh.ts";

const destination = {
  countryId: "01a17000-0000-7000-8000-0000000000aa",
  administrativeAreaId: "01a17000-0000-7000-8000-0000000000bb",
  cityId: "01a17000-0000-7000-8000-0000000000cc",
};

test("destination transition requests exactly the verified current identifiers", () => {
  const first = planDestinationRefresh({
    capturedActive: true,
    destinationKey: destinationIdentityKey(destination),
    appliedKey: "",
    destination,
  });
  assert.equal(first.action, "request");
  assert.deepEqual(first.destination, destination);
  assert.equal(first.destination?.cityId, destination.cityId);

  // A deeper verification (city arrives after the area) is a NEW identity → its own request.
  const deeper = {
    ...destination,
    cityId: "01a17000-0000-7000-8000-0000000000dd",
  };
  const second = planDestinationRefresh({
    capturedActive: true,
    destinationKey: destinationIdentityKey(deeper),
    appliedKey: destinationIdentityKey(destination),
    destination: deeper,
  });
  assert.equal(second.action, "request");
  assert.equal(second.destination?.cityId, deeper.cityId);
});

test("unrelated renders and legacy OFF never issue a request", () => {
  const key = destinationIdentityKey(destination);
  assert.equal(
    planDestinationRefresh({ capturedActive: true, destinationKey: key, appliedKey: key, destination })
      .action,
    "skip",
  );
  // Legacy (captured preview OFF) is untouched even when the destination changes.
  assert.equal(
    planDestinationRefresh({ capturedActive: false, destinationKey: key, appliedKey: "", destination })
      .action,
    "skip",
  );
});

test("clearing a parent clears the lower identity and removes the stale price", () => {
  assert.equal(destinationIdentityKey(null), "");
  // Parent area cleared: the city disappears with it, so the identity really is country-only.
  const cleared = { countryId: destination.countryId, administrativeAreaId: null, cityId: null };
  assert.equal(destinationIdentityKey(cleared), `${destination.countryId}||`);
  assert.notEqual(destinationIdentityKey(cleared), destinationIdentityKey(destination));

  const plan = planDestinationRefresh({
    capturedActive: true,
    destinationKey: destinationIdentityKey(cleared),
    appliedKey: destinationIdentityKey(destination),
    destination: cleared,
  });
  assert.equal(plan.action, "request");
  assert.equal(plan.destination?.administrativeAreaId, null);
  assert.equal(plan.destination?.cityId, null);

  // Everything cleared → one request without any destination identity.
  const empty = planDestinationRefresh({
    capturedActive: true,
    destinationKey: "",
    appliedKey: destinationIdentityKey(cleared),
    destination: null,
  });
  assert.equal(empty.action, "request");
  assert.equal(empty.destination, null);

  // Repeated clear (already applied) does not loop.
  assert.equal(
    planDestinationRefresh({ capturedActive: true, destinationKey: "", appliedKey: "", destination: null })
      .action,
    "skip",
  );
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
