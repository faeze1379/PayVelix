using PayVelix.Contracts.Payments;

namespace PayVelix.Payments;

public interface IPayVelixPaymentsClient
{
    Task<CreatePaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<VerifyPaymentResponse> VerifyAsync(
        string paymentId,
        CancellationToken cancellationToken = default);
}
