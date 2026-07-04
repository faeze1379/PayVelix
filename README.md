# PayVelix
# PayVelix

PayVelix is a .NET SDK for working with the PayVelix payment API.

The SDK currently supports:

- Create Payment
- Verify Payment

It uses the official PayVelix API base URL:

```text
https://api.payvelix.com
```

Authentication is sent with the `X-Api-Key` header. The SDK does not use
`Authorization: Bearer`.

## Projects

```text
PayVelix.Contracts  Public request/response DTOs and reusable exceptions
PayVelix            SDK implementation, HTTP client, options, DI
PayVelix.Tests      Unit tests
```

## Requirements

- .NET 8.0 or later
- A PayVelix API key

## Installation

If you are consuming the SDK from this solution directly, add project
references to both the SDK and contracts projects:

```powershell
dotnet add YourApp.csproj reference path\to\PayVelix\PayVelix.csproj
dotnet add YourApp.csproj reference path\to\PayVelix.Contracts\PayVelix.Contracts.csproj
```

The SDK uses `Microsoft.Extensions.DependencyInjection` and `IHttpClientFactory`
for configuration.

## Configuration

Register the SDK with dependency injection:

```csharp
using Microsoft.Extensions.DependencyInjection;
using PayVelix.DependencyInjection;

var services = new ServiceCollection();

services.AddPayVelix(options =>
{
    options.ApiKey = Environment.GetEnvironmentVariable("PAYVELIX_API_KEY")
        ?? throw new InvalidOperationException("PAYVELIX_API_KEY is not set.");
});
```

Available options:

```csharp
public sealed class PayVelixOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.payvelix.com";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

Never commit real API keys to source control. Prefer environment variables,
user secrets, or your deployment platform's secret store.

## Create Payment

Endpoint used by the SDK:

```text
POST /api/Payments/Create
```

Example:

```csharp
using Microsoft.Extensions.DependencyInjection;
using PayVelix;
using PayVelix.Contracts.Payments;
using PayVelix.DependencyInjection;

var serviceProvider = services.BuildServiceProvider();
var payVelix = serviceProvider.GetRequiredService<IPayVelixClient>();

var payment = await payVelix.Payments.CreateAsync(new CreatePaymentRequest
{
    Amount = 1,
    Currency = "USDT",
    ReturnUrl = "https://example.com/payment/return",
