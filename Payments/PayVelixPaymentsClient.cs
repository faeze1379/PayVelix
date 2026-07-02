using System.Net;
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
            return DeserializeSuccessResponse<CreatePaymentResponse>(
                responseBody,
                response.StatusCode,
                "create payment");
        }

        throw CreateApiException(response.StatusCode, responseBody);
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
            return DeserializeSuccessResponse<VerifyPaymentResponse>(
                responseBody,
                response.StatusCode,
                "verify payment");
        }

        throw CreateApiException(response.StatusCode, responseBody);
    }

    private static T DeserializeSuccessResponse<T>(
        string responseBody,
        HttpStatusCode statusCode,
        string operationName)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new PayVelixApiException(
                "PayVelix returned an empty response.",
                statusCode,
                responseBody: responseBody);
        }

        var result = JsonSerializer.Deserialize<T>(
            responseBody,
            JsonOptions);

        return result ?? throw new PayVelixApiException(
            $"Unable to deserialize PayVelix {operationName} response.",
            statusCode,
            responseBody: responseBody);
    }

    private static PayVelixApiException CreateApiException(
        HttpStatusCode statusCode,
        string responseBody)
    {
        var error = TryDeserializeError(responseBody);

        return new PayVelixApiException(
            error?.Message ?? "PayVelix API request failed.",
            statusCode,
            error?.Code,
            responseBody);
    }

    private static PayVelixErrorResponse? TryDeserializeError(string responseBody)
    {
        try
        {
            var errorEnvelope = JsonSerializer.Deserialize<PayVelixErrorEnvelope>(
                responseBody,
                JsonOptions);

            if (errorEnvelope?.Error is not null)
            {
                return errorEnvelope.Error;
            }
        }
        catch
        {
            // Preserve raw response body even if it is not valid JSON.
        }

        try
        {
            return JsonSerializer.Deserialize<PayVelixErrorResponse>(
                responseBody,
                JsonOptions);
        }
        catch
        {
            // Preserve raw response body even if it is not valid JSON.
            return null;
        }
    }
}
