# Anti-pattern scan

| Pattern | Status |
| --- | --- |
| Fashion hardcoded separate page renderer | CLEAN — uses StorefrontLandingSections / shared renderer |
| Screenshot-only fake preview | CLEAN — live iframe route |
| CSS zoom as mobile/tablet | CLEAN — px viewport resize |
| Unrelated generic products | CLEAN — fashion titles/categories/images |
| Untraceable demo records | CLEAN — origin marker + demo-fashion-* IDs |
| Destructive cleanup | CLEAN — buttons disabled |
| Admin theme leak | CLEAN — iframe sandbox boundary |
| Cart/auth mutation from preview | CLEAN — click/submit blocked |
| All industries implemented | CLEAN — Fashion pilot only |
