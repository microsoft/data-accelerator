// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DataX.Config.Utility
{
    public static class ConfigHelper
    {
        /// <summary>
        /// Parses the account name from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>account name</returns>
        public static string ParseBlobAccountName(string connectionString)
        {
            string matched;
            try
            {
                matched = Regex.Match(connectionString, @"(?<=AccountName=)(.*)(?=;AccountKey)").Value;
            }
            catch (Exception)
            {
                return "The connectionString does not have AccountName";
            }

            return matched;
        }

        /// <summary>
        /// Parses the account key from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>account key</returns>
        public static string ParseBlobAccountKey(string connectionString)
        {
            string matched;
            try
            {
                matched = Regex.Match(connectionString, @"(?<=AccountKey=)(.*)(?=;EndpointSuffix)").Value;
            }
            catch (Exception)
            {
                return "The connectionString does not have AccountKey";
            }

            return matched;
        }

        /// <summary>
        /// Get a partition increment value using the given blob path
        /// </summary>
        /// <returns></returns>
        public static long GetPartitionIncrement(string path)
        {
            Regex regex = new Regex(@"\{([yMdHhmsS\-\/.,: ]+)\}*", RegexOptions.IgnoreCase);
            Match mc = regex.Match(path);

            if (mc != null && mc.Success && mc.Groups.Count > 1)
            {
                var value = mc.Groups[1].Value.Trim();

                value = value.Replace(@"[\/:\s-]", "", StringComparison.InvariantCultureIgnoreCase).Replace(@"(.)(?=.*\1)", "", StringComparison.InvariantCultureIgnoreCase);

                if (value.Contains("h", StringComparison.InvariantCultureIgnoreCase))
                {
                    return 1 * 60;
                }
                else if (value.Contains("d", StringComparison.InvariantCultureIgnoreCase))
                {
                    return 1 * 60 * 24;
                }
                else if (value.Contains("M", StringComparison.InvariantCulture))
                {
                    return 1 * 60 * 24 * 30;
                }
                else if (value.Contains("y", StringComparison.InvariantCultureIgnoreCase))
                {
                    return 1 * 60 * 24 * 30 * 12;
                }
            }

            return 1;
        }

        /// <summary>
        /// Normalize the datetime base on interval and delay
        /// </summary>
        /// <returns></returns>
        public static DateTime NormalizeTimeBasedOnInterval(DateTime dateTime, string intervalType, TimeSpan delay)
        {
            dateTime = dateTime.Add(-delay);
            int second = dateTime.Second;
            int minute = dateTime.Minute;
            int hour = dateTime.Hour;
            int day = dateTime.Day;
            int month = dateTime.Month;
            int year = dateTime.Year;

            switch (intervalType)
            {
                case "min":
                    {
                        second = 0;
                        break;
                    }
                case "hour":
                    {
                        second = 0;
                        minute = 0;
                        break;
                    }
                default:
                    {
                        second = 0;
                        minute = 0;
                        hour = 0;
                        break;
                    }
            }

            return new DateTime(year, month, day, hour, minute, second);
        }

        /// <summary>
        /// Generate a timespan for Interval
        /// </summary>
        /// <returns></returns>
        public static TimeSpan TranslateInterval(string value, string unit)
        {
            var translatedValue = TranslateIntervalHelper(unit);
            int multiplier = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            translatedValue = translatedValue * multiplier;

            var timeSpan = new TimeSpan(0, 0, translatedValue, 0, 0);
            return timeSpan;
        }

        /// <summary>
        /// Generate a timespan for Window
        /// </summary>
        /// <returns></returns>
        public static TimeSpan TranslateWindow(string value, string unit)
        {
            var translatedValue = TranslateIntervalHelper(unit);
            int multiplier = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            translatedValue = translatedValue * multiplier;

            var timeSpan = new TimeSpan(0, 0, translatedValue - 1, 59, 59);
            return timeSpan;
        }

        /// <summary>
        /// Generate a timespan for Delay
        /// </summary>
        /// <returns></returns>
        public static TimeSpan TranslateDelay(string value, string unit)
        {
            var translatedValue = TranslateIntervalHelper(unit);
            int multiplier = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            translatedValue = translatedValue * multiplier;

            var timeSpan = new TimeSpan(0, 0, translatedValue, 0, 0);
            return timeSpan;
        }

        public static bool ShouldScheduleJob(bool disabled, bool isOneTime, DateTime? startTime, DateTime? endTime)
        {
            if (disabled || startTime == null || (isOneTime && endTime == null))
            {
                return false;
            }

            return true;
        }

        public static bool IsValidRecurringJob(DateTime currentTime, DateTime startTime, DateTime? endTime)
        {
            if (currentTime < startTime || (endTime != null && endTime < currentTime))
            {
                return false;
            }

            return true;
        }

        public static string GetBlobPartitionFormat(string inputMode, string blobPartitionFormat)
        {
            var timeFormat = inputMode != Constants.InputMode_Batching ? $"%1$tY/%1$tm/%1$td/%1$tH/${{quarterBucket}}/${{minuteBucket}}" : $"{TransformPartitionFormat(blobPartitionFormat)}";
            return timeFormat;
        }

        private static string TransformPartitionFormat(string partitionFormat)
        {
            var parts = partitionFormat.Split(new char[] { ',', '/', ':', '-', ' ' });

            string value = "";
            foreach (var part in parts)
            {
                value = "";
                switch (part.Substring(0, 1))
                {
                    case "s":
                        value = "%1$tS";
                        break;
                    case "m":
                        value = "%1$tM";
                        break;
                    case "h":
                    case "H":
                        value = "%1$tH";
                        break;
                    case "d":
                        value = "%1$td";
                        break;
                    case "M":
                        value = "%1$tm";
                        break;
                    case "y":
                        value = "%1$ty";
                        break;
                    default:
                        value = "";
                        break;
                }

                partitionFormat = partitionFormat.Replace(part, value);

            }

            return partitionFormat;
        }

        public static string GetValueFromJdbcConnection(string connectionString, string key)
        {
            try
            {
                Match match = Regex.Match(connectionString, $"{key}=([^;]*);", RegexOptions.IgnoreCase);
                string value = match.Groups[1].Value;
                return value;
            }
            catch (Exception)
            {
                throw new Exception($"{key} not found in jdbc connection string");
            }
        }
        public static string GetUrlFromJdbcConnection(string connectionString)
        {
            try
            {
                Match match = Regex.Match(connectionString, @"jdbc:sqlserver://(.*):", RegexOptions.IgnoreCase);
                string value = match.Groups[1].Value;
                return value;
            }
            catch (Exception)
            {
                throw new Exception("url pattern not found in jdbc connecton string");
            }
        }

        /// <summary>
        /// Translate Interval to a timespan value
        /// </summary>
        /// <returns></returns>
        private static int TranslateIntervalHelper(string unit)
        {
            switch (unit)
            {
                case "min":
                    return 1;
                case "hour":
                    return 60;
                default:
                    return 60 * 24;
            }
        }
    }
}
