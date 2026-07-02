using PayVelix.Payments;

namespace PayVelix;

internal sealed class PayVelixClient : IPayVelixClient
{
    public PayVelixClient(IPayVelixPaymentsClient payments)
    {
        Payments = payments;
    }

    public IPayVelixPaymentsClient Payments { get; }
}
