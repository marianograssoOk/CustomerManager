namespace CustomerManagerIngest.Application.Contracts;

public record TwilioWebhookForm(
    string MessageSid,
    string From,
    string To,
    string Body
    // agrega si necesitás más campos: AccountSid, WaId, etc.
);