# Architecture Guard Audit

WalletArchitectureGuardTests enforce Endpoints project, path↔namespace, CQRS folders, Host ownership removal, no Guid.NewGuid in Wallet production Endpoints/Application/Infrastructure/Domain/Contracts, no TypeForwardedTo, Contracts-only Notification, IIdGenerator/IClock no DI bypass, silent catch gate, STABLE_CODES_ONLY mapper.
