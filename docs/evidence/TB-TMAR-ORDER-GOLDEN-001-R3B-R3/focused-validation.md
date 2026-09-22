# Focused validation — R3B-R3

- Order.Tests: 50 passed
- OrderAdminOperationsArchitectureGuardTests: 8 passed (incl. Message-parsing forbid)
- Host AdminOrderOperations*: 9 passed
- Fulfillment Architecture: 3; ErrorAndGrid: 4
- Returns Architecture|SemanticPresentation: 8
- Settlement Architecture|ErrorAndGrid: 7
- PaymentR1CqrsContractTests: 6
- dotnet build src/backend/Tooba.slnx: succeeded (0 errors)

Message parsing absent from touched adapters / ContractOperationFault (verified by guard + grep).
