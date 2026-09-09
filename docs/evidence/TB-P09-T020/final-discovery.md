# Final discovery — TB-P09-T020

Correct: multi-shipment domain, quantity snapshots, cancel-after-dispatch gate, Return/Refund split, invoice headers, settlement compensation.

Repair required: `ProcessSelections`/`PackSelections` treated `Dispatched`/`InTransit` as warehouse-terminal, blocking remainder 0.75 after dispatching 0.50.

Out of scope: Orders redesign, workflow engine, visual accept, T021.
