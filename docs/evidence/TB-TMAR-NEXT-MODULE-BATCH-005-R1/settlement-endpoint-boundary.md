# settlement-endpoint-boundary

Routes preserved (seller + admin). Host thin transport:

auth/bind → ISender → Settlement Application Command/Query → Result → ApiResponseFactory

No manual Results.Json business envelopes.
No catch InvalidOperationException → ex.Message.
No SettlementPanelComposer.

Settlement-Endpoint-State: HOST_THIN_TRANSPORT
