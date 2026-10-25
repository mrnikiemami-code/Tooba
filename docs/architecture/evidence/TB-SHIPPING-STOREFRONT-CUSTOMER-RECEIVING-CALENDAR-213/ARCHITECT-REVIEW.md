# Architect review — TB213

ACCEPT_BOUNDED_CALENDAR_PRIMITIVE; NOT_WIRED_TO_STOREFRONT. Reviewed27a8f834005cabfbf6ecfbd5e0dc7b21c2d64298 full source/test diff and RESULT. Root did not rerun .NET/runtime.

The additive method correctly excludes reference date for N>=1, counts only configured working weekdays, skips closed offered dates, retains each date's TimeOnly interval/fractions, handles allclosed before search, validates unique seven days/positive working intervals, and keeps two inputs independent. Existing legacy members and consumers unchanged. Worker11/11 focusedfacts +projectbuild clean; two original expectation corrections disclosed. Owner's Saturday/Sunday1/Mondayclosed/Tuesday2 example is tested.

Precise limits before future wiring: deliveryHorizonDays currently means NUMBER OF AVAILABLE DATES, not calendar-day span (e.g. five requested dates can span more than five calendar days). Caller must map this explicitly; never silently reuse calendar-span configuration as slot count. Upper90 is an internal defensive cap, NOT user-approved booking policy;365 follows authored daylimit. DateOnly extremes can overflow AddDays (including needless final increment at maxdate); present-dayinternal primitive not publicvalidatedinput, so not approval to expose arbitrarydate endpoint. Zerooffset doesn't remove already-past intervals/cutoff. Method timezone conversion is caller responsibility. Maxdeliveryduration is not enforced by this primitive; nocustomerpromise certification.

These limits do not reopen minworkingday proof or warrant a standalone confidence loop. Address relevant boundary semantics when wiring actual consumers, not pretend11tests prove complete integration. Legacy carrierpickup must remain separate.

Next214 implementation approved only as lossless IMMUTABLE CAPTURE of an owner-scoped authoring revision, not customer activation: persisted complete Shipping-owned snapshot +strict owner/revision read, no publicpublishroute/livepointer, no lossy legacyconversion. All currently authorizedowner kinds retained; no blanket seller rejection. Future explicit publication can reference capturedrevision after readiness/consumer wiring, not currentdraft live reads. This is an architectural capture primitive, not productapproval of go-live.

Preserve realuserdata/runtime/appliedmigrations and TB185paused/defaultOFF. No113/191/etc replay, no date/rate policy guessed, no second quoteengine. Review214 before any shared migration/runtime refresh.
