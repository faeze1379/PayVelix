using PayVelix.Payments;

namespace PayVelix;

public interface IPayVelixClient
{
    IPayVelixPaymentsClient Payments { get; }
}
