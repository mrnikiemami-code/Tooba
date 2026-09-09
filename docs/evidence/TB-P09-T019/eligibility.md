# Eligibility — TB-P09-T019

Queue summary from OrderLine snapshot + batched last DeliveredAt:

- before delivery: snapshot label (e.g. ۷ روز پس از تحویل)
- after delivery: remaining time + deadline
- expired: منقضی
- non-returnable: غیرقابل مرجوعی / `return.non_returnable`

Evaluator does not consult settlement.
