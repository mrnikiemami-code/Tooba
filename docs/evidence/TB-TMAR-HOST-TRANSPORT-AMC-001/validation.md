# Validation — Host/Transport AMC-001

## Build

- `Tooba.Host`
- `Tooba.Host.Tests`

## Tests

- `HostTransportAmcGuardTests` (new)
- `TmarDurableGuardTests` (retargeted)

## Expected

- Host/Transport PRESENT, exactly 3 files, namespace EXACT
- Foreign module layers ZERO
- Payload / connection-secret logging ZERO
- Recovery pointers on Transport KEEP stop
- Schema/frontend unchanged
