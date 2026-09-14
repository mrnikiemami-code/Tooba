# Performance

One page projection + one home + one listing for cards. No Admin/storefront polling.
Take capped at 24. Section load is a single query per page.
Writes invalidate `store-landing-page:` and `store-landing-home:` only.
