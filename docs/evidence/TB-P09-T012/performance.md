# Performance — TB-P09-T012

Capabilities composed in existing `ListAsync` after fulfillments already loaded. Projector does not call fulfillment directory. Source test asserts `AdminFulfillmentCapabilityProjector.Project` + `lineCaps` and no `ListForCheckoutAsync(` in the projector.
