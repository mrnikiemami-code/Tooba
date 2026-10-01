# Policy validation

CachePolicy.EnsureBounded rejects:
- missing absolute and sliding
- zero/negative absolute
- zero/negative sliding
- CacheNull without NullAbsoluteExpiration
- zero/negative NullAbsoluteExpiration
Both absolute and sliding when present must be positive.
