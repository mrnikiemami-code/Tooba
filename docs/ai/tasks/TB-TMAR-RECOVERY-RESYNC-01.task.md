PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-RECOVERY-RESYNC-01

Parent-Task:
TB-TMAR-OFFER-REF-W1

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
ISSUED

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Title:
Recovery Resync — Reconstruct Current Accepted TMAR State Before Any Further Offer Work

Task Type:
RECOVERY / READ-ONLY RESYNC

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

(Task body persisted from Bridge claim 476cb1e9; full Architect instructions require read-only resync evidence under docs/evidence/TB-TMAR-RECOVERY-RESYNC-01/ and Result contract fields Accepted-Lineage, Checkout-State, Offer-State, Offer-Module-Recovery-State, Reference-Module-Lineage, Next-Recommended-Task.)

END_TOOBA_TASK
