namespace CustomerManagerIngest.Application.Contracts;

public record InboundMessageDto(string InboundMessageSid,
    string From,
    string To,
    string Body,
    DateTimeOffset TimestampUtc,
    string TenantId,
    string Locale,
    bool RagNeeded
);