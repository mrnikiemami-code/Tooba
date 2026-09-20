# Inventory adapter
- OrderInventoryLifecycleAdapter in Tooba.Inventory.Application implements IOrderInventoryLifecyclePort
- Delegates to IInventoryDirectory (Inventory authority)
- Registered in InventoryModule: AddScoped<IOrderInventoryLifecyclePort, OrderInventoryLifecycleAdapter>
- No Order writes to Inventory tables
