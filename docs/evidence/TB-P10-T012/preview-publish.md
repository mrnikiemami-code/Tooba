# Preview and publish

Admin GET preview returns enabled sections for Draft or Published.
Public GET /v1/storefront/pages/{slug} remains 404 for Draft.
Preview route is under Admin (robots noindex) and uses the same renderer.
Publish/unpublish invalidates page+home caches.
Unpublish clears Home if that page was selected.
