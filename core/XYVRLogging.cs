namespace XYVR.Core;

public static class XYVRLogging
{
    public static void WriteLine(object caller, string str)
    {
        Console.WriteLine($"{Header(caller)} {str}");
    }

    public static void WriteLine(object caller, Exception e)
    {
        Console.Error.WriteLine("{2} Exception occurred: {0}\nStack Trace: {1}", e.Message, e.StackTrace, Header(caller));
    }

    public static void ErrorWriteLine(object caller, string str)
    {
        Console.Error.WriteLine($"{Header(caller)} {str}");
    }

    private static string Header(object caller)
    {
        var callerType = caller is Type t ? t.Name : caller.GetType().Name;
        return $"{Timestamp()} - {callerType} -";
    }

    private static string Timestamp()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}