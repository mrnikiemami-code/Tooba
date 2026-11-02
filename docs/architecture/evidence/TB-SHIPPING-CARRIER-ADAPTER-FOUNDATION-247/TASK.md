# TB-SHIPPING-CARRIER-ADAPTER-FOUNDATION-247

Build an extensible carrier adapter boundary and add Tipax V4 as the first rate provider. Use the
official V4 Pricing wire contract; keep credentials out of persistence and source; keep the adapter
disabled by default; do not activate Checkout before owner configuration, Geography mapping and quote
evidence are implemented. Preserve existing Manual and Free behavior. Record the remaining activation
slices explicitly so adding another carrier is additive.
