# Cart policy

Cart is a shopping-intent container. Add/update validates Offer + sellable quantity and stores CartLine. No hard reservation. Lines stay if stock later changes; GET projects Available / LimitedQuantity / Unavailable. Persistence TTL is `Cart:PersistenceHours` (168), not inventory hold.
