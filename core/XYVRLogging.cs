namespace XYVR.Core;

public static class XYVRLogging
{
    public static event ErrorLog? OnErrorLog;
    public delegate void ErrorLog(string message);

    public static TextWriter? LogFile;
    public static object _lock = new object();

    public static void SetupLogFile()
    {
        if (LogFile != null) return;
        try
        {
            var logPath = Path.Combine(Path.GetDirectoryName(typeof(XYVRLogging).Assembly.Location)!, "Latest.log");
            LogFile = new StreamWriter(logPath);
            
            OnErrorLog += (msg) => {
                lock (_lock)
                {
                    LogFile.WriteLine("ERROR: " + msg);
                    LogFile.Flush();
                }
            };
        }
        catch (Exception e)
        {
            ErrorWriteLine(typeof(XYVRLogging), e);
        }
    }
    public static void CleanupLogFile()
    {
        LogFile?.Flush();
        LogFile?.Close();
        LogFile = null;
    }

    public static void WriteLine(object caller, string str)
    {
        var msg = $"{Header(caller)} {str}";
        Console.WriteLine(msg);

        if(LogFile != null)
        {
            lock (_lock)
            {
                LogFile.WriteLine("INFO: " + msg);
				LogFile.Flush();
            }
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