using System;


namespace okx;


public class Helper
{
    public static void PrintAtCenter(string str, string padding="-")
    {
        var width = Console.WindowWidth;
        var len = str.Length;
        var left = (width - len) / 2;
        var right = width - len - left;
        Console.WriteLine($"{padding.PadLeft(left, padding[0])}{str}{padding.PadRight(right, padding[0])}");
    }
}