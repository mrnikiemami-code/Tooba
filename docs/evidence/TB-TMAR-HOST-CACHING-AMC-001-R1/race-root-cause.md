# Race root cause

Parent ReleaseInflight used Interlocked.Decrement to zero then TryRemove(KeyValuePair) without mutual exclusion against Acquire.

Interleaving: A hits zero; B GetOrAdd+Increment attaches; A TryRemove still succeeds by key/value; C creates a new slot — split-brain gates for the same key.
