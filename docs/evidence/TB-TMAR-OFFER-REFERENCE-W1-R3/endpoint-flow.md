# Endpoint flow

Offer seller CRUD routes authorize the seller and dispatch commands/queries through `ISender`.

Create and Patch no longer call the Host panel for mutation. The panel is used only for read enrichment after dispatch; price and inventory remain residual panel writes for R4.
