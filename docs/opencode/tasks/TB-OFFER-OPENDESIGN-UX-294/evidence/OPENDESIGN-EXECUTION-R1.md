# OpenDesign real execution — TB-294-R1 (OpenDesign AI generated)
Project: routine-1a39bd38-5ca2-4610-973b-f9a7737e71ac
Conversation: routine-conv-f715b6fb-b235-455a-8afd-0451fef84003
Agent run: 4732b9f0-8c38-4843-bd25-ad3de5e5713a   (agentId: opencode; model: metisdeepseek/deepseek-v4-flash*)
Automation routine: routine-66b23571-9d07-41b8-a9b4-401c11176c70 (target = new project per run)
Generation: `od automation run` dispatched the prompt to OpenDesign's own agent; it wrote
  opendesign-output/toba-offer-workspace.html (≈219 KB) + assets/ into the project dir
  C:\OpenDesign\.od\projects\routine-1a39bd38-...\
MCP: config valid in ~/.config/opencode/opencode.json; MCP server process running (node cli.js mcp).
Root cause of HTTP 400: legacy inline `POST /api/projects` body was removed in the daemon
  (server.js: "legacy POST /api/projects body deleted — see registerProjectRoutes"); supported path is
  automation run (target new-project) / registerProjectRoutes — which is what was used.
OpenDesign lint on generated artifact: p0=0, p1=0, p2=1 (missing data-od-id anchors).
Render: HTTP 200, title «توبا — فضای کاری پیشنهادها», 24 sections, Persian/RTL.
View: D:\Tooba-Offer-OpenDesign\opendesign-output\toba-offer-workspace.html  (UI: http://127.0.0.1:7456 project routine-1a39bd38-...)
* model shown on prior successful daemon runs; this run's state.model was null while still status=running,
  though the artifact was fully produced and rendered.
