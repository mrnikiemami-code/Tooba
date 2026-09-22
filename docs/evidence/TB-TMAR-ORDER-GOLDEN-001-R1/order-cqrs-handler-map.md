# Order CQRS Handler Map

No complete Endpoint → `ISender` → MediatR 12.5 handler map exists for the audited Order-owned HTTP surfaces. Creating wrapper handlers over Host composers would be fake CQRS and was not done. Gate: FAIL.
