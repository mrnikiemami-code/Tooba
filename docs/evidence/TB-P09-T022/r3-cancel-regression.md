# R3 cancel regression — TB-P09-T022-R3

Whole-order cancel still releases Held reservations via existing cancel/inventory path. Commit does not change Released/Consumed semantics. Covered by focused filter including `WholeOrderCancelUntilDispatchTests`.
