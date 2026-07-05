# PayVelix Client

PayVelix Client is a .NET 8 SDK for integrating applications with the PayVelix payment API. It provides a small typed wrapper around the payment endpoints and is designed to work with `HttpClientFactory` and Microsoft Dependency Injection.

## Features

- Create payments through `/api/Payments/Create`
- Verify payments through `/api/Payments/{paymentId}/Verify`
- Send the API key with the `X-Api-Key` header
- Serialize and deserialize JSON with camelCase property names
- Preserve unknown response fields in `AdditionalData`
- Convert unsuccessful API responses to `PayVelixApiException`

## Project Structure

```text
PayVelix.Client/
|- PayVelix/              # Main client, DI setup, and HTTP implementation
|- PayVelix.Contracts/    # DTOs, enums, and shared exceptions
|- PayVelix.Tests/        # xUnit tests
`- README.md
```

## Requirements

- .NET SDK 8.0 or later
- A valid PayVelix API key

## Local Usage

During development, reference the projects from your consuming application:

```powershell
dotnet add <YourProject>.csproj reference .\PayVelix\PayVelix.csproj
dotnet add <YourProject>.csproj reference .\PayVelix.Contracts\PayVelix.Contracts.csproj
```

If this SDK is later published as a NuGet package, install the package instead of using local project references.

## Configuration

In ASP.NET Core or any application that uses `IServiceCollection`:

```csharp
using PayVelix.DependencyInjection;

builder.Services.AddPayVelix(options =>
{
    options.ApiKey = builder.Configuration["PayVelix:ApiKey"]
        ?? throw new InvalidOperationException("PayVelix API key is missing.");

    options.BaseUrl = builder.Configuration["PayVelix:BaseUrl"]
        ?? "https://api.payvelix.com";

    options.Timeout = TimeSpan.FromSeconds(30);
});
```

Available options:

| Option | Default | Description |
| --- | --- | --- |
| `ApiKey` | `string.Empty` | PayVelix API key. An empty value causes client creation to fail. |
| `BaseUrl` | `https://api.payvelix.com` | Base URL of the PayVelix API. |
| `Timeout` | `30s` | HTTP request timeout. |

Example `appsettings.json`:

```json
{
  "PayVelix": {
    "ApiKey": "your_api_key",
    "BaseUrl": "https://api.payvelix.com"
  }
}
```

## SDK Usage

Inject `IPayVelixClient` into your service after registering `AddPayVelix`. Payment operations are exposed through `payVelix.Payments`.

| Method | Purpose |
| --- | --- |
| `CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)` | Creates a payment and returns the payment identifier, amount, payment link, and expiration time. |
| `VerifyAsync(string paymentId, CancellationToken cancellationToken = default)` | Verifies an existing payment and returns its current status, paid amount, fees, currency, and network details. |

## Create a Payment

Use `CreateAsync` when your application needs to start a new payment.

```csharp
Task<CreatePaymentResponse> CreateAsync(
    CreatePaymentRequest request,
    CancellationToken cancellationToken = default);
```

```csharp
using PayVelix;
using PayVelix.Contracts.Payments;

public sealed class CheckoutService
{
    private readonly IPayVelixClient _payVelix;

    public CheckoutService(IPayVelixClient payVelix)
    {
        _payVelix = payVelix;
    }

    public async Task<CreatePaymentResponse> CreatePaymentAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        return await _payVelix.Payments.CreateAsync(
            new CreatePaymentRequest
            {
                Amount = 25.50m,
                Currency = "USD",
                ReturnUrl = $"https://example.com/payments/return?orderId={orderId}",
                WebhookUrl = "https://example.com/webhooks/payvelix",
                CallbackParams = new Dictionary<string, string>
                {
                    ["orderId"] = orderId
                }
            },
            cancellationToken);
    }
}
```

`Amount` must be greater than zero. Otherwise, `ArgumentException` is thrown.

Store the returned `PaymentId` in your own order or transaction record. If the API returns a `PaymentLink`, redirect the customer to that URL to complete the payment.

## Verify a Payment

Use `VerifyAsync` after a redirect, webhook, or background reconciliation job to fetch the latest payment state.

```csharp
Task<VerifyPaymentResponse> VerifyAsync(
    string paymentId,
    CancellationToken cancellationToken = default);
```

```csharp
using PayVelix;
using PayVelix.Contracts.Payments;

public sealed class PaymentVerificationService
{
    private readonly IPayVelixClient _payVelix;

    public PaymentVerificationService(IPayVelixClient payVelix)
    {
        _payVelix = payVelix;
    }

    public async Task<bool> IsPaidAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _payVelix.Payments.VerifyAsync(
            paymentId,
            cancellationToken);

        return string.Equals(
            payment.Status,
            PaymentStatus.Paid.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }
}
```

`paymentId` must not be null, empty, or whitespace. Otherwise, `ArgumentException` is thrown.

Compare `VerifyPaymentResponse.Status` with `PaymentStatus.Paid.ToString()` when you need to confirm that the payment has been completed.

## Models

### `CreatePaymentRequest`

| Property | Type | Description |
| --- | --- | --- |
| `ReturnUrl` | `string?` | URL where the customer is redirected after payment. |
| `Amount` | `decimal` | Payment amount. Must be greater than zero. |
| `Currency` | `string?` | Currency code. |
| `WebhookUrl` | `string?` | URL that receives PayVelix webhook callbacks. |
| `CallbackParams` | `Dictionary<string, string>?` | Custom parameters for tracking orders or callback context. |

### `CreatePaymentResponse`

| Property | Type |
| --- | --- |
| `PaymentId` | `string?` |
| `Amount` | `decimal` |
| `PaymentLink` | `string?` |
| `ExpiresAt` | `DateTimeOffset?` |
| `AdditionalData` | `Dictionary<string, JsonElement>?` |

### `VerifyPaymentResponse`

| Property | Type |
| --- | --- |
| `PaymentId` | `string?` |
| `Amount` | `decimal` |
| `PaidAmount` | `decimal` |
| `FeeAmount` | `decimal` |
| `ExpectedAmount` | `decimal` |
| `MerchantReceivableAmount` | `decimal` |
| `Currency` | `string?` |
| `Network` | `string?` |
| `Status` | `string?` |
| `ExpiresAt` | `DateTimeOffset?` |
| `AdditionalData` | `Dictionary<string, JsonElement>?` |

## Error Handling

For unsuccessful HTTP responses, the client throws `PayVelixApiException`:

```csharp
using PayVelix.Contracts.Common;

try
{
    var payment = await payVelix.Payments.VerifyAsync(paymentId, cancellationToken);
}
catch (PayVelixApiException ex)
{
    logger.LogWarning(
        "PayVelix request failed. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, Body: {Body}",
        ex.StatusCode,
        ex.ErrorCode,
        ex.ResponseBody);
}
```

Useful exception properties:

| Property | Description |
| --- | --- |
| `StatusCode` | HTTP status code returned by the API. |
| `ErrorCode` | PayVelix error code, when available. |
| `ResponseBody` | Raw response body for troubleshooting. |

The client supports both direct error responses such as `{"code":"...","message":"..."}` and nested error responses such as `{"error":{...}}`.

## Build and Test

```powershell
dotnet restore .\PayVelix\PayVelix.sln
dotnet build .\PayVelix\PayVelix.sln
dotnet test .\PayVelix\PayVelix.sln
```

## Implementation Notes

- `IPayVelixClient` is registered as scoped.
- `IPayVelixPaymentsClient` is registered with `AddHttpClient`.
- `Accept: application/json` is added automatically.
- `X-Api-Key` is built from `PayVelixOptions.ApiKey`.
- Empty or invalid successful API responses are converted to `PayVelixApiException`.
