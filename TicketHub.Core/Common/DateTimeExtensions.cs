namespace TicketHub.Core.Common;

using System;
using System.Globalization;

public static class DateTimeExtensions
{
    public static string ToPersianDateTimeString(this DateTime dateTime, bool includeSeconds = false)
    {
        var pc = new PersianCalendar();
        var localDt = dateTime.Kind == DateTimeKind.Utc ? dateTime.ToLocalTime() : dateTime;
        
        int year = pc.GetYear(localDt);
        int month = pc.GetMonth(localDt);
        int day = pc.GetDayOfMonth(localDt);
        int hour = localDt.Hour;
        int minute = localDt.Minute;

        if (includeSeconds)
        {
            int second = localDt.Second;
            return $"{hour:D2}:{minute:D2}:{second:D2} - {year:D4}/{month:D2}/{day:D2}";
        }

        return $"{hour:D2}:{minute:D2} - {year:D4}/{month:D2}/{day:D2}";
    }

    public static string ToPersianDateString(this DateTime dateTime)
    {
        var pc = new PersianCalendar();
        var localDt = dateTime.Kind == DateTimeKind.Utc ? dateTime.ToLocalTime() : dateTime;
        
        int year = pc.GetYear(localDt);
        int month = pc.GetMonth(localDt);
        int day = pc.GetDayOfMonth(localDt);

        return $"{year:D4}/{month:D2}/{day:D2}";
    }
}
