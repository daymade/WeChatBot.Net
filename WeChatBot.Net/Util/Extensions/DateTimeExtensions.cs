using System;

namespace WeChatBot.Net.Util.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Convert to unix time (seconds)
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static long ToUnixTime(this DateTime date)
        {
            return Convert.ToInt64(Math.Floor((date.ToUniversalTime() - UnixEpoch).TotalSeconds));
        }

        /// <summary>
        /// Convert from unix time (seconds)
        /// </summary>
        /// <param name="unixTime"></param>
        /// <returns></returns>
        public static DateTime FromUnixTime(this long unixTime)
        {
            return UnixEpoch.AddSeconds((double) unixTime);
        }
    }
}
