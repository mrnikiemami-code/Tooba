# TB-P05-T014 — Shopeiva address fidelity

## Reuse decision

The implementation preserves Shopeiva's Persian RTL customer-panel shell, address cards/forms, checkout «انتخاب آدرس» block (`آدرس جدید` / `آدرس‌های من`), spacing, and Tooba blue `#2563EB` CTA treatment. It does not invent a parallel address design system, map picker, delivery-time chips, order-count badges, or save-from-checkout checkbox (Shopeiva has none).

## Evidence map

- `03-customer-address-list-desktop.png` (`1440x900`): live list with default/home and work cards.
- `04-customer-address-create.png` (`1440x900`): Shopeiva create modal with live field labels.
- `05-customer-address-edit-default.png` (`1440x900`): edit modal opened from the default card.
- `06-customer-address-empty-state.png` (`1440x900`): honest empty copy «هیچ آدرسی یافت نشد».
- `07-checkout-saved-address-selection.png` (`1440x900`): checkout «آدرس‌های من» cards with default badge.
- `10-customer-address-mobile-390x844.png` (`390x844`): customer Address Book at the exact mobile viewport.
- `11-checkout-mobile-address-390x844.png` (`390x844`): checkout saved-address selection at the exact mobile viewport.

## Capture integrity

All images were captured from normal local Development processes with installed Google Chrome in headless standard-browser mode. Data came through the real Next application and Host API using deterministic Development records. No request interception, mocked payload, disabled browser security, DOM editing, image editing or fabricated state was used.
