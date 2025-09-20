namespace XYVR.Core;

public static class XYVRLogging
{
    public static void WriteLine(string str)
    {
        Console.WriteLine(str);
    }
    
    public static void WriteLine(Exception e)
    {
        Console.Error.WriteLine("Exception occurred: {0}\nStack Trace: {1}", e.Message, e.StackTrace);
    }

    public static void ErrorWriteLine(string s)
    {
        Console.Error.WriteLine(s);
    }
}