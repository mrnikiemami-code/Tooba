# Foundation adoption scan — TB-TMAR-NEXT-MODULE-BATCH-002

## Clock / ID bypasses (pre-repair)

### Wallet
- Domain factories: UuidV7.New() (AccountId, EntryId, CardId, RedemptionId)
- WalletDirectory: DateTimeOffset.UtcNow (multiple), Guid.NewGuid() (adjustmentId)
- No IClock/IIdGenerator ctor params
- GiftCard.GenerateDisplayCode uses RandomNumberGenerator (OK — not Guid/UtcNow)

### Payment
- Domain factories: Guid.NewGuid() (AllocationId, AttemptId, ProofAssetRowId, PaymentId)
- PaymentDirectory / gateways / webhook: DateTimeOffset.UtcNow extensively
- PaymentWebhookInboxRecord: Guid.NewGuid()
- Fake/Manual/Webhook/Wallet gateways: UtcNow for expiry
- No IClock/IIdGenerator on PaymentDirectory

### Fallback ctors
- None of form `clock ?? new SystemUtcClock()` observed (deps simply missing)

## Silent / catch-all
- PaymentDirectory refund path: `catch (Exception)` swallow/mark failure
- WalletDirectory: catch(DbUpdateException) for concurrency — keep with rethrow/logic, not empty ignore
- Host webhook: catch JsonException for invalid payload (transport) — Host residual

## Localized exception prose
- Wallet Domain + WalletEnumParsing: Persian/spaced InvalidOperationException messages
- Payment Domain: Persian prose throws
- Target: stable codes (`wallet.*`, `payment.*`, `domain.*`)

## Observability
- No raw StartActivity in Wallet
- PaymentGatewayInstrumentation: review; replace with IModuleCallTracer only for Payment→Wallet Contracts calls (WalletPaymentGateway)

## PlatformHttpException / Results.Json
- Host WalletEndpoints + HoldPolicySettings + webhook: Host transport residual (Endpoint-State NOT_APPLICABLE for module Endpoints; no owned Result HTTP in module)

## Result pattern
- Directory methods mostly throw Semantic/InvalidOperation — not full Result adoption (Inventory-style directory ports; document Result-Adoption as PORT_THROW_CODES unless existing Result returns found)
