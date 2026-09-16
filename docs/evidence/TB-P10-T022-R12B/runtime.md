# Runtime — TB-P10-T022-R12B

- Host http://127.0.0.1:5088
- FE http://127.0.0.1:3000
- ok=true
- errors=none

## Steps
- PASS api-tile-ceramic-status — {"status":200}
- PASS api-tile-ceramic-counts — {"products":15,"roots":8,"brands":6,"pure":true}
- PASS api-interior-decor-status — {"status":200}
- PASS api-interior-decor-counts — {"products":15,"roots":8,"brands":6,"pure":true}
- PASS api-home-appliances-status — {"status":200}
- PASS api-home-appliances-counts — {"products":15,"roots":8,"brands":6,"pure":true}
- PASS file:tile-ceramic-preview-desktop.png — {"bytes":324092}
- PASS file:tile-ceramic-preview-mobile.png — {"bytes":111264}
- PASS tile-ceramic-sample-purity — {"purity":"pure"}
- PASS tile-ceramic-store-no-template-origin — {"storeOrigin":"operational-store-catalog","storePurity":"store"}
- PASS file:interior-decor-preview-desktop.png — {"bytes":149955}
- PASS file:interior-decor-preview-mobile.png — {"bytes":75883}
- PASS interior-decor-sample-purity — {"purity":"pure"}
- PASS interior-decor-store-no-template-origin — {"storeOrigin":"operational-store-catalog","storePurity":"store"}
- PASS file:home-appliances-preview-desktop.png — {"bytes":73717}
- PASS file:home-appliances-preview-mobile.png — {"bytes":45958}
- PASS home-appliances-sample-purity — {"purity":"pure"}
- PASS home-appliances-store-no-template-origin — {"storeOrigin":"operational-store-catalog","storePurity":"store"}
- PASS file:batch-b-template-selector.png — {"bytes":116910}
- PASS file:tile-ceramic-template-overview.png — {"bytes":116902}
- PASS file:interior-decor-template-overview.png — {"bytes":116119}
- PASS file:home-appliances-template-overview.png — {"bytes":142432}
- PASS file:batch-b-use-template.png — {"bytes":76884}
- PASS use-template-sections — {"sectionCount":0,"url":"http://127.0.0.1:3000/admin/landing-pages/01a0ab09-fb00-7000-bb0e-6ee7d2d45878"}
