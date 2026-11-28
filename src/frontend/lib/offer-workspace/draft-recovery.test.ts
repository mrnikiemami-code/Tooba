import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import test from "node:test";
import { ensureDom } from "../../features/order-carrier-execution/test-dom-harness.ts";
import { discardCreateDrafts, draftStorageKey, readDraft, writeDraft } from "./draft-recovery.ts";
import type { OfferWorkspaceAdapter } from "./types.ts";

ensureDom();
test("explicit New removes scoped create recovery, not edit drafts or another actor", () => {
  window.sessionStorage.clear();
  window.localStorage.setItem("tooba.adminActorUserId", "actor-a");
  window.localStorage.setItem("tooba.sellerActorUserId", "seller-actor");
  const a = { role: "admin", mode: "create" } as OfferWorkspaceAdapter;
  const edit = { ...a, mode: "edit", offerId: "existing" } as OfferWorkspaceAdapter;
  const seller = { role: "seller", mode: "create", fixedSeller: { sellerPartyId: "seller-b", displayName: "" } } as OfferWorkspaceAdapter;
  const key = (adapter: OfferWorkspaceAdapter) => draftStorageKey(adapter, null, null, null, "");
  const draft = { draft: { sellerSku: "OLD", createPrices: { IRR: "1000", USD: "5" } } };
  const createKey = key(a);
  writeDraft(createKey, a, draft);
  writeDraft(key(edit), edit, draft);
  writeDraft(key(seller), seller, draft);
  window.localStorage.setItem("tooba.adminActorUserId", "actor-b");
  writeDraft(key(a), a, draft);
  window.localStorage.setItem("tooba.adminActorUserId", "actor-a");
  assert.equal(readDraft(createKey, a)?.draft?.sellerSku, "OLD"); // refresh / locale remount
  discardCreateDrafts(a);
  assert.equal(readDraft(createKey, a), null);
  assert.equal(readDraft(key(edit), edit)?.draft?.sellerSku, "OLD");
  assert.equal(readDraft(key(seller), seller)?.draft?.sellerSku, "OLD");
  window.localStorage.setItem("tooba.adminActorUserId", "actor-b");
  assert.equal(readDraft(key(a), a)?.draft?.sellerSku, "OLD");
  window.sessionStorage.clear();
});
