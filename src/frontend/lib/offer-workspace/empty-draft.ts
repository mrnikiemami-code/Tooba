import type { OfferWorkspaceDraft } from "./model.ts";

/** Blank editable workspace draft; not a domain write and not a default business value. */
export function emptyDraft(): OfferWorkspaceDraft {
  return {
    sellerSku: "",
    amount: "",
    currency: "",
    inventoryOnHand: {},
    inventoryReason: "",
    offeredQuantity: "",
    returnPolicyChoice: "Default",
    customReturnWindowDays: "",
    minimumOrderQuantity: "",
    maximumOrderQuantity: "",
    status: "Draft",
  };
}
