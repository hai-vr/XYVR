using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace XYVR.UI.WebviewUI;

public static class BFFUtils
{
    public static JsonSerializerSettings NewSerializer()
    {
        return new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        };
    }
}