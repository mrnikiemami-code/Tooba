# Inventory Event Boundary Audit

Inventory integration events and messaging adapters are isolated under Infrastructure/Events and Infrastructure/Messaging. Return restock is inbox-backed and idempotent. Incoming/outgoing interactions use Contracts/events without foreign Application handler coupling; existing outbox/inbox behavior is preserved.

Verdict: transport can be replaced during extraction without business rewrite.
