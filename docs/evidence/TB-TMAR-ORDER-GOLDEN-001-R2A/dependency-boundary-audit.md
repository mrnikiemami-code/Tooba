# Dependency-boundary audit
Order Endpoints dispatches with `ISender`; Order Application and Infrastructure gained no foreign Application/Infrastructure references. Host contains only the authorization adapter and registration. No Host composer, Host endpoint implementation, or foreign DbContext was restored.
