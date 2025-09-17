namespace XYVR.Core;

public static class XYVRLogging
{
    public static void WriteLine(string str)
    {
        Console.WriteLine(str);
    }
    
    public static void WriteLine(Exception e)
    {
        Console.WriteLine(e);
    }

    public static void ErrorWriteLine(string s)
    {
        Console.Error.WriteLine(s);
    }
}