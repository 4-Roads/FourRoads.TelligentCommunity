namespace FourRoads.TelligentCommunity.Sentrus.Helpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    public static class DateHelper
    {
        public static DateTime BaseUtcDateTime
        {
            get { return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); }
        }

        /// <summary>
        ///     Convert UTC time, as returned by Facebook, to localtime.
        /// </summary>
        /// <param name="secondsSinceEpoch">The number of seconds since Jan 1, 1970.</param>
        /// <returns>Local time.</returns>
        internal static DateTime ConvertDoubleToDate(double secondsSinceEpoch)
        {
            return TimeZone.CurrentTimeZone.ToLocalTime(BaseUtcDateTime.AddSeconds(secondsSinceEpoch));
        }

        //Event dates are stored by assuming the time which the user input was in Pacific
        // time (PST or PDT, depending on the date), converting that to UTC, and then
        // converting that to Unix epoch time. This algorithm reverses that process.
        internal static DateTime ConvertDoubleToEventDate(double secondsSinceEpoch)
        {
            DateTime utcDateTime = BaseUtcDateTime.AddSeconds(secondsSinceEpoch);
            int pacificZoneOffset = utcDateTime.IsDaylightSavingTime() ? -7 : -8;
            return utcDateTime.AddHours(pacificZoneOffset);
        }

        /// <summary>
        ///     Convert datetime to UTC time, as understood by Facebook.
        /// </summary>
        /// <param name="dateToConvert">The date that we need to pass to the api.</param>
        /// <returns>The number of seconds since Jan 1, 1970.</returns>
        internal static double? ConvertDateToDouble(DateTime? dateToConvert)
        {
            return dateToConvert != null ? new double?((dateToConvert.Value - BaseUtcDateTime).TotalSeconds) : null;
        }

        public static string ToUnixTime(DateTime dateTime)
        {
            if (dateTime < BaseUtcDateTime)
            {
                throw new ArgumentException("Date time cannot be less than 01/01/1970.");
            }
            Contract.EndContractBlock();

            DateTime epoch = BaseUtcDateTime;
            TimeSpan range = dateTime - epoch;
            return Math.Floor(range.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "unixTime")]
        public static DateTime FromUnixTime(string unixTime)
        {
            if (string.IsNullOrEmpty(unixTime))
            {
                throw new ArgumentNullException("unixTime");
            }
            Contract.EndContractBlock();

            long seconds;

            if (!long.TryParse(unixTime, out seconds) || seconds < 0)
            {
                throw new FormatException("The unix time provided was not in the correct format.");
            }

            DateTime epoch = BaseUtcDateTime;
            long max = Int64.Parse(ToUnixTime(DateTime.MaxValue));

            return seconds < max ? epoch.AddSeconds(seconds) : DateTime.MaxValue;
        }
    }
}