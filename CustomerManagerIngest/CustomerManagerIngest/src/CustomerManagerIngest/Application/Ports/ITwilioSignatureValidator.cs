namespace CustomerManagerIngest.Application.Ports;

public interface ITwilioSignatureValidator
{
    bool IsValid(string twilioSignatureHeader, string fullUrl, IDictionary<string, string> formFields);
}