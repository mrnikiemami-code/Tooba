# TB-P10-T004-R24-R1-R1 — Order snapshot

`StorefrontCheckoutComposer.BuildCommand` still writes independently:

- `RecipientFirstName` ← prepared `FirstName`
- `RecipientLastName` ← prepared `LastName`
- `RecipientName` ← prepared RecipientName (first+last when both exist; else legacy)

Payment / Customer / Admin read the committed checkout snapshot through `StorefrontRecipientNames.Display`. They do not re-query AddressBook for the summary name.

Historical orders with empty First/Last keep rendering `RecipientName`.
