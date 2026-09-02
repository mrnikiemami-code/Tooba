Prefixed /fa|/en public paths rewrite to internal routes. Next re-invokes middleware on the rewrite target; isPublicStorefrontPath then 308’d back to /fa/... (self-loop).

planLocaleMiddleware treats rewritten requests with x-tooba-locale as pass. Smoke: /fa /en /fa/blogs /en/blogs /fa/blogs/guide-online-shopping /en/blogs/guide-online-shopping all HTTP 200.
