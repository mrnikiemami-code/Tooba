# Performance — TB-P09-T021

Additive reads only: packages + membership lookups keyed by checkout / shipment ids. No N+1 cross-schema joins. Projection loads packages once per Admin detail / operations page. Orchestration reuses existing per-shipment dispatch/deliver (sequential members, same transaction boundaries as directory).
