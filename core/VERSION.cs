using System.Globalization;
using System.Reflection;

namespace XYVR.Core;

public class VERSION
{
    // ReSharper disable once InconsistentNaming
    public static string version { get; private set; }
    public static string miniVersion { get; private set; }

    static VERSION()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        if (File.Exists("ExecutingFromSource"))
        {
            version = "ExecutingFromSource";
            miniVersion = "v0.0.0";
        }
        else if (v == null) // Not sure how this can be null
        {
            version = "Unknown-NullVersion";
            miniVersion = "v0.0.0";
        }
        else
        {
            version = string.Format(CultureInfo.InvariantCulture, "v{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision);
            miniVersion = version;
        }
    }
}