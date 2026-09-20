# Next Implementation Tasks

## 1) TB-TMAR-FND-001 — Architecture Foundation (FIRST)

Scope: install MediatR **12.5.0** + FluentValidation; add IClock + IIdGenerator; pipeline behaviors; Host freeze architecture tests; error-code foundation if safe.  
No mass Directory conversion · no Host cleanup wave · no folder moves · no Redis.  
Dep: Baseline ACCEPT · Effort: M · Risk: M  
Proof: packages present; sample Handler smoke; freeze tests fail on new Host SaveChanges; HEAD==origin/main

## 2) TB-TMAR-HOST-W1 — Host Dangerous Writes Removal (slice 1)

Scope: StoreLanding/Menu/Appearance write paths → module Commands.  
Dep: FND-001 · Effort: L · Risk: H · Proof: Host SaveChanges removed from those files; API parity

## 3) TB-TMAR-CONTRACT-001 — Pricing/Offer/Inventory/Catalog Contracts extraction

Dep: FND-001 · Effort: L · Risk: M · Proof: Cart/Order depend on Contracts not Application

## 4) TB-TMAR-CQRS-001 — Strangler Stage A for Cart/Checkout entrypoints

Dep: FND-001 · Effort: M · Risk: M · Proof: endpoints use ISender → Handler → Directory

## 5) TB-TMAR-OWN-001 — Move Storefront settings/landing/menu types out of Catalog Domain

Dep: HOST-W1 + CONTRACT · Effort: L · Risk: H · Proof: ownership registry + schema migration plan

## 6) TB-TMAR-READ-001 — Read gateways for StorefrontComposer

Dep: CONTRACT-001 · Effort: L · Risk: M · Proof: composer uses gateways only

## 7) TB-TMAR-ERR-LOC-001 — ErrorCode + locale policy foundation rollout

Dep: FND-001 · Effort: M · Risk: M · Proof: sample Domain path uses codes; ProblemDetails localized

## 8) TB-TMAR-CACHE-001 — Migrate IMemoryCache bypasses to ICache

Dep: FND-001 · Effort: S · Risk: S · Proof: no IMemoryCache in listed composers
