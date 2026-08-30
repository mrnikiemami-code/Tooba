# TB-P07-T035 — Final data integrity

## Authoritative live status
- rootsDemo: 15
- categoriesDemo/total: 116/116
- brandsDemo/total: 22/22
- tagsDemo/total: 36/38
- productsTotal/Demo: 283/283
- Draft/Published/Archived: 283/0/0
- Admin grid totalCount: 283
- environment: Development; allowResetAndSeed: true

## Sample integrity (50 products page 1)
- media count == 5 and exactly one primary: PASS
- aggregate readiness Ready: 50/50
- brandless subset present: yes

## Workspace domain samples
See live-workspace-samples.json (11 products).

## Residual Published
Published=0; Archived=0; all Draft. T034-R1 cleanup remains effective.

## Conclusion
Data integrity PASS for accepted demo contract.
