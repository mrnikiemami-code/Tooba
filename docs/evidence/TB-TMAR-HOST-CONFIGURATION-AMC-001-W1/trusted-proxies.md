# trusted-proxies — TB-TMAR-HOST-CONFIGURATION-AMC-001-W1

| Surface | State |
| --- | --- |
| PlatformOptionsValidator.ValidateTrustedProxies | FAIL_FAST whenever list configured |
| Blank / invalid entry | ValidateOptionsResult failure |
| Empty list | valid (forwarded headers not enabled) |
| Program KnownProxies | `IPAddress.Parse` — NO silent TryParse skip |

Tests: HostConfigurationAmcW1BehaviorTests TrustedProxies_* .
