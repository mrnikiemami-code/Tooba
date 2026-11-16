# TB-274 — receipts

All commands were run from the repository root `D:\Users\User\source\repos\SarvNewVer`. Nothing below created a committed
business row, and no provider/Wallet/Inventory/waybill/notification/money operation was performed.

## 1. Base

```
git rev-parse HEAD
ca2dbed34ba67251dea5b60fd048d1040eef02dd          # == Expected-Base-SHA
git merge-base --is-ancestor ca2dbed3… HEAD -> yes
git log --oneline -1 ca2dbed3…
ca2dbed34 docs(returns): accept 273-R2 repair scope and advance to customer receiving
```

## 2. Pre-apply backup and apply

```
(Get-FileHash D:\temp\tb274-backup\tooba_alpha-pre-tb274.dump -Algorithm SHA256).Hash
ADCE728BD0D5732C09F5EECDC219287859CB7FE56173F9F5BD07A10BDC64CD16
Get-Item …pre-tb274.dump | Length
2966890
Get-Content D:\temp\tb274-backup\apply.log -Tail 6
      "pendingMigrations": [],
      "succeeded": true,
      "durationMs": 122
```

## 3. Post-apply read-only schema

```
docker exec -e PGPASSWORD=*** postgres-db psql -U admin -d tooba_alpha -t -A -c \
  "select migration_id from returns.__ef_migrations_history order by migration_id desc limit 3;"
20261116122318_AddCustomerReceiptsAndDecisionReason
20261114170000_AddReturnRequestDecisions
20261113174500_NormalizeCourierOriginColumnNames

select count(*) from returns.customer_return_receipts;        -- 0
select count(*) from returns.customer_return_receipt_lines;   -- 0

select column_name, data_type, character_maximum_length, is_nullable
  from information_schema.columns
 where table_schema='returns' and table_name='return_request_decisions';
return_request_id|uuid||NO
decision|character varying|32|NO
decided_by_user_id|uuid||NO
decided_at|timestamp with time zone||NO
reason|character varying|512|YES
```

## 4. Build and focused suites

```
dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj -c Release
Passed!  - Failed: 0, Passed: 93, Skipped: 0, Total: 93

dotnet test … -c Release --filter "FullyQualifiedName~ReturnsCustomerReceipts274Tests"
Passed!  - Failed: 0, Passed: 25, Skipped: 0, Total: 25

dotnet build src/backend/Tooba.slnx -c Release
Build succeeded. 0 Error(s)

dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~ReturnsCustomerReceipts274RelationalTests"
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6
  TB274-A cumulative coverage  target=tooba_alpha:5432/172.18.0.2  committedRows=0
  TB274-A mixed conditions     target=tooba_alpha:5432/172.18.0.2  committedRows=0
  TB274-B receiving gate       target=tooba_alpha:5432/172.18.0.2  committedRows=0
  TB274-C rejection reason     target=tooba_alpha:5432/172.18.0.2  committedRows=0
  TB274-C missing reason       target=tooba_alpha:5432/172.18.0.2  committedRows=0
  TB274-D decided case         target=tooba_alpha:5432/172.18.0.2  committedRows=0

dotnet test … --filter "FullyQualifiedName~ReturnsModuleAmsc001"
Passed!  - Failed: 0, Passed: 34, Skipped: 0, Total: 34

$env:TB273R1_CONNECTION="…Database=tooba_alpha"
dotnet test … --filter "FullyQualifiedName~ReturnsRecovery273RelationalTests"
Passed!  - Failed: 0, Passed: 3, Skipped: 0, Total: 3
```

## 5. Publish, restart and served artifact

```
# graceful stop of the locking process, then
dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts/publish-primary
Tooba.Host -> D:\Users\User\source\repos\SarvNewVer\artifacts\publish-primary\
(Get-FileHash artifacts/publish-primary/Tooba.Host.dll -Algorithm SHA256).Hash
A3A67D8D129A76D6E6BBBB42371CE7E224735436F2A499FC97232FF59DDBD805

Get-NetTCPConnection -LocalPort 5088 -State Listen | OwningProcess
7804
/health             -> 200
/health/ready       -> 200 {"edition":"SingleStore","postgresql":"configured","messaging":"Healthy", …}
```

## 6. Read-only wire probes (no actor, no case, no write)

```
POST /v1/admin/returns/{random-guid}/receipt
401 admin.actor.missing   (route served; authentication precedes anything else)

POST /v1/admin/returns/query   {}   -> 401 admin.actor.missing
```

No live return case exists, so no served operator walkthrough was produced and no demo case/order/hold was created.

## 7. Change surface (source only; untracked `scripts/tb2*` scratch files are excluded from the change set)

Added:

```
src/backend/Modules/Returns/Tooba.Returns.Domain/Aggregates/ReturnReceipt.cs
src/backend/Modules/Returns/Tooba.Returns.Domain/Aggregates/ReturnReceiptLine.cs
src/backend/Modules/Returns/Tooba.Returns.Domain/ValueObjects/ReturnReceivingSummary.cs
src/backend/Modules/Returns/Tooba.Returns.Application/ReturnRequests/Commands/RecordReturnReceiptCommand.cs
src/backend/Modules/Returns/Tooba.Returns.Application/Composition/ReturnItemConditionParser.cs
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.Receipts.cs
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Persistence/Migrations/20261116122318_AddCustomerReceiptsAndDecisionReason.cs
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Persistence/Migrations/20261116122318_AddCustomerReceiptsAndDecisionReason.Designer.cs
src/backend/Modules/Returns/Tooba.Returns.Tests/Behavior/ReturnsCustomerReceipts274Tests.cs
src/backend/Host/Tooba.Host.Tests/ReturnsCustomerReceipts274RelationalTests.cs
docs/ai/tasks/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274/TASK.md
docs/architecture/evidence/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274/*.md
```

Modified:

```
src/backend/Modules/Returns/Tooba.Returns.Application/ReturnRequests/Models/{ReturnSnapshot.cs,ReturnRecoveryDescription.cs}
src/backend/Modules/Returns/Tooba.Returns.Application/ReturnRequests/Ports/IReturnDirectory.cs
src/backend/Modules/Returns/Tooba.Returns.Application/Validation/{ReturnsRequestValidators.cs,ReturnsValidationCodes.cs}
src/backend/Modules/Returns/Tooba.Returns.Contracts/Errors/ReturnsErrorCodes.cs
src/backend/Modules/Returns/Tooba.Returns.Contracts/Operations/ReturnAdminOperationsContracts.cs
src/backend/Modules/Returns/Tooba.Returns.Contracts/Resources/{ReturnsErrors.resx,ReturnsErrors.fa.resx}
src/backend/Modules/Returns/Tooba.Returns.Endpoints/Admin/ReturnAdminEndpoints.cs
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Adapters/ReturnAdminOperationsAdapter.cs
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory{.cs,.CourierOrigin.cs,.Decisions.cs,.Recovery.cs}
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Errors/ReturnsErrorCatalogContributor.cs
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Observability/ReturnsInstrumentation.cs
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Persistence/{ReturnDecisionRecord.cs,ReturnsDbContext.cs,ReturnsDbContextModelSnapshot.cs}
src/backend/Modules/Returns/Tooba.Returns.Tests/Behavior/{ReturnsRecovery273Tests.cs,ReturnsCharacterizationTests.cs}
src/backend/Host/Tooba.Host.Tests/Architecture/{ReturnsModuleAmsc001W1MigrateGuardTests.cs,ReturnsModuleAmsc001W3CertGuardTests.cs,ReturnsModuleAmsc001W3R1SetEqualityGuardTests.cs}
src/backend/Modules/Order/Tooba.Order.Tests/Admin/{AdminOrderPostDispatchReplayTests.cs,MarketplaceOperationalPackingChoice259Tests.cs}  (compile-only fake of the extended port)
docs/{AGENTS.md,prompts/START-HERE-IF-CHATGPT-IS-LOST.md,planning/returns/RETURNS-AND-REFUNDS-COMPLETION-PLAN-272-276.md}
```

`FE` is untouched: no frontend file, build, restart or screenshot.
