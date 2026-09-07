# Runtime E2E — TB-P09-T006

Host `http://127.0.0.1:5088` (alpha.localhost) + FE `:3000`.

## Schema

Applied on `tooba_alpha`:

- `offer.offers.return_policy_choice` / `custom_return_window_days`
- `fulfillment.shipments.shipping_method_code` / `shipping_method_label` / `provider_metadata_json` / `provider_metadata_version`

## Smoke

1. `GET /v1/admin/shipping-methods` → 200 with post, tipax, snapp_courier, store_courier, in_person.
2. Paid multi-seller checkout `01a07a63-adea-7000-b184-277160114a48`:
   - `POST .../operations` create_shipment with `shippingMethodCode=post` + provider metadata JSON → **200**.
   - Line return UI remained policy label (قبل از تحویل) — undelivered clock not started.
3. Focused unit tests green for return-policy resolver, shipping metadata validator, split-delivery domain, FE modal markers, recovery guard.

## Notes

- No fake carrier booking/dispatch on create.
- USER_VISUAL_ACCEPTED=NO.
