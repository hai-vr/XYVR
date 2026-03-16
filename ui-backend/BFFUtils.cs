using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.Core;

namespace XYVR.UI.Backend;

public static class BFFUtils
{
    public static JsonSerializerSettings NewSerializer()
    {
        return new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        };
    }

    public static TReturn LogErrors<TReturn>(object caller, Func<TReturn> inner)
    {
        try
        {
            return inner.Invoke();
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(caller, e);
            throw;
        }
    }
}