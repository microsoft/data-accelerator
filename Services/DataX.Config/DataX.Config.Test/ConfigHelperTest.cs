// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using DataX.Config.Utility;
using System.Globalization;
using DataX.Config.ConfigDataModel;

namespace DataX.Config.Test
{
    [TestClass]
    public class ConfigHelperTest
    {
        [TestMethod]
        public void GetPartitionIncrementTest()
        {
            var actual = ConfigHelper.GetPartitionIncrement("wasbs://container@sa.blob.core.windows.net/path1/{yyyy/MM/dd/hh}");
            Assert.AreEqual(60, actual);

            actual = ConfigHelper.GetPartitionIncrement("wasbs://container@sa.blob.core.windows.net/path1/{yyyy-MM-dd-hh}");
            Assert.AreEqual(60, actual);

            actual = ConfigHelper.GetPartitionIncrement("wasbs://container@sa.blob.core.windows.net/path1/{yyyy/MM/dd/hh}/path2");
            Assert.AreEqual(60, actual);

            actual = ConfigHelper.GetPartitionIncrement("wasbs://container@sa.blob.core.windows.net/path1/{yyyy-MM-dd-hh}/path2");
            Assert.AreEqual(60, actual);

            actual = ConfigHelper.GetPartitionIncrement("wasbs://container@sa.blob.core.windows.net/path1/{yyyy/MM/dd}");
            Assert.AreEqual(1440, actual);

            actual = ConfigHelper.GetPartitionIncrement("wasbs://container@sa.blob.core.windows.net/path1/{yyyy/MM}");
            Assert.AreEqual(43200, actual);

            actual = ConfigHelper.GetPartitionIncrement("wasbs://container@sa.blob.core.windows.net/path1/{yyyy}");
            Assert.AreEqual(518400, actual);
        }

        [TestMethod]
        public void NormalizeTimeBasedOnIntervalTest()
        {
            var current = DateTime.Parse("9/10/2019 13:05:30", CultureInfo.InvariantCulture);

            var actual = ConfigHelper.NormalizeTimeBasedOnInterval(current, "min", new TimeSpan(00, 00, 00));
            Assert.AreEqual("09/10/2019 13:05:00", actual.ToString(CultureInfo.InvariantCulture));

            actual = ConfigHelper.NormalizeTimeBasedOnInterval(current, "min", new TimeSpan(00, 05, 00));
            Assert.AreEqual("09/10/2019 13:00:00", actual.ToString(CultureInfo.InvariantCulture));

            actual = ConfigHelper.NormalizeTimeBasedOnInterval(current, "hour", new TimeSpan(00, 00, 00));
            Assert.AreEqual("09/10/2019 13:00:00", actual.ToString(CultureInfo.InvariantCulture));

            actual = ConfigHelper.NormalizeTimeBasedOnInterval(current, "hour", new TimeSpan(00, 05, 00));
            Assert.AreEqual("09/10/2019 13:00:00", actual.ToString(CultureInfo.InvariantCulture));

            actual = ConfigHelper.NormalizeTimeBasedOnInterval(current, "default", new TimeSpan(00, 00, 00));
            Assert.AreEqual("09/10/2019 00:00:00", actual.ToString(CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public void TranslateIntervalTest()
        {
            var actual = ConfigHelper.TranslateInterval("10", "min");
            Assert.AreEqual("00:10:00", actual.ToString());

            actual = ConfigHelper.TranslateInterval("10", "hour");
            Assert.AreEqual("10:00:00", actual.ToString());

            actual = ConfigHelper.TranslateInterval("10", "default");
            Assert.AreEqual("10.00:00:00", actual.ToString());
        }

        [TestMethod]
        public void TranslateWindowTest()
        {
            var actual = ConfigHelper.TranslateWindow("10", "min");
            Assert.AreEqual("00:09:59.0590000", actual.ToString());

            actual = ConfigHelper.TranslateWindow("10", "hour");
            Assert.AreEqual("09:59:59.0590000", actual.ToString());

            actual = ConfigHelper.TranslateWindow("10", "default");
            Assert.AreEqual("9.23:59:59.0590000", actual.ToString());
        }

        [TestMethod]
        public void TranslateDelayTest()
        {
            var actual = ConfigHelper.TranslateDelay("10", "min");
            Assert.AreEqual("00:10:00", actual.ToString());

            actual = ConfigHelper.TranslateDelay("10", "hour");
            Assert.AreEqual("10:00:00", actual.ToString());

            actual = ConfigHelper.TranslateDelay("10", "default");
            Assert.AreEqual("10.00:00:00", actual.ToString());
        }

        [TestMethod]
        public void ShouldScheduleJobTest()
        {
            var actual = ConfigHelper.ShouldScheduleJob(true, false, DateTime.Now, DateTime.Now.AddDays(1));
            Assert.IsFalse(actual);

            actual = ConfigHelper.ShouldScheduleJob(false, false, null, DateTime.Now.AddDays(1));
            Assert.IsFalse(actual);

            actual = ConfigHelper.ShouldScheduleJob(false, true, DateTime.Now, null);
            Assert.IsFalse(actual);

            actual = ConfigHelper.ShouldScheduleJob(false, false, DateTime.Now, null);
            Assert.IsTrue(actual);

            actual = ConfigHelper.ShouldScheduleJob(false, false, DateTime.Now, DateTime.Now.AddDays(1));
            Assert.IsTrue(actual);

            actual = ConfigHelper.ShouldScheduleJob(false, true, DateTime.Now, DateTime.Now.AddDays(1));
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void IsValidRecurringJobTest()
        {
            var actual = ConfigHelper.IsValidRecurringJob(DateTime.Now, DateTime.Now.AddDays(1), null);
            Assert.IsFalse(actual);

            actual = ConfigHelper.IsValidRecurringJob(DateTime.Now, DateTime.Now.AddDays(-2), DateTime.Now.AddDays(-1));
            Assert.IsFalse(actual);

            actual = ConfigHelper.IsValidRecurringJob(DateTime.Now, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void TransformPartitionFormatTest()
        {
            var actual = ConfigHelper.GetBlobPartitionFormat(Constants.InputMode_Batching, "yyyy-MM-dd/HH");
            Assert.AreEqual("%1$ty-%1$tm-%1$td/%1$tH", actual);

            actual = ConfigHelper.GetBlobPartitionFormat(Constants.InputMode_Batching, "yyyy-MM-dd-HH");
            Assert.AreEqual("%1$ty-%1$tm-%1$td-%1$tH", actual);

            actual = ConfigHelper.GetBlobPartitionFormat(Constants.InputMode_Streaming, "yyyy-MM-dd-HH");
            Assert.AreEqual($"%1$tY/%1$tm/%1$td/%1$tH/${{quarterBucket}}/${{minuteBucket}}", actual);
        }

        [TestMethod]
        public void GetValueFromJdbcConnectionTest()
        {
            var connectionString = "jdbc:sqlserver://mysqlserver.database.windows.net:1433;database=mydb;user=user1@sqlserver;password=passwd1;encrypt=true;trustServerCertificate=false;hostNameInCertificate=*.database.windows.net;loginTimeout=30;";
            var actual = ConfigHelper.GetValueFromJdbcConnection(connectionString, "database");
            Assert.AreEqual("mydb", actual);

            actual = ConfigHelper.GetValueFromJdbcConnection(connectionString, "user");
            Assert.AreEqual("user1@sqlserver", actual);

            actual = ConfigHelper.GetValueFromJdbcConnection(connectionString, "password");
            Assert.AreEqual("passwd1", actual);
        }

        [TestMethod]
        public void GetUrlFromJdbcConnectionTest()
        {
            var connectionString = "jdbc:sqlserver://mysqlserver.database.windows.net:1433;database=mydb;user=user1@sqlserver;password=passwd1;encrypt=true;trustServerCertificate=false;hostNameInCertificate=*.database.windows.net;loginTimeout=30;";
            var actual = ConfigHelper.GetUrlFromJdbcConnection(connectionString);
            Assert.AreEqual("mysqlserver.database.windows.net", actual);
        }
    }
}
