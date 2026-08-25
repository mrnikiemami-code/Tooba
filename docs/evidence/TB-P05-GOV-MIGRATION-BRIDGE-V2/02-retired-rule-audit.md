# Retired rule audit

Task: `TB-P05-GOV-MIGRATION-BRIDGE-V2`

Audit scope:

- root governance files;
- current operational documents under `docs/ai/`, `docs/prompts/`, and
  `docs/pipeline/`;
- historical `docs/ai/tasks/**` and prior `docs/evidence/**` classified
  separately and not edited.

## Classification

| Phrase / behavior | Current operational matches after migration | Classification | Resolution |
| --- | ---: | --- | --- |
| `WAITING_FOR_ARCHITECT_IN_SAME_SESSION` | 1 | CURRENT RETIRED NOTICE | Appears only in the explicit retired-behavior list in the canonical protocol |
| same Architect chat / same conversation | 0 | — | Removed from current operation |
| manual Cursor envelope / manual Task paste | 0 instructions | — | Current docs say the user does not paste Tasks; Bridge dispatches |
| `HUMAN/PIPELINE` conversational handoff | 1 | CURRENT RETIRED NOTICE | Appears only in the explicit retired-behavior list |
| `No Envelope = No Execution` chat transport | 1 | CURRENT RETIRED NOTICE | Appears only as the named retired chat-session mechanic |
| `Cursor must remain` | 0 | — | No current dependency |
| Architect chat Composer / send button | 1 retired summary | CURRENT RETIRED NOTICE | Canonical protocol names the transport only to prohibit it |
| `Cursor PASS != Architect ACCEPT` | 0 | — | Replaced by `Worker PASS != Architect ACCEPT` |

The controller's statement that it must not read an Architect chat, paste Tasks
or Results, or drive a browser composer is a prohibition, not an operational
instruction to perform those actions.

## Historical evidence

Legacy matches remain under:

```text
docs/ai/tasks/**
docs/evidence/**
```

Classification: `HISTORICAL_EVIDENCE`.

These files record prior execution and may retain
`BEGIN_TOOBA_CURSOR_*`, same-session waiting, manual envelope, and chat
transport syntax. They are not a queue and are not current operational
instructions. They were preserved without mass editing.

## Verdict

```text
Unresolved CURRENT_OPERATIONAL conflicts: 0
```
