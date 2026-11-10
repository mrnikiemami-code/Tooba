namespace Tooba.Fulfillment.Domain.ValueObjects;


/// <summary>
/// TB-258: physical condition of a physically received returned line. Only <see cref="Usable"/> goods may become
/// resend capacity; damaged goods are received and recorded but never resendable, and missing goods are simply not
/// part of the receipt.
/// </summary>
public enum ReturnReceiptCondition
{
    /// <summary>Received and physically usable.</summary>
    Usable = 0,

    /// <summary>Received but damaged / not fit to resend.</summary>
    Damaged = 1,
}
