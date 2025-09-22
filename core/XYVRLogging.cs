namespace XYVR.Core;

public static class XYVRLogging
{
    public static event ErrorLog? OnErrorLog;
    public delegate void ErrorLog(string message); 
    
    public static void WriteLine(object caller, string str)
    {
        Console.WriteLine($"{Header(caller)} {str}");
    }

    public static void ErrorWriteLine(object caller, Exception e)
    {
        var line = $"{Header(caller)} Exception occurred: {e.Message}\nStack Trace: {e.StackTrace}";
        Console.Error.WriteLine(line);
        
        OnErrorLog?.Invoke(line);
    }

    public static void ErrorWriteLine(object caller, string str)
    {
        var line = $"{Header(caller)} {str}";
        Console.Error.WriteLine(line);
        
        OnErrorLog?.Invoke(line);
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