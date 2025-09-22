using Newtonsoft.Json;

namespace XYVR.Core;

public class XYVRSerializationUtils
{
    public static T? LogDeserializeOrNull<T>(object callerForLogging, string rawText)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(rawText)!;
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(callerForLogging, e);
            return default;
        }
    }
}