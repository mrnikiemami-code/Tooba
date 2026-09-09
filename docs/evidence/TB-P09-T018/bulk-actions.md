# Bulk Actions

Safe codes: `mark_processing`, `mark_packed`, `dispatch_shipment`, `deliver_shipment`.
Intersection + same seller. Create shipment / tracking excluded (need extra input).

Runtime multi-seller checkout `01a08175-32ba-7000-9551-daa24d42da61` (3 sellers / 3 queue rows): bulk `mark_processing` → 400 human «انتخاب ناسازگار» / cross-seller. Partial failures already return succeeded/attempted.
