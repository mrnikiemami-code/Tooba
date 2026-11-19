# CORRECTION-ADDENDUM — TB-282-R9

- R8 fixed Snapshot bind but Story remained 403 because InMemory tuples were empty under skip-bootstrap.
- Recovery projects only from durable AccessControl assignments for the trusted tenant; it does not treat admin email/Snapshot as membership.
- Party membership rows without a matching AccessControl seller-scope assignment remain denied (explicit fail-closed; not invented).
