# validation — TB-TMAR-HOST-SECURITY-AMC-001-W1

## Builds

- Tooba.Host: PASS
- Tooba.Host.Tests: PASS

## Focused tests PASS

- SellerPanelAuthorizationTests (all)
- SettingsFoundationTests seller capability / foreign actor (PlatformHttp→SemanticException)
- ReviewsFoundationTests.Foreign_seller_party_header_is_denied_by_seller_panel_access
- StoryFoundationTests seller panel actor/deny assertions
- HostSellerAmcR1GuardTests.Host_order_and_support_authorizers… (SemanticException catch)

HostSecurityAmcGuardTests (stale 18-count): **not used as W1 gate** (deferred W2).
