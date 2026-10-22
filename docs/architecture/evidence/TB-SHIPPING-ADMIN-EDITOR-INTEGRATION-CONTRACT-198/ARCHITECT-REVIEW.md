# Architect review — TB198
Verdict: ACCEPT_SOURCE_ANALYSIS_WITH_CORRECTIONS. Reviewed against main 66db0f405e039fcb6a9a53ab9137c93094a4124d; compare from e33ef067 has documentation changes only. No implementation or test execution is claimed by Architect.

Accepted: one own Shipping editor operation, server owner authorization, shared root Revision, Contracts-only read facts before mutation, no foreign I/O after commit, existing capability APIs preserved. Field matrix correctly identifies preview fixtures and unresolved cutoff/status/owner-selector/carrier/funding facts. Analysis stage closed; do not replay it.

Binding implementation corrections (supersede proposal prose/examples):
1. Region replacement already uses staged order plus two saves in one own transaction because immediate unique indexes make reorder unsafe in one implicit save. One atomic COMMIT, not one SaveChanges, is the invariant. Preserve node-row moves/identity and rollback.
2. Existing Apply* methods increment root Revision independently. They cannot simply be called sequentially. Extract shared mutation cores/whole-document plan with one final root revision/timestamp advance, preserving old independent APIs. No counter reset.
3. Group-rate/title preflight currently resolves groups in the EXISTING tree. New groups with nested rates/titles need detached prospective identities/graph validation before touching tracked state; don't add then validate.
4. Method text actual bound is 200/2000 and group title200 (existing presentation), not matrix128/512 (type-catalog bounds). Reuse actual canonical validators/domain; don't copy example limits.
5. Proposal top-level absent=unchanged is a composite update, not literal replacement of every field. Presence must be explicit. Settings/schedule clearing has no existing authority; do not add null-clear semantics. Inactive text omitted stays preserved; a GET echo containing inactive text is not automatically a lawful write. New group titles must nest under their group; no fake ids.
6. GET example omits six weekdays and uses abbreviated ids: illustrative only, not a valid request or fixture. Actual schedule requires seven ISO days. Existing enum serialization is authoritative.
7. Reuse of existing blank draft POST is a staged draft creation workflow, not an atomic create of the complete method. Next task is existing-draft editor save only; full create/canonical-choice UI acceptance remains open.
8. Funding ownership and readiness cannot be established from preview labels: no new Promotion authority or completeness algorithm approved here. Cutoff/status/owner-selector remain excluded.

Next sole task TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199: implement existing-draft GET/PUT aggregate document and meaningful isolated PostgreSQL tests under these corrections, no UI/schema/runtime quote. No proof-only loop for old accepted tasks.
