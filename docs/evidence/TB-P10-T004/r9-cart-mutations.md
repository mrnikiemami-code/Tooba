# Cart mutations

- Add: validate + persist line; no ReserveAsync
- Increase: validate quantity; release historical cart hold if present; no new hold
- Remove/abandon/expire: release only if CartLine.ReservationId exists
- GET: batched availability; no mutation
