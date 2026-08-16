namespace TicketHub.Core.Common;

using System;
using System.Globalization;

public static class DateTimeExtensions
{
    public static DateTime ToTehranTime(this DateTime dateTime)
    {
        DateTime utcTime = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };

        try
        {
            TimeZoneInfo tehranZone = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tehranZone);
        }
        catch
        {
            try
            {
                TimeZoneInfo tehranZoneAlt = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tehranZoneAlt);
            }
            catch
            {
                return utcTime.AddHours(3.5);
            }
        }
    }

    public static DateTime ToTehranTime(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.UtcDateTime.ToTehranTime();
    }

    public static DateTime FromTehranTimeToUtc(this DateTime tehranDateTime)
    {
        try
        {
            TimeZoneInfo tehranZone = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(tehranDateTime, DateTimeKind.Unspecified), tehranZone);
        }
        catch
        {
            try
            {
                TimeZoneInfo tehranZoneAlt = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
                return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(tehranDateTime, DateTimeKind.Unspecified), tehranZoneAlt);
            }
            catch
            {
                return tehranDateTime.AddHours(-3.5);
            }
        }
    }

    public static string ToPersianDateTimeString(this DateTime dateTime, bool includeSeconds = false)
    {
        var pc = new PersianCalendar();
        var tehranDt = dateTime.ToTehranTime();
        
        int year = pc.GetYear(tehranDt);
        int month = pc.GetMonth(tehranDt);
        int day = pc.GetDayOfMonth(tehranDt);
        int hour = tehranDt.Hour;
        int minute = tehranDt.Minute;

        if (includeSeconds)
        {
            int second = tehranDt.Second;
            return $"{hour:D2}:{minute:D2}:{second:D2} - {year:D4}/{month:D2}/{day:D2}";
        }

        return $"{hour:D2}:{minute:D2} - {year:D4}/{month:D2}/{day:D2}";
    }

    public static string ToPersianDateString(this DateTime dateTime)
    {
        var pc = new PersianCalendar();
        var tehranDt = dateTime.ToTehranTime();
        
        int year = pc.GetYear(tehranDt);
        int month = pc.GetMonth(tehranDt);
        int day = pc.GetDayOfMonth(tehranDt);

        return $"{year:D4}/{month:D2}/{day:D2}";
    }

    public static string ToPersianNumbers(this object? input)
    {
        if (input == null) return string.Empty;
        var str = input.ToString() ?? string.Empty;
        return str.Replace('0', '۰')
                  .Replace('1', '۱')
                  .Replace('2', '۲')
                  .Replace('3', '۳')
                  .Replace('4', '۴')
                  .Replace('5', '۵')
                  .Replace('6', '۶')
                  .Replace('7', '۷')
                  .Replace('8', '۸')
                  .Replace('9', '۹');
    }
}

