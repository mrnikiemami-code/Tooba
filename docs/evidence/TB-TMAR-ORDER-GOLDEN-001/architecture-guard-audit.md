# Architecture guard audit

Existing durable guards cannot truthfully add Order to `CompleteHttpModules`: the endpoint project does not exist and Host authority remains.

Required R1 guards: endpoint project existence/mapping, ISender-only dispatch, Order MediatR registration, no Host Order endpoints/composers, no foreign Application/Infrastructure/DbContext dependencies, canonical time/ID, no message classification/silent catch, namespace/layout alignment, and recovery-state parity.

Order was deliberately not added to the COMPLETE manifest.
