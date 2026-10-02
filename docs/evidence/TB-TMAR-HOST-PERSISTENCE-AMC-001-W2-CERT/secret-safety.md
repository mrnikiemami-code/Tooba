# secret-safety — TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT

Required ZERO exposure proven for:

- raw connection string
- connection reference
- username/password tokens
- parser exception prose
- host/database/SSL options via exception path
- logs (`ILogger` / `Console` absent)
- metrics tags / tracing tags of credentials (none present on resolver)
- ProblemDetails detail from resolver

Focused behavior tests assert failure paths do not echo reference or connection string.
