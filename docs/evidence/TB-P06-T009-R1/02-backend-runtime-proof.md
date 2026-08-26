# 02 — Backend runtime proof (TB-P06-T009-R1)

Probed after backend restart (post build/test):

```text
GET http://127.0.0.1:5088/health/live  -> 200
GET http://127.0.0.1:5088/health/ready -> 200
```

Interpretation: Host process healthy and ready checks pass at runtime.
