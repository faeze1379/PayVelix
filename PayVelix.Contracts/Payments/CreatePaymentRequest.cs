namespace PayVelix.Contracts.Payments;

public sealed class CreatePaymentRequest
{
    public string? ReturnUrl { get; set; }

    public decimal Amount { get; set; }

    public string? Currency { get; set; }

    public string? WebhookUrl { get; set; }

    public Dictionary<string, string>? CallbackParams { get; set; }
}
