# TB-P0-KEYED-CONSUME-FAIL-CLOSED-004R — Task Receipt

Persisted claim artifact — full Architect body received via Bridge
`GET /api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`
at Bridge task UUID `056ca75b-9d24-4afd-928c-252f4ce586ad` (createdAtUtc 2026-10-12T14:27:13.8029893Z).

* Task-ID: `TB-P0-KEYED-CONSUME-FAIL-CLOSED-004R`
* Parent-Task: `TB-P0-FULFILLMENT-KEYED-CONSUME-WIRING-004`
* Program: `TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY`
* Channel: `tooba-main`, WorkerId: `tooba-worker-01`, AgentType: `cursor`
* Mode: `IMPLEMENT_MINIMAL_FAIL_CLOSED_CORRECTION_STOP_FOR_ACCEPTANCE`
* Starting HEAD (required): `5f9e4d0fcb332317ce5c4d0089a6b43f301e9765`
* Skill: `.cursor/skills/tooba-backend-feature/SKILL.md` (read before coding)

## Scope actually executed

1. Replaced the silent unkeyed fallback in `IFulfillmentInventoryGateway`'s keyed default interface
   member with a deterministic explicit failure (`NotSupportedException`, stable text) so an
   implementation lacking keyed capability can never deduct inventory.
2. Corrected the inaccurate 78-character claim in the key-format comment to the true 111 characters
   (`4 + 32 + 5 + 32 + 6 + 32`); the key format itself is unchanged.
3. Added ONE targeted regression test proving a legacy gateway double without the keyed
   implementation fails closed, does not call its unkeyed method, and leaves no member dispatched.

Touched: one production file + one test file + this receipt + compact evidence. No W6 change, no
Inventory schema/port/journal change, no package-routing change, no new workflow.

## Result contract

Per Bridge Task body — `BEGIN_TOOBA_WORKER_RESULT … END_TOOBA_WORKER_RESULT` posted to
`POST /api/results` only after validate + commit + push + `HEAD == origin/main`, then complete the
Bridge task lifecycle and stop.

## Stop rule

STOP for Architect independent acceptance. Do NOT start the next task.
