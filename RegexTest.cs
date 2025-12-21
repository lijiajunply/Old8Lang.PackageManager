using System;

public class RegexTest
{
    public static void Main()
    {
        var version = "2.28.0.dev0";
        var isPythonVersion = System.Text.RegularExpressions.Regex.IsMatch(
            version, 
            @"^\d+\.\d+(\.\d+)?(([ab]|rc|alpha|beta|pre|post|dev)\d*)?$"
        );
        Console.WriteLine($"Version: {version}");
        Console.WriteLine($"Is Valid: {isPythonVersion}");
    }
}