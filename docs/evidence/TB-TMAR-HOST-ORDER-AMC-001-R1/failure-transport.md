# Failure transport

Host throws SemanticException(SemanticError(FoundationErrorCodes.CheckoutAuthenticationRequired)).
StorefrontOrderResult catches SemanticException and maps to Result.Failure with same stable code.
StorefrontOrderException Application type is ZERO on Host/Order.
