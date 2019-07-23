using System;
using System.Collections.Generic;

namespace Util.Helpers {
    /// <summary>
    /// 时间操作
    /// </summary>
    public static class Time {
        /// <summary>
        /// 日期
        /// </summary>
        private static DateTime? _dateTime;

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="dateTime">时间</param>
        public static void SetTime( DateTime? dateTime ) {
            _dateTime = dateTime;
        }

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="dateTime">时间</param>
        public static void SetTime( string dateTime ) {
            _dateTime = Util.Helpers.Convert.ToDateOrNull( dateTime );
        }

        /// <summary>
        /// 重置时间
        /// </summary>
        public static void Reset() {
            _dateTime = null;
        }

        /// <summary>
        /// 获取当前日期时间
        /// </summary>
        public static DateTime GetDateTime() {
            if( _dateTime == null )
                return DateTime.Now;
            return _dateTime.Value;
        }

        /// <summary>
        /// 获取当前日期,不带时间
        /// </summary>
        public static DateTime GetDate() {
            return GetDateTime().Date;
        }

        /// <summary>
        /// 获取Unix时间戳
        /// </summary>
        public static long GetUnixTimestamp() {
            return GetUnixTimestamp( DateTime.Now );
        }

        /// <summary>
        /// 获取Unix时间戳
        /// </summary>
        /// <param name="time">时间</param>
        public static long GetUnixTimestamp( DateTime time ) {
            var start = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long ticks = (time - start).Ticks;
            return Util.Helpers.Convert.ToLong(ticks / TimeSpan.TicksPerSecond);
        }

        /// <summary>
        /// 从Unix时间戳获取东八区时间
        /// </summary>
        /// <param name="timestamp">Unix时间戳</param>
        public static DateTime GetTimeFromUnixTimestamp( long timestamp ) {
            var start = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            TimeSpan span = new TimeSpan( timestamp*TimeSpan.TicksPerSecond);
            return start.Add( span );
        }
        public static void GetTimeByWeek(int year, int weekDataFlag, out DateTime startTime, out DateTime endTime)
        {
            List<int> result = new List<int>();

            DateTime weekStart, weekEnd = DateTime.Now;

            DateTime startDay = new DateTime(year, 1, 1);

            DayOfWeek dayOfWeek = startDay.DayOfWeek;


            if (weekDataFlag == 1)
            {

                if (dayOfWeek == DayOfWeek.Sunday)
                {
                    weekStart = startDay;
                    weekEnd = startDay.AddHours(23).AddMinutes(59).AddSeconds(59);
                }
                else
                {

                    weekStart = startDay;
                    weekEnd = startDay.AddDays(7 - (int)dayOfWeek).AddHours(23).AddMinutes(59).AddSeconds(59);

                }
            }
            else
            {
                if (dayOfWeek == DayOfWeek.Sunday)
                {
                    weekStart = startDay.AddDays((weekDataFlag - 2) * 7 + 1);
                    weekEnd = weekStart.AddDays((6)).AddHours(23).AddMinutes(59).AddSeconds(59);

                }
                else
                {
                    weekStart = startDay.AddDays((7 - (int)dayOfWeek) + (weekDataFlag - 2) * 7);
                    weekEnd = weekStart.AddDays(7 - (int)dayOfWeek);
                }

            }
            startTime = weekStart;
            endTime = weekEnd;
        }
        public static void GetTimeByMonth(int year, int month, out DateTime startTime, out DateTime endTime)
        {
            DateTime dt = new DateTime();
            startTime = dt.AddYears(year - 1).AddMonths(month - 1);
            endTime = startTime.AddMonths(1);
            
        }
        public static void GetTimeBySeason(int year, int season, out DateTime startTime, out DateTime endTime)
        {
            DateTime dt = new DateTime();
            
            if (season > 0 && season < 5)
            {
                startTime = dt.AddYears(year - 1).AddMonths(3 * (season - 1));
                endTime = startTime.AddMonths(3);
            }
            else
            {
                startTime = dt.AddYears(year - 1);
                endTime = startTime.AddMonths(3);
            }
        
        }
        public static void GetTimeByYear(int year, out DateTime startTime, out DateTime endTime)
        {
            DateTime dt = new DateTime();

            startTime = dt.AddYears(year - 1);
            endTime = startTime.AddYears(1);
            
        }
        public static int GetWeekNum(DateTime dateTime)
        {
            System.Globalization.CultureInfo curCI = new System.Globalization.CultureInfo("zh-CN");
            return curCI.Calendar.GetWeekOfYear(dateTime, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }
        public static int GetSeasonNum(DateTime date)
        {
            return (date.Month + 2) / 3;
        }
    }
}