# TB-P10-T004-R24-R1-R1 — Payment summary

Scenario: legacy saved address (`محمد لمامی`) selected, then explicit FirstName=`محمد` LastName=`امامی`.

| Surface | Expected |
| --- | --- |
| Shipping draft after save | `محمد امامی` |
| Payment `page.recipientName` | `محمد امامی` from committed snapshot Display |
| Must not show | `محمد لمامی` |

`src/frontend/app/payment/storefront-payment-handoff.tsx` still prints `page.recipientName`. The backend `MapPage` now canonicalizes that string from snapshot First/Last. Payment does not re-read AddressBook RecipientName.

Runtime checkout `01a09aae-37d0-7000-89cd-a22cebf0a90f`: commit + GET checkout + customer + admin all returned `محمد امامی`. Independent snapshot columns: First=`محمد` Last=`امامی` Recipient=`محمد امامی`.
