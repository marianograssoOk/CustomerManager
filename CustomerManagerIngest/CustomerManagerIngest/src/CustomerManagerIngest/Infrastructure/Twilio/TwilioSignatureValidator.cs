using System.Security.Cryptography;
using System.Text;
using CustomerManagerIngest.Application.Ports;

namespace CustomerManagerIngest.Infrastructure.Twilio;

public class TwilioSignatureValidator(string authToken) : ITwilioSignatureValidator
{
    private readonly string _authToken = authToken ?? throw new ArgumentNullException(nameof(authToken));

    public bool IsValid(string twilioSignature, string fullUrl, IDictionary<string, string> formFields)
    {
        // 1) Construir base string: fullUrl + concat(key=value) ordenados por key (ASCII)
        var sb = new StringBuilder(fullUrl);
        foreach (var kv in formFields.OrderBy(k => k.Key, StringComparer.Ordinal))
            sb.Append(kv.Key).Append(kv.Value);

        var data = Encoding.UTF8.GetBytes(sb.ToString());
        var key = Encoding.UTF8.GetBytes(_authToken);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(data);
        var expected = Convert.ToBase64String(hash);

        // Twilio firma en base64; comparar de forma constante
        return SlowEquals(expected, twilioSignature);
    }

    private static bool SlowEquals(string a, string b)
    {
        // compara de forma tiempo-constante
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        int diff = ba.Length ^ bb.Length;
        for (int i = 0; i < ba.Length && i < bb.Length; i++)
            diff |= ba[i] ^ bb[i];
        return diff == 0;
    }
}