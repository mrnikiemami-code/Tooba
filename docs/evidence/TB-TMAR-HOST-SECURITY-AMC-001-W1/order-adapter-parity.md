# order-adapter-parity — TB-TMAR-HOST-SECURITY-AMC-001-W1

## HostOrderSellerAuthorizer.ResolveAsync

| Before | After |
| --- | --- |
| catch PlatformHttpException → SemanticError(ex.ErrorCode) | catch SemanticException → return ex.Error |
| success → (actor, seller, null) | unchanged |

No `exception.Message` / text classification. Unknown exceptions still propagate. No broad `catch (Exception)`.

PlatformHttpException catch: **not retained** — sole `ISellerPanelAccess` impl is HostSellerPanelAccess → SellerPanelAccess (SemanticException only after W1).
