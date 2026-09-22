# Order physical tree

Present projects:

- `Tooba.Order.Domain`
- `Tooba.Order.Application`
- `Tooba.Order.Contracts`
- `Tooba.Order.Infrastructure`

Missing required project:

- `Tooba.Order.Endpoints`

No TypeForwardedTo repair was introduced. Because the endpoint project and CQRS command/query layout are absent, physical state is `INCOMPLETE`.
