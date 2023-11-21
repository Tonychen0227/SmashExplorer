public static class IntegerExtensions
{
    public static string DisplayWithSuffix(this int num)
    {
        string number = num.ToString();
        if (number.EndsWith("11")) return number + "th";
        if (number.EndsWith("12")) return number + "th";
        if (number.EndsWith("13")) return number + "th";
        if (number.EndsWith("1")) return number + "st";
        if (number.EndsWith("2")) return number + "nd";
        if (number.EndsWith("3")) return number + "rd";
        return number + "th";
    }
}