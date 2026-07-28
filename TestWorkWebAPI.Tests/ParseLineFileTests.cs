using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;
using TestWorkWebAPI.Endpoints;

namespace TestWorkWebAPI.Tests.Unit
{
    public class ParseLineFileTests
    {
        [Fact]
        public void ParseLineFile_ValidLine_ReturnsCorrectValues()
        {
            string line = "2025-03-20T10-15-30.1234Z;120;45.67";
            
            var (date, execTime, value) = FileEndpoints.ParseLineFile(line);
            
            Assert.Equal(new DateTime(2025, 3, 20, 10, 15, 30, 123, DateTimeKind.Utc), date);
            Assert.Equal(120, execTime);
            Assert.Equal(45.67f, value);
        }

        [Fact]
        public void ParseLineFile_InvalidDateFormat_ThrowsException()
        {
            string line = "2025/03/20;120;45.67";
            Assert.Throws<ValidationException>(() => FileEndpoints.ParseLineFile(line));
        }

        [Fact]
        public void ParseLineFile_NegativeExecutionTime_ThrowsException()
        {
            string line = "2025-03-20T10-15-30.1234Z;-5;45.67";
            Assert.Throws<ValidationException>(() => FileEndpoints.ParseLineFile(line));
        }

        [Fact]
        public void ParseLineFile_NegativeValue_ThrowsException()
        {
            string line = "2025-03-20T10-15-30.1234Z;120;-10.5";
            Assert.Throws<ValidationException>(() => FileEndpoints.ParseLineFile(line));
        }

        [Fact]
        public void ParseLineFile_FutureDate_ThrowsException()
        {
            string line = "2030-01-01T00-00-00.0000Z;120;45.67";
            Assert.Throws<ValidationException>(() => FileEndpoints.ParseLineFile(line));
        }

        [Fact]
        public void ParseLineFile_TooFewFields_ThrowsException()
        {
            string line = "2025-03-20T10-15-30.1234Z;120";
            Assert.Throws<ValidationException>(() => FileEndpoints.ParseLineFile(line));
        }

        [Fact]
        public void ParseLineFile_DateBeforeMin_ThrowsException()
        {
            string line = "1999-12-31T23-59-59.9999Z;120;45.67";
            Assert.Throws<ValidationException>(() => FileEndpoints.ParseLineFile(line));
        }
    }

    public class CalcResultsTests
    {
        [Fact]
        public void CalcResults_ValidData_ReturnsCorrectAggregates()
        {
            var dates = new List<DateTime>
            {
                new DateTime(2025, 3, 20, 10, 15, 30, DateTimeKind.Utc),
                new DateTime(2025, 3, 20, 10, 35, 40, DateTimeKind.Utc)
            };
            var execTimes = new List<long> { 120, 200 };
            var values = new List<float> { 45.67f, 88.76f };
            
            var result = FileEndpoints.CalcResults("test.csv", dates, execTimes, values);
            
            Assert.Equal("test.csv", result.FileName);
            Assert.Equal(1210.0, result.DeltaSeconds, 1);
            Assert.Equal(160, result.AvgExecutionTime);
            Assert.Equal(67.215, result.AvgValue, 3);
            Assert.Equal(67.215, result.MedianValue, 3);
            Assert.Equal(88.76f, result.MaxValue);
            Assert.Equal(45.67f, result.MinValue);
            Assert.Equal(dates[0], result.MinDate);
        }

        [Fact]
        public void CalcResults_OddCount_MedianCalculatedCorrectly()
        {
            var dates = new List<DateTime> { DateTime.UtcNow, DateTime.UtcNow.AddSeconds(10) };
            var execTimes = new List<long> { 100, 200, 300 };
            var values = new List<float> { 10f, 20f, 30f };
            var result = FileEndpoints.CalcResults("test.csv", dates, execTimes, values);
            Assert.Equal(20f, result.MedianValue);
        }

        [Fact]
        public void CalcResults_EvenCount_MedianAverageOfTwoMiddle()
        {
            var dates = new List<DateTime> { DateTime.UtcNow, DateTime.UtcNow.AddSeconds(10) };
            var execTimes = new List<long> { 100, 200 };
            var values = new List<float> { 10f, 20f, 30f, 40f };
            var result = FileEndpoints.CalcResults("test.csv", dates, execTimes, values);
            Assert.Equal(25f, result.MedianValue);
        }

        [Fact]
        public void CalcResults_AllSameValues_MedianEqualsValue()
        {
            var dates = new List<DateTime> { DateTime.UtcNow, DateTime.UtcNow.AddSeconds(1) };
            var execTimes = new List<long> { 100, 100 };
            var values = new List<float> { 50f, 50f, 50f };
            var result = FileEndpoints.CalcResults("test.csv", dates, execTimes, values);
            Assert.Equal(50f, result.MedianValue);
            Assert.Equal(50f, result.AvgValue);
            Assert.Equal(50f, result.MaxValue);
            Assert.Equal(50f, result.MinValue);
        }

        [Fact]
        public void CalcResults_DeltaSeconds_CalculatedCorrectly()
        {
            var dates = new List<DateTime>
            {
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 1, 0, 1, 30, DateTimeKind.Utc) // 90 секунд
            };
            var result = FileEndpoints.CalcResults("test.csv", dates, new List<long> { 1 }, new List<float> { 1 });
            Assert.Equal(90, result.DeltaSeconds);
        }
    }
}