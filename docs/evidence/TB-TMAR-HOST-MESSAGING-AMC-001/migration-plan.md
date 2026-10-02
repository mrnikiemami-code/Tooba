# migration-plan — TB-TMAR-HOST-MESSAGING-AMC-001

## Recommended wave count: 2

### W1 — structure / namespace / cohesion hygiene (~12–15 min)

- Exact namespace `Tooba.Host.Messaging` for all Messaging production files
- Split `MessagingOptionsValidator` into own file
- Update Program / Health / tests usings as required
- Add `HostMessagingAmcW1GuardTests` (tree, namespace, locator gates)
- Behavior preserved; no transport redesign; no test-double redesign

### W2-CERT — certify (~11–15 min)

- Durable `HostMessagingAmcCertGuardTests`
- Lock dispositions, fail-closed selection, Health/Outbox boundaries, ZERO foreign layers
- Label: `HOST_MESSAGING_AMC_CERTIFIED`

## Deferred (not required before CERT unless Architect opens)

- Metric rename alignment
- Disabled-publisher stable error code
- In-process typed handler registry (replace reflection)
- Moving Npgsql registration into Transport folder

## Option B rejected for single mega-wave

Runtime redesign + CERT exceeds safe 20-minute bound; Prefer **A** (W1 then CERT).
