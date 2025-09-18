namespace XYVR.Core;

public static class XYVRLogging
{
    public static void WriteLine(string str)
    {
        System.Diagnostics.Debug.WriteLine(str);
    }
    
    public static void WriteLine(Exception e)
    {
        System.Diagnostics.Trace.TraceError("Exception occurred: {0}\nStack Trace: {1}", e.Message, e.StackTrace);
    }

    public static void ErrorWriteLine(string s)
    {
        System.Diagnostics.Trace.TraceError(s);
    }
}