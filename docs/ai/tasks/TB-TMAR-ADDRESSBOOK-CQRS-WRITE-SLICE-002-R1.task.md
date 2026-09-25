PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1
Parent-Task: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: Repair missing Delete + SetDefault transport validators only
Backend-Only: YES

PARENT-COMMIT:
92a7f9688f72e56cfe9c108bbcca6ff09ab49ac6

ARCHITECT-VERDICT-ON-PARENT:
PARTIAL_ACCEPTANCE_ONLY

DeleteCustomerAddressCommand + Handler: PRESENT
SetDefaultCustomerAddressCommand + Handler: PRESENT
claimed validators: MISSING
endpoint migration: NOT STARTED
Host six-route ownership: UNCHANGED

TIMEBOX:
Target <= 4 minutes.
Hard maximum 6 minutes.
No retry loop.
No scope expansion.

ONE OBJECTIVE ONLY:

Add the two missing AddressId-only validators that the parent Result claimed but did not commit.

CREATE EXACTLY:

src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Validators/Customer/Delete/DeleteCustomerAddressCommandValidator.cs

src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Validators/Customer/SetDefault/SetDefaultCustomerAddressCommandValidator.cs

VALIDATION RULE:

Both validators:

derive from AbstractValidator<T>
validate only AddressId
reuse existing AddressBookFluentRules.RequireId(...)
reuse existing AddressBookValidationCodes.AddressIdRequired
stable error code must remain:
customer.address.id_required

Do NOT validate ActorUserId.

Do NOT add:

new helper
new validation code
new business rule
HTTP mapping
error translation

DO NOT MODIFY:

DeleteCustomerAddressCommand.cs
SetDefaultCustomerAddressCommand.cs
any other CQRS file unless compilation strictly requires a namespace/import correction
Host/AddressBook/**
Program.cs
AddressBookEndpointModule.cs
AddressBook routes
AddressBookDevelopmentSeed.cs
root structure
schema/migrations
frontend

POST-REPAIR EXPECTATION:

AddressBook endpoint-reachable CQRS inventory:

6 requests total
VALIDATOR_REQUIRED = 5
Get
Create
Update
Delete
SetDefault
NO_VALIDATOR_REQUIRED = 1
List
validators present = 5
validator gap = ZERO

FOCUSED VALIDATION:

Run only:

dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Tooba.AddressBook.Application.csproj --no-restore

Optionally, if already cheap:
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

No solution build.
No tests unless an existing tiny validator guard already exists and is directly relevant.
No broad architecture suite.
No retries.

CANONICAL TASK ARTIFACT:

Commit exact task at:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1/missing-validator-repair.md

Evidence must include:

parent discrepancy: Result claimed two validators, commit had none
exact two new validator files
exact rule/error code
6-request inventory
5 required / 5 present / 1 no-validator-required
Host routes unchanged
Endpoints route count still ZERO
production scope limited to two validator files

PASS ONLY IF:

both missing validator files exist
both compile
both validate AddressId only
ActorUserId is not validated
no unrelated production file changed
no route moved
Host still owns six routes
canonical task/evidence committed

If safe completion exceeds 6 minutes:
Status = INCOMPLETE
STOP.
Do not start endpoint migration.
Do not loop.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1
Parent-Task: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Discrepancy-State:
Delete-Validator-State:
SetDefault-Validator-State:
Validator-Rule-State:
Validator-Coverage-State:
Total-AddressBook-CQRS-UseCases:
Host-Route-Ownership-State:
New-Endpoint-Route-Count:
Focused-Builds:
Focused-Tests:
Production-Code-Scope:
Test-Code-Scope:
Canonical-Task-State:
Evidence:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start endpoint migration.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
