namespace XYVR.Core;

public static class XYVRLogging
{
    public static event ErrorLog? OnErrorLog;
    public delegate void ErrorLog(string message);

    public const string LogFile = "Latest.log";
    public static object _lock = new object(); 
    static XYVRLogging()
    {
        lock (_lock)
        {
            File.WriteAllText(LogFile, "");
        }
        OnErrorLog += (msg) => {
            lock (_lock)
            {
                File.AppendAllLines(LogFile, ["ERROR: " + msg]);
            }  
        };
    }

    public static void WriteLine(object caller, string str)
    {
        var msg = $"{Header(caller)} {str}";
        Console.WriteLine(msg);

        lock (_lock)
        {
            File.AppendAllLines(LogFile, ["INFO: " + msg]);
        }
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