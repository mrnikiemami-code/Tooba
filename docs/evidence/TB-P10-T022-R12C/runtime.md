# Runtime — TB-P10-T022-R12C

- Host http://127.0.0.1:5088
- FE http://127.0.0.1:3000
- ok=true
- errors=none

## Steps
- PASS api-shoes-status — {"status":200}
- PASS api-shoes-counts — {"products":15,"roots":8,"brands":6,"pure":true}
- PASS api-plants-status — {"status":200}
- PASS api-plants-counts — {"products":15,"roots":8,"brands":6,"pure":true}
- PASS api-beauty-status — {"status":200}
- PASS api-beauty-counts — {"products":15,"roots":8,"brands":6,"pure":true}
- PASS file:shoes-preview-desktop.png — {"bytes":127153}
- PASS file:shoes-preview-mobile.png — {"bytes":69003}
- PASS shoes-sample-purity — {"purity":"pure"}
- PASS shoes-store-no-template-origin — {"storeOrigin":"operational-store-catalog","storePurity":"store"}
- PASS file:plants-preview-desktop.png — {"bytes":263320}
- PASS file:plants-preview-mobile.png — {"bytes":76145}
- PASS plants-sample-purity — {"purity":"pure"}
- PASS plants-store-no-template-origin — {"storeOrigin":"operational-store-catalog","storePurity":"store"}
- PASS file:beauty-preview-desktop.png — {"bytes":157197}
- PASS file:beauty-preview-mobile.png — {"bytes":73567}
- PASS beauty-sample-purity — {"purity":"pure"}
- PASS beauty-store-no-template-origin — {"storeOrigin":"operational-store-catalog","storePurity":"store"}
- PASS file:batch-c-template-selector.png — {"bytes":136593}
- PASS file:shoes-template-overview.png — {"bytes":136593}
- PASS file:plants-template-overview.png — {"bytes":210369}
- PASS file:beauty-template-overview.png — {"bytes":136603}
- PASS file:batch-c-use-template.png — {"bytes":76396}
- PASS use-template-sections — {"sectionCount":0,"url":"http://127.0.0.1:3000/admin/landing-pages/01a0ab1c-3c49-7000-9015-71fcbbc7e422"}
