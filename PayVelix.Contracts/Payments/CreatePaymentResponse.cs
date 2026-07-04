using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayVelix.Contracts.Payments;

public sealed class CreatePaymentResponse
{
    public string? PaymentId { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentLink { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
