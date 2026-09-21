# ProblemDetails / log / trace join — TB-TMAR-FND-OBSERR-001-R2

## Join keys

| Surface | Field |
| --- | --- |
| ProblemDetails | `correlationId`, `traceId` (+ optional `requestId`) |
| Response header | `X-Correlation-ID` |
| Log scope | `CorrelationId`, `TraceId`, `SpanId` |
| Activity | `tooba.correlation_id` |

## Proof

`CorrelationRuntimeTests`:

- Valid incoming GUID preserved (normalized N) on header + ProblemDetails for `/__platform-error`
- Invalid incoming replaced
- `/__platform-conflict` joins correlation + trace + errorCode
- Production-style body omits stack / raw unexpected message text on unexpected path (detail may show type name only in Development)

Middleware order places Correlation **outside** ExceptionHandler so AsyncLocal is alive during ProblemDetails write.
