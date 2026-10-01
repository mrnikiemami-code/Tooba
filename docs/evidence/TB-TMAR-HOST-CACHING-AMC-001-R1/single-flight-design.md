# Single-flight design

CacheInflightCoordinator:
- Acquire under slot.Sync; reject Retired; verify dictionary membership; increment RefCount
- Release: decrement under Sync; optional test window; re-enter Sync; if RefCount!=0 or Retired skip remove; else Retired=true and TryRemove
- Gate SemaphoreSlim never disposed
