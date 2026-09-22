# Settlement CQRS audit

- MediatR package: 12.5.0 (BuildingBlocks).
- Host CQRS foundation registers Settlement.Application assembly via GetSellerSettlementBalanceQuery type.
- Every HTTP route sends exactly one IRequest Command/Query through ISender.
- Use-case folders:
  - Commands/RequestSellerPayout, ProcessAdminPayout, RetryAdminPayout
  - Queries/GetSellerSettlementBalance, ListSellerSettlementEntries, ListSellerSettlementStatements, ListSellerPayoutRequests, ListAdminSettlementBalances, ListAdminPayoutQueue, QueryAdminPayoutGrid
- No ceremonial duplicate handlers.
- Settlement-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
