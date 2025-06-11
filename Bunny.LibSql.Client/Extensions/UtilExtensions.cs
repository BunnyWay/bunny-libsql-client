public static class UtilExtensions
{
    public static long ToUnixDate(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }
    public static DateTime ToUnixDateTime(this long unixDate)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixDate).DateTime;
    }
}