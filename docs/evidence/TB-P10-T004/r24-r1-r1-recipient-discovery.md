# TB-P10-T004-R24-R1-R1 — Recipient discovery

| Surface | FirstName | LastName | RecipientName | Previous precedence | Required / repaired precedence |
| --- | --- | --- | --- | --- | --- |
| AddressBook entity | optional columns | optional columns | required legacy string | RecipientName always stored; ApplyRecipientNames only when both parts exist | unchanged: no guessed split |
| Shipping form | `shipping-first-name` | `shipping-last-name` | composeRecipient first+last else form recipient | FE composeRecipient already preferred first+last | same rule; no new split helper |
| Selected saved address | mapped if present | mapped if present | book RecipientName | UI showed `recipientDisplayName`; **PrepareShipping replaced the whole snapshot from the book** | book supplies address/mobile; names use explicit First+Last when both present |
| Checkout / shipping draft | `CartShippingDraft.FirstName` | `LastName` | mirrored first+last when both set | Save wrote prepared book RecipientName then ApplyRecipientNames(request) | Prepare now keeps explicit First+Last; draft RecipientName = first+last |
| Shipping commit DTO | `StorefrontCheckoutShippingInput.FirstName` | `LastName` | RecipientName | Submit called Prepare again and re-applied the book name | explicit First+Last survive Prepare and are committed |
| Order snapshot | `CheckoutGroup.RecipientFirstName` | `RecipientLastName` | compatibility mirror | stored from prepared shipping | same columns; prepared RecipientName is first+last when both exist |
| Payment projection | snapshot First/Last | snapshot First/Last | `MapPage` RecipientName | used `snapshot.RecipientName` only | `StorefrontRecipientNames.Display(First, Last, Recipient)` |
| Customer Order | group First/Last | group First/Last | group RecipientName | `group.RecipientName` only | same Display helper |
| Admin Order | group First/Last | group First/Last | group RecipientName | `group.RecipientName` only | same Display helper |

Defect path: `StorefrontCheckoutComposer.PrepareShippingAsync` discarded request First/Last whenever `SavedAddressId` resolved. `SubmitAsync` always re-prepares, so Payment read a stale book `RecipientName` (e.g. `محمد لمامی`) instead of `محمد امامی`.
