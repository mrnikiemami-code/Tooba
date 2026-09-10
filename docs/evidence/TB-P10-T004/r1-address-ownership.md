# Address Ownership — TB-P10-T004-R1

- Customer A lists only own address; foreign address GET → 404
- Customer A shipping selection with Customer B `savedAddressId` → 403 `shipping.address.forbidden`
Backend-authoritative; no address GUID required in normal UI smoke.
