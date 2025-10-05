using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using CustomerManagerIngest.Application.Contracts;
using CustomerManagerIngest.Application.Ports;
using CustomerManagerIngest.Infrastructure.Twilio;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CustomerManagerIngest.API;

public class Function
{
    private readonly ITwilioSignatureValidator _signatureValidator;

    public Function()
    {
        var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") 
                        ?? throw new InvalidOperationException("TWILIO_AUTH_TOKEN missing");
        _signatureValidator = new TwilioSignatureValidator(authToken);
    }

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest req, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"Ingest webhook received. Path: {req.Path}");

            var dict = FormParser.ToDictionary(req.Body);

            // 2) Validar headers básicos Twilio (lo implementamos en el paso siguiente)
            if (!req.Headers.TryGetValue("X-Twilio-Signature", out var twilioSig) || string.IsNullOrWhiteSpace(twilioSig))
                return Resp(HttpStatusCode.Unauthorized, "Missing Twilio signature");
        
            var fullUrl = BuildFullUrl(req);
            if (!_signatureValidator.IsValid(twilioSig, fullUrl, dict))
                return Resp(HttpStatusCode.Unauthorized, "Invalid signature");
        
            var form = new TwilioWebhookForm(
                MessageSid: dict["MessageSid"],
                From:       dict["From"],
                To:         dict["To"],
                Body:       dict["Body"]
            );
        
            var dto = new InboundMessageDto(
                InboundMessageSid: form.MessageSid,
                From: form.From,
                To: form.To,
                Body: form.Body,
                TimestampUtc: DateTimeOffset.UtcNow,
                TenantId: Environment.GetEnvironmentVariable("TENANT_ID") ?? "default",
                Locale: Environment.GetEnvironmentVariable("LOCALE") ?? "es-AR",
                RagNeeded: true
            );
        
            // 5) Persistencia mínima (UPsert inbound) — TODO en Paso 6
            // await _inboundRepo.UpsertAsync(dto); 

            // 6) Publicar en SQS — TODO en Paso 7
            // await _publisher.PublishAsync(dto);

            return Resp(HttpStatusCode.OK, "OK");
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex.ToString());
            return Resp(HttpStatusCode.InternalServerError, "Error");
        }
    }
    
    private static APIGatewayProxyResponse Resp(HttpStatusCode code, string body) =>
        new() { StatusCode = (int)code, Body = body };
    
    private static string BuildFullUrl(APIGatewayProxyRequest req)
    {
        // si tenés el dominio en env var, mejor
        var host = req.Headers.TryGetValue("Host", out var h) ? h : "example.execute-api.aws";
        var proto = req.Headers.TryGetValue("X-Forwarded-Proto", out var p) ? p : "https";
        var path = req.Path ?? "/";
        var qs = req.QueryStringParameters?.Count > 0
            ? "?" + string.Join("&", req.QueryStringParameters.Select(kv => $"{kv.Key}={kv.Value}"))
            : "";
        return $"{proto}://{host}{path}{qs}";
    }

}