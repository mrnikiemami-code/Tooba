# Source-size compliance — TB-TMAR-CONTRACTS-W3

New files under 800 LOC:
- `WalletRefundCreditPort.cs`
- characterization test files

Touched oversized legacy files: minimal diff only (ReturnDirectory constructor type; WalletDirectory explicit interface; OrderContracts using).

No structural god-file decomposition.
`Hand_written_source_size_does_not_expand_beyond_baseline` expected green.
