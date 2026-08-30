# Live Attribute Inheritance / Override (TB-P07-T036-R1)

Source: `live-r1-proof.json` section C (**9/9 pass**)

## Sequence
1. Demo L1→L2→L3 chain (digital gadgets ancestry).
2. Bind AttributeDefinition on L1; effective schema on L2/L3 shows DefinitionId **once**.
3. L2 override (same DefinitionId, different binding flags).
4. L3 effective follows nearest (L2) override — no duplicate entry.
5. Unbind L2 override → fallback to L1.
6. Unbind L1 restore baseline.

## APIs
- `GET …/attribute-schema/effective`
- `POST/DELETE …/attribute-schema/bindings[/{definitionId}]`
