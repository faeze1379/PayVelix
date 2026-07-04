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
|- PayVelix.SandboxConsole/ # Manual sandbox smoke-test console app
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
    "ApiKey": "pv_sandbox_test_key",
    "BaseUrl": "https://api.payvelix.com"
  }
}
```

## Sandbox Console Manual Test

The repository includes `PayVelix.SandboxConsole`, a small console app for manually testing a sandbox API key against the PayVelix API. It creates a test payment and can immediately verify the returned `paymentId`.

Set the API key as an environment variable so it is not stored in source code or shell history:

```powershell
$env:PAYVELIX_API_KEY = "your_sandbox_api_key"
```

Run the console app:

```powershell
dotnet run --project .\PayVelix.SandboxConsole
```

By default, the console app uses:

| Setting | Environment variable | Default |
| --- | --- | --- |
| API key | `PAYVELIX_API_KEY` | Required |
| Base URL | `PAYVELIX_BASE_URL` | `https://api.payvelix.com` |
| Amount | `PAYVELIX_AMOUNT` | `1` |
| Currency | `PAYVELIX_CURRENCY` | `USD` |
| Return URL | `PAYVELIX_RETURN_URL` | `https://example.com/payvelix/return` |
| Webhook URL | `PAYVELIX_WEBHOOK_URL` | Empty |

You can also override values with command-line options:

```powershell
dotnet run --project .\PayVelix.SandboxConsole -- --amount 5 --currency USD --base-url https://api.payvelix.com
```

To create a payment without running the verification prompt:

```powershell
dotnet run --project .\PayVelix.SandboxConsole -- --skip-verify
```

To see all available options:

```powershell
dotnet run --project .\PayVelix.SandboxConsole -- --help
```

## Create a Payment

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

## Verify a Payment

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
