# TB-TMAR-HOST-CONTENT-AMC-001

**Channel:** tooba-main (user-authorized after Host/Development checkpoint)  
**Parent:** TB-TMAR-HOST-DEVELOPMENT-AMC-001  
**Mode:** BACKEND_ONLY — AMC Host/Content → production ZERO  
**Target:** `src/backend/Host/Tooba.Host/Content/`

## User AMC ask (summarized)

Empty Host/Content via Analyze → Migrate → Certify. Do **not** empty or alter Host/Development retained exception (3 marketplace bootstraps). Create `Tooba.Content.Endpoints`, move endpoints/composers/admin access, move media validator to Infrastructure/Adapters, relocate `ContentDevelopmentSeedHost` to `Host/Composition`, move Content grid engines out of Host/Grid into Content.Infrastructure so Endpoints never references Host. Wire Program/slnx, durable guard, evidence, `hostContentAmc` SoT. No commit unless asked.

## Inventory before

12 production files under Host/Content (verified).

## After

Host/Content = **0** `.cs`. See `docs/evidence/TB-TMAR-HOST-CONTENT-AMC-001/`.

## PASS criteria

Host/Content production file count = 0; focused guards pass; evidence + SoT `hostContentAmc`; no next Host folder started; Development untouched.
