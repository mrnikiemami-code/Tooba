# Anti-pattern scan

CLEAN: no ReserveAsync in AddToCart; no GET mutation; no hold-renewal timer; no magic TTL in new paths (`Cart:PersistenceHours` + Payment:Gateway holds); no silent line drop; no duplicate Order on retry; no first-seller shortcut; customer errors stay coded (`cart.inventory.insufficient`, `checkout.inventory.unavailable`); failed commit releases acquired holds.
