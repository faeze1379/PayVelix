using System.Net.Http.Json;
using System.Text.Json;
using PayVelix.Contracts.Common;
using PayVelix.Contracts.Payments;

namespace PayVelix.Payments;

internal sealed class PayVelixPaymentsClient : IPayVelixPaymentsClient
{
    private const string CreatePaymentPath = "/api/Payments/Create";

    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

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
            JsonOptions,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new PayVelixApiException(
                    "PayVelix returned an empty response.",
                    response.StatusCode,
                    responseBody: responseBody);
            }

            var result = JsonSerializer.Deserialize<CreatePaymentResponse>(
                responseBody,
                JsonOptions);

            return result ?? throw new PayVelixApiException(
                "Unable to deserialize PayVelix create payment response.",
                response.StatusCode,
                responseBody: responseBody);
        }

        PayVelixErrorResponse? error = null;

        try
        {
            error = JsonSerializer.Deserialize<PayVelixErrorResponse>(
                responseBody,
                JsonOptions);
        }
        catch
        {
            // Preserve raw response body even if it is not valid JSON.
        }

        throw new PayVelixApiException(
            error?.Message ?? "PayVelix API request failed.",
            response.StatusCode,
            error?.Code,
            responseBody);
    }
}
