# Boundary and telemetry

Host/Caching remains generic platform only; foreign module layers ZERO; module DbContext ZERO.
Telemetry dimensions: cache.provider / cache.namespace / cache.edition only.
Added: tooba.cache.type_mismatch, tooba.cache.factory.failure; stampede wait preserved.
No TenantId / full key / payload / user id in metrics or logs.
Redis packages absent; provider Memory|None only.
