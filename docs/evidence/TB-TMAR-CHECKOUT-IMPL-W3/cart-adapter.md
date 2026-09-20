# Cart adapter
- CartConversionAdapter in Tooba.Cart.Application implements ICartConversionPort
- Delegates to ICartDirectory.ConvertAsync (Cart owns semantics + persistence)
- Registered in CartModule: AddScoped<ICartConversionPort, CartConversionAdapter>
- In-process now; future HTTP/gRPC/message adapter without Order changes
- Order does not write Cart tables / no shared repository / no Host involvement
