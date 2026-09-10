# Decimal regression — TB-P09-T022

Weighted path uses PARS offer (`01a03826-…`) which accepts qty `1.25` (proven T015/T020).

Runtime J (checkout `01a08974-2617-7000-8e44-1ece1b900d22`):

- Cart accepted PARS quantity `1.25` + ARMAN `1`
- Pack/ship/package create preserved exact `1.25` (`exactDecimal: true`)
- Split 0.50+0.75 not required for this gate; qty architecture unchanged
