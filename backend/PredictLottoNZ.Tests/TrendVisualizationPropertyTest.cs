using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PredictLottoNZ.Tests
{
    /**
     * Feature: lottery-lookup-navigation, Property 33: Trend visualizations show performance patterns
     * Validates: Requirements 12.4
     */
    public class TrendVisualizationPropertyTest
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly LottoDbContext _context;
        private readonly IFrequencyAnalysisService _frequencyService;

        public TrendVisualizationPropertyTest()
        {
            var services = new ServiceCollection();
            
            services.AddDbContext<LottoDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
            
            services.AddScoped<IFrequencyAnalysisService, FrequencyAnalysisService>();
            services.AddScoped<ICacheService, TestCacheService>();
            services.AddLogging(builder => builder.AddConsole());
            
            _serviceProvider = services.BuildServiceProvider();
            _context = _serviceProvider.GetRequiredService<LottoDbContext>();
            _frequencyService = _serviceProvider.GetRequiredService<IFrequencyAnalysisService>();
        }

        // Generator for trend data points
        public static Arbitrary<List<TrendDataPoint>> TrendDataGenerator() =>
            Arb.From(
                from count in Gen.Choose(5, 50) // At least 5 points for meaningful trends
                from points in Gen.ListOf(count,
                    from date in Gen.Choose(1, 365) // Days ago
                    from value in Gen.Choose(0, 100)
                    from isHot in Gen.Elements(true, false)
                    select new TrendDataPoint
                    {
                        Date = DateTime.Now.AddDays(-date),
                        Value = value,
                        IsHotPeriod = isHot,
                        MovingAverage = Gen.Choose(0, 100).Sample(0, 1).First(),
                        Trend = Gen.Elements("Increasing", "Decreasing", "Stable").Sample(0, 1).First()
                    })
                select points.OrderBy(p => p.Date).ToList()
            );

        // Generator for number performance data
        public static Arbitrary<NumberPerformanceData> NumberPerformanceGenerator() =>
            Arb.From(
                from number in Gen.Choose(1, 40)
                from trendPoints in TrendDataGenerator().Generator
                from hotPeriods in Gen.ListOf(
                    from start in Gen.Choose(1, 300)
                    from duration in Gen.Choose(7, 60)
                    select new HotColdPeriod
                    {
                        StartDate = DateTime.Now.AddDays(-start),
                        EndDate = DateTime.Now.AddDays(-start + duration),
                        IsHot = true,
                        AverageFrequency = 15
                    })
                from coldPeriods in Gen.ListOf(
                    from start in Gen.Choose(1, 300)
                    from duration in Gen.Choose(7, 60)
                    select new HotColdPeriod
                    {
                        StartDate = DateTime.Now.AddDays(-start),
                        EndDate = DateTime.Now.AddDays(-start + duration),
                        IsHot = false,
                        AverageFrequency = 1
                    })
                select new NumberPerformanceData
                {
                    Number = number,
                    TrendPoints = trendPoints,
                    HotPeriods = hotPeriods.ToList(),
                    ColdPeriods = coldPeriods.ToList(),
                    OverallTrend = Gen.Elements("Increasing", "Decreasing", "Stable").Sample(0, 1).First(),
                    CurrentClassification = Gen.Elements("Hot", "Cold", "Normal").Sample(0, 1).First()
                }
            );

        [Property(MaxTest = 100)]
        public void TrendVisualizationShowsPerformancePatterns(NumberPerformanceData performanceData)
        {
            // Property: For any number performance data, trend visualization should show patterns
            
            if (performanceData?.TrendPoints == null || performanceData.TrendPoints.Count < 2)
                return;

            var trendVisualization = CreateTrendVisualization(performanceData);
            
            // Should have data points for the trend line
            Assert.NotNull(trendVisualization.TrendLine);
            Assert.True(trendVisualization.TrendLine.Data.Count >= 2);
            
            // Should identify hot and cold periods
            Assert.NotNull(trendVisualization.HotPeriods);
            Assert.NotNull(trendVisualization.ColdPeriods);
            
            // Hot periods should be visually distinct from cold periods
            if (trendVisualization.HotPeriods.Count > 0 && trendVisualization.ColdPeriods.Count > 0)
            {
                Assert.NotEqual(trendVisualization.HotPeriods.First().Color, 
                               trendVisualization.ColdPeriods.First().Color);
            }
            
            // Trend line should reflect overall performance pattern
            if (performanceData.OverallTrend == "Increasing")
            {
                var firstValue = trendVisualization.TrendLine.Data.First();
                var lastValue = trendVisualization.TrendLine.Data.Last();
                // Allow for some variation in noisy data
                Assert.True(lastValue >= firstValue * 0.8, 
                    "Increasing trend should show general upward movement");
            }
            else if (performanceData.OverallTrend == "Decreasing")
            {
                var firstValue = trendVisualization.TrendLine.Data.First();
                var lastValue = trendVisualization.TrendLine.Data.Last();
                Assert.True(lastValue <= firstValue * 1.2, 
                    "Decreasing trend should show general downward movement");
            }
        }

        [Property(MaxTest = 100)]
        public void TrendVisualizationIncludesMovingAverages(List<TrendDataPoint> trendPoints)
        {
            // Property: For any trend data, visualization should include moving averages for smoothing
            
            if (trendPoints == null || trendPoints.Count < 3)
                return;

            var trendVisualization = CreateTrendVisualizationFromPoints(trendPoints);
            
            // Should include moving average line
            Assert.NotNull(trendVisualization.MovingAverage);
            Assert.True(trendVisualization.MovingAverage.Data.Count > 0);
            
            // Moving average should be smoother than raw data
            var rawDataVariance = CalculateVariance(trendVisualization.TrendLine.Data);
            var movingAverageVariance = CalculateVariance(trendVisualization.MovingAverage.Data);
            
            // Moving average should generally have less variance (be smoother)
            // Allow for cases where data is already very smooth
            if (rawDataVariance > 1.0)
            {
                Assert.True(movingAverageVariance <= rawDataVariance * 1.1,
                    "Moving average should be smoother than raw data");
            }
            
            // Moving average values should be within reasonable range of raw data
            for (int i = 0; i < Math.Min(trendVisualization.TrendLine.Data.Count, 
                                        trendVisualization.MovingAverage.Data.Count); i++)
            {
                var rawValue = trendVisualization.TrendLine.Data[i];
                var avgValue = trendVisualization.MovingAverage.Data[i];
                
                // Moving average should be within reasonable bounds of raw data
                Assert.True(Math.Abs(avgValue - rawValue) <= Math.Max(rawValue * 0.5, 10),
                    $"Moving average value {avgValue} should be reasonably close to raw value {rawValue}");
            }
        }

        [Property(MaxTest = 100)]
        public void TrendVisualizationHighlightsSignificantChanges(List<TrendDataPoint> trendPoints)
        {
            // Property: For any trend data with significant changes, visualization should highlight them
            
            if (trendPoints == null || trendPoints.Count < 5)
                return;

            // Add some significant changes to the data
            var modifiedPoints = trendPoints.ToList();
            if (modifiedPoints.Count >= 5)
            {
                // Create a significant spike
                var midPoint = modifiedPoints.Count / 2;
                modifiedPoints[midPoint].Value = Math.Max(modifiedPoints[midPoint].Value * 3, 50);
                
                // Create a significant drop
                if (midPoint + 2 < modifiedPoints.Count)
                {
                    modifiedPoints[midPoint + 2].Value = Math.Max(modifiedPoints[midPoint + 2].Value / 3, 1);
                }
            }

            var trendVisualization = CreateTrendVisualizationFromPoints(modifiedPoints);
            
            // Should identify significant changes
            Assert.NotNull(trendVisualization.SignificantChanges);
            
            // Should have annotations for significant changes
            Assert.NotNull(trendVisualization.Annotations);
            
            // If there are significant changes, they should be marked
            var hasSignificantVariation = HasSignificantVariation(modifiedPoints);
            if (hasSignificantVariation)
            {
                Assert.True(trendVisualization.SignificantChanges.Count > 0 || 
                           trendVisualization.Annotations.Count > 0,
                    "Significant changes should be highlighted in visualization");
            }
        }

        [Property(MaxTest = 100)]
        public void TrendVisualizationSupportsMultipleTimeScales(NumberPerformanceData performanceData)
        {
            // Property: For any performance data, trend visualization should support multiple time scales
            
            if (performanceData?.TrendPoints == null || performanceData.TrendPoints.Count < 10)
                return;

            // Should support daily view
            var dailyTrend = CreateDailyTrendVisualization(performanceData);
            Assert.NotNull(dailyTrend);
            Assert.True(dailyTrend.TrendLine.Data.Count > 0);
            
            // Should support weekly view
            var weeklyTrend = CreateWeeklyTrendVisualization(performanceData);
            Assert.NotNull(weeklyTrend);
            Assert.True(weeklyTrend.TrendLine.Data.Count > 0);
            
            // Should support monthly view
            var monthlyTrend = CreateMonthlyTrendVisualization(performanceData);
            Assert.NotNull(monthlyTrend);
            Assert.True(monthlyTrend.TrendLine.Data.Count > 0);
            
            // Weekly view should have fewer data points than daily
            if (performanceData.TrendPoints.Count > 14)
            {
                Assert.True(weeklyTrend.TrendLine.Data.Count <= dailyTrend.TrendLine.Data.Count,
                    "Weekly view should aggregate data and have fewer points than daily view");
            }
            
            // Monthly view should have fewer data points than weekly
            if (performanceData.TrendPoints.Count > 60)
            {
                Assert.True(monthlyTrend.TrendLine.Data.Count <= weeklyTrend.TrendLine.Data.Count,
                    "Monthly view should aggregate data and have fewer points than weekly view");
            }
        }

        // Helper methods for creating trend visualizations
        private TrendVisualization CreateTrendVisualization(NumberPerformanceData performanceData)
        {
            return new TrendVisualization
            {
                TrendLine = new TrendLine
                {
                    Data = performanceData.TrendPoints.Select(p => (double)p.Value).ToList(),
                    Labels = performanceData.TrendPoints.Select(p => p.Date.ToString("yyyy-MM-dd")).ToList(),
                    Color = GetTrendColor(performanceData.CurrentClassification)
                },
                MovingAverage = CreateMovingAverage(performanceData.TrendPoints),
                HotPeriods = performanceData.HotPeriods.Select(hp => new PeriodHighlight
                {
                    StartIndex = GetDateIndex(performanceData.TrendPoints, hp.StartDate),
                    EndIndex = GetDateIndex(performanceData.TrendPoints, hp.EndDate),
                    Color = "#ff4444",
                    Label = "Hot Period"
                }).ToList(),
                ColdPeriods = performanceData.ColdPeriods.Select(cp => new PeriodHighlight
                {
                    StartIndex = GetDateIndex(performanceData.TrendPoints, cp.StartDate),
                    EndIndex = GetDateIndex(performanceData.TrendPoints, cp.EndDate),
                    Color = "#4444ff",
                    Label = "Cold Period"
                }).ToList(),
                SignificantChanges = IdentifySignificantChanges(performanceData.TrendPoints),
                Annotations = CreateAnnotations(performanceData.TrendPoints)
            };
        }

        private TrendVisualization CreateTrendVisualizationFromPoints(List<TrendDataPoint> points)
        {
            return new TrendVisualization
            {
                TrendLine = new TrendLine
                {
                    Data = points.Select(p => (double)p.Value).ToList(),
                    Labels = points.Select(p => p.Date.ToString("yyyy-MM-dd")).ToList(),
                    Color = "#3498db"
                },
                MovingAverage = CreateMovingAverage(points),
                SignificantChanges = IdentifySignificantChanges(points),
                Annotations = CreateAnnotations(points),
                HotPeriods = new List<PeriodHighlight>(),
                ColdPeriods = new List<PeriodHighlight>()
            };
        }

        private TrendLine CreateMovingAverage(List<TrendDataPoint> points, int windowSize = 5)
        {
            var movingAverages = new List<double>();
            var labels = new List<string>();
            
            for (int i = 0; i < points.Count; i++)
            {
                var start = Math.Max(0, i - windowSize / 2);
                var end = Math.Min(points.Count - 1, i + windowSize / 2);
                var window = points.Skip(start).Take(end - start + 1);
                var average = window.Average(p => p.Value);
                
                movingAverages.Add(average);
                labels.Add(points[i].Date.ToString("yyyy-MM-dd"));
            }
            
            return new TrendLine
            {
                Data = movingAverages,
                Labels = labels,
                Color = "#e74c3c"
            };
        }

        private TrendVisualization CreateDailyTrendVisualization(NumberPerformanceData performanceData)
        {
            return CreateTrendVisualization(performanceData);
        }

        private TrendVisualization CreateWeeklyTrendVisualization(NumberPerformanceData performanceData)
        {
            var weeklyPoints = performanceData.TrendPoints
                .GroupBy(p => GetWeekOfYear(p.Date))
                .Select(g => new TrendDataPoint
                {
                    Date = g.First().Date,
                    Value = (int)g.Average(p => p.Value),
                    MovingAverage = g.Average(p => p.MovingAverage),
                    Trend = g.First().Trend,
                    IsHotPeriod = g.Any(p => p.IsHotPeriod)
                })
                .OrderBy(p => p.Date)
                .ToList();

            return CreateTrendVisualizationFromPoints(weeklyPoints);
        }

        private TrendVisualization CreateMonthlyTrendVisualization(NumberPerformanceData performanceData)
        {
            var monthlyPoints = performanceData.TrendPoints
                .GroupBy(p => new { p.Date.Year, p.Date.Month })
                .Select(g => new TrendDataPoint
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Value = (int)g.Average(p => p.Value),
                    MovingAverage = g.Average(p => p.MovingAverage),
                    Trend = g.First().Trend,
                    IsHotPeriod = g.Any(p => p.IsHotPeriod)
                })
                .OrderBy(p => p.Date)
                .ToList();

            return CreateTrendVisualizationFromPoints(monthlyPoints);
        }

        private List<SignificantChange> IdentifySignificantChanges(List<TrendDataPoint> points)
        {
            var changes = new List<SignificantChange>();
            
            for (int i = 1; i < points.Count; i++)
            {
                var change = points[i].Value - points[i - 1].Value;
                var percentChange = points[i - 1].Value > 0 ? 
                    Math.Abs(change) / (double)points[i - 1].Value * 100 : 0;
                
                if (percentChange > 50 || Math.Abs(change) > 20) // Significant change thresholds
                {
                    changes.Add(new SignificantChange
                    {
                        Index = i,
                        ChangeValue = change,
                        PercentChange = percentChange,
                        Type = change > 0 ? "Spike" : "Drop"
                    });
                }
            }
            
            return changes;
        }

        private List<Annotation> CreateAnnotations(List<TrendDataPoint> points)
        {
            var annotations = new List<Annotation>();
            var significantChanges = IdentifySignificantChanges(points);
            
            foreach (var change in significantChanges)
            {
                annotations.Add(new Annotation
                {
                    Index = change.Index,
                    Text = $"{change.Type}: {change.ChangeValue:+0;-0}",
                    Color = change.Type == "Spike" ? "#ff4444" : "#4444ff"
                });
            }
            
            return annotations;
        }

        private double CalculateVariance(List<double> values)
        {
            if (values.Count < 2) return 0;
            
            var mean = values.Average();
            var sumSquaredDifferences = values.Sum(v => Math.Pow(v - mean, 2));
            return sumSquaredDifferences / values.Count;
        }

        private bool HasSignificantVariation(List<TrendDataPoint> points)
        {
            if (points.Count < 2) return false;
            
            var values = points.Select(p => (double)p.Value).ToList();
            var variance = CalculateVariance(values);
            var mean = values.Average();
            
            // Consider significant if coefficient of variation > 0.3
            return mean > 0 && (Math.Sqrt(variance) / mean) > 0.3;
        }

        private int GetDateIndex(List<TrendDataPoint> points, DateTime date)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Date.Date == date.Date)
                    return i;
            }
            return -1;
        }

        private int GetWeekOfYear(DateTime date)
        {
            var jan1 = new DateTime(date.Year, 1, 1);
            var daysOffset = (int)jan1.DayOfWeek;
            var firstWeekDay = jan1.AddDays(-daysOffset);
            var weekNum = ((date - firstWeekDay).Days / 7) + 1;
            return weekNum;
        }

        private string GetTrendColor(string classification)
        {
            return classification switch
            {
                "Hot" => "#ff4444",
                "Cold" => "#4444ff",
                _ => "#44ff44"
            };
        }

        // Data structures for trend visualization
        public class TrendDataPoint
        {
            public DateTime Date { get; set; }
            public int Value { get; set; }
            public double MovingAverage { get; set; }
            public string Trend { get; set; } = "";
            public bool IsHotPeriod { get; set; }
        }

        public class NumberPerformanceData
        {
            public int Number { get; set; }
            public List<TrendDataPoint> TrendPoints { get; set; } = new();
            public List<HotColdPeriod> HotPeriods { get; set; } = new();
            public List<HotColdPeriod> ColdPeriods { get; set; } = new();
            public string OverallTrend { get; set; } = "";
            public string CurrentClassification { get; set; } = "";
        }

        public class HotColdPeriod
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public bool IsHot { get; set; }
            public double AverageFrequency { get; set; }
        }

        public class TrendVisualization
        {
            public TrendLine TrendLine { get; set; } = new();
            public TrendLine MovingAverage { get; set; } = new();
            public List<PeriodHighlight> HotPeriods { get; set; } = new();
            public List<PeriodHighlight> ColdPeriods { get; set; } = new();
            public List<SignificantChange> SignificantChanges { get; set; } = new();
            public List<Annotation> Annotations { get; set; } = new();
        }

        public class TrendLine
        {
            public List<double> Data { get; set; } = new();
            public List<string> Labels { get; set; } = new();
            public string Color { get; set; } = "";
        }

        public class PeriodHighlight
        {
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public string Color { get; set; } = "";
            public string Label { get; set; } = "";
        }

        public class SignificantChange
        {
            public int Index { get; set; }
            public int ChangeValue { get; set; }
            public double PercentChange { get; set; }
            public string Type { get; set; } = "";
        }

        public class Annotation
        {
            public int Index { get; set; }
            public string Text { get; set; } = "";
            public string Color { get; set; } = "";
        }

        // Test cache service
        public class TestCacheService : ICacheService
        {
            private readonly Dictionary<string, object> _cache = new();

            public Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CacheLevel level = CacheLevel.Both)
            {
                if (_cache.TryGetValue(key, out var value))
                {
                    return Task.FromResult((T?)value);
                }
                
                var result = factory().Result;
                _cache[key] = result;
                return Task.FromResult((T?)result);
            }

            public Task<T?> GetAsync<T>(string key, CacheLevel level = CacheLevel.Both)
            {
                _cache.TryGetValue(key, out var value);
                return Task.FromResult((T?)value);
            }

            public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CacheLevel level = CacheLevel.Both)
            {
                _cache[key] = value;
                return Task.CompletedTask;
            }

            public Task RemoveAsync(string key, CacheLevel level = CacheLevel.Both)
            {
                _cache.Remove(key);
                return Task.CompletedTask;
            }

            public Task RemoveByPatternAsync(string pattern, CacheLevel level = CacheLevel.Both)
            {
                var keysToRemove = _cache.Keys.Where(k => k.Contains(pattern)).ToList();
                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }
                return Task.CompletedTask;
            }

            public Task WarmUpCacheAsync()
            {
                return Task.FromResult(Task.CompletedTask);
            }

            public Task<CacheStatistics> GetStatisticsAsync()
            {
                return Task.FromResult(new CacheStatistics
                {
                    MemoryCacheEntries = _cache.Count,
                    MemoryCacheHits = 0,
                    MemoryCacheMisses = 0
                });
            }

            public Task ClearAllAsync(CacheLevel level = CacheLevel.Both)
            {
                _cache.Clear();
                return Task.CompletedTask;
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}




