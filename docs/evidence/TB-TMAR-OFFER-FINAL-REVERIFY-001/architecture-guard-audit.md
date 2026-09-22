# Architecture Guard Audit

Guards now enforce:

- no pricing/inventory business gateways or writes in Offer.Endpoints;
- price/inventory routes dispatch real Application commands through ISender;
- both command handlers exist and invoke owner Contracts;
- Endpoints has no Pricing.Contracts/Inventory.Contracts references;
- Offer Application/Infrastructure foreign references are Contracts-only;
- no type forwarding artifact or namespace/path exemption;
- Offer membership in the durable COMPLETE HTTP module manifest.
