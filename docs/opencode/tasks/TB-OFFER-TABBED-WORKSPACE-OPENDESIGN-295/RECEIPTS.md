# RECEIPTS — TB-295 (design-only)
- od automation create --target new-project → routine-be8dff93-10ef-45ce-9f4b-e07b4b349708
- od automation run → project routine-da1eab56-6b62-4dd7-8392-48932da26db4, conversation
  routine-conv-0d466675-d1e2-4810-b9fc-9c56bdaea44b, agentRun fb0aae91-3e55-4328-ae6f-a7662f142efc
- run state → status=succeeded, artifactCount=1, artifactPaths=[tooba-offer-form.html]
- origin tooba-offer-form.html 391,881 B; hash DA8AAFBF…6F01A2; delivered to D:\Tooba-Offer-OpenDesign\295\FINAL\index.html (same hash)
- od lint final → p0=0,p1=0,p2=1
- Chromium verify → 7 tabs, distinct per-tab content, capabilities per tab, create pickers + disabled deps + no fake history,
  beforeunload guard, overflow=0 at 360/390/768/1280/1440
- Evidence: evidence/{requirements.md,artifact.json,agent-run-state.json,delivery-hash-proof.json,verify-tabs.json,
  verify-capabilities.json,opendesign-lint-295.json,tab-1..7.png,mobile-360.png}
- No product code changed.
