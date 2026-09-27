# CQRS authority — R4

One authoritative MediatR Command/Query per use case.
Handlers pass the request to directory ports (no second Models.*Command wrapper).
Validator matrix preserved: 17 REQUIRED / 17 PRESENT / 34 NO_VALIDATOR.
