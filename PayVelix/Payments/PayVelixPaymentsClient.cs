using System.Net.Http.Json;
using PayVelix.Contracts.Payments;
using PayVelix.Internal;

namespace PayVelix.Payments;

internal sealed class PayVelixPaymentsClient : IPayVelixPaymentsClient
{
    private const string CreatePaymentPath = "/api/Payments/Create";

    private readonly HttpClient _httpClient;

    public PayVelixPaymentsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreatePaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(request));
        }

        using var response = await _httpClient.PostAsJsonAsync(
            CreatePaymentPath,
            request,
            PayVelixHttp.JsonOptions,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return PayVelixHttp.DeserializeSuccessResponse<CreatePaymentResponse>(
                responseBody,
                response.StatusCode,
                "create payment");
        }

        throw PayVelixHttp.CreateApiException(response.StatusCode, responseBody);
    }

    public async Task<VerifyPaymentResponse> VerifyAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            throw new ArgumentException("PaymentId is required.", nameof(paymentId));
        }

        var escapedPaymentId = Uri.EscapeDataString(paymentId);
        var path = $"/api/Payments/{escapedPaymentId}/Verify";

        using var response = await _httpClient.GetAsync(path, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return PayVelixHttp.DeserializeSuccessResponse<VerifyPaymentResponse>(
                responseBody,
                response.StatusCode,
                "verify payment");
        }

        throw PayVelixHttp.CreateApiException(response.StatusCode, responseBody);
    }
}
