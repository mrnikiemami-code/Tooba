# TB-P10-T004-R4 — Performance

- AwaitingAdmin: `shouldPollStorefrontPayment` false → interval never started / cleared immediately
- Terminal Succeeded/Failed/Cancelled: no ongoing poll
- Transient online Pending/Processing/Verifying: poll ≤20s, 1.5s cadence, single in-flight GET
- Source contract verified in `r4-runtime-raw.json` polling-contract step
- Host rapid 401 loop eliminated when FE uses committed proof after Cart clear
