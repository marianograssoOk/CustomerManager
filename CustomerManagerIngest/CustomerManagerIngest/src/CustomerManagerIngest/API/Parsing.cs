using System.Web;

namespace CustomerManagerIngest.API;

public static class FormParser
{
    public static IDictionary<string,string> ToDictionary(string formEncodedBody)
    {
        var nvc = HttpUtility.ParseQueryString(formEncodedBody ?? string.Empty);
        return nvc.AllKeys!
            .Where(k => k != null)
            .ToDictionary(k => k!, k => nvc[k] ?? string.Empty);
    }
}