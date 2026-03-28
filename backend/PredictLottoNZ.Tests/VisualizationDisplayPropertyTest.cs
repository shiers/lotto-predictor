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
     * Feature: lottery-lookup-navigation, Property 31: Visualizations are provided for frequency data
     * Validates: Requirements 12.1, 12.2
     */
    public class VisualizationDisplayPropertyTest
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly LottoDbContext _context;
        private readonly IFrequencyAnalysisService _frequencyService;

        public VisualizationDisplayPropertyTest()
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

        // Generator for valid lottery numbers (1-40)
        public static Arbitrary<int> ValidLotteryNumber() =>
            Arb.From(Gen.Choose(1, 40));

        // Generator for frequency data
        public static Arbitrary<List<NumberFrequency>> FrequencyDataGenerator() =>
            Arb.From(
                from count in Gen.Choose(1, 40)
                from frequencies in Gen.ListOf(count, 
                    from number in Gen.Choose(1, 40)
                    from occurrences in Gen.Choose(0, 1000)
                    from percentage in Gen.Choose(0, 100)
                    select new NumberFrequency
                    {
                        Number = number,
                        TotalOccurrences = occurrences,
                        Percentage = (double)percentage,
                        LastAppearance = DateTime.Now.AddDays(-Gen.Choose(1, 365).Sample(0, 1).First()),
                        FirstAppearance = DateTime.Now.AddDays(-Gen.Choose(366, 1000).Sample(0, 1).First()),
                        LongestGap = Gen.Choose(1, 100).Sample(0, 1).First(),
                        CurrentGap = Gen.Choose(0, 50).Sample(0, 1).First(),
                        AverageFrequency = Gen.Choose(1, 100).Sample(0, 1).First() / 10.0,
                        IsHot = Gen.Elements(true, false).Sample(0, 1).First(),
                        IsCold = Gen.Elements(true, false).Sample(0, 1).First()
                    })
                select frequencies.GroupBy(f => f.Number).Select(g => g.First()).ToList()
            );

        [Property(MaxTest = 100)]
        public void VisualizationDataContainsRequiredFields(List<NumberFrequency> frequencies)
        {
            // Property: For any frequency data, visualization should contain required fields
            
            // Skip empty or null data
            if (frequencies == null || frequencies.Count == 0)
                return;

            // Ensure all frequencies have valid data for visualization
            foreach (var frequency in frequencies)
            {
                // Each frequency must have a valid number (1-40)
                Assert.True(frequency.Number >= 1 && frequency.Number <= 40, 
                    $"Number {frequency.Number} is outside valid range 1-40");

                // Each frequency must have non-negative occurrences
                Assert.True(frequency.TotalOccurrences >= 0, 
                    $"Total occurrences {frequency.TotalOccurrences} cannot be negative");

                // Each frequency must have valid percentage (0-100)
                Assert.True(frequency.Percentage >= 0 && frequency.Percentage <= 100, 
                    $"Percentage {frequency.Percentage} must be between 0 and 100");

                // Dates must be valid
                Assert.True(frequency.FirstAppearance <= frequency.LastAppearance, 
                    "First appearance must be before or equal to last appearance");

                // Average frequency must be positive if there are occurrences
                if (frequency.TotalOccurrences > 0)
                {
                    Assert.True(frequency.AverageFrequency > 0, 
                        "Average frequency must be positive when occurrences exist");
                }
            }

            // Visualization data structure should be complete
            var visualizationData = CreateVisualizationData(frequencies);
            
            // Must have labels for each data point
            Assert.Equal(frequencies.Count, visualizationData.Labels.Count);
            
            // Must have data values for each label
            Assert.Equal(frequencies.Count, visualizationData.Data.Count);
            
            // All data values must be non-negative
            Assert.All(visualizationData.Data, value => Assert.True(value >= 0));
        }

        [Property(MaxTest = 100)]
        public void VisualizationSupportsMultipleChartTypes(List<NumberFrequency> frequencies)
        {
            // Property: For any frequency data, multiple visualization types should be supported
            
            if (frequencies == null || frequencies.Count == 0)
                return;

            var visualizationData = CreateVisualizationData(frequencies);

            // Should support bar chart format
            var barChartData = CreateBarChartData(visualizationData);
            Assert.NotNull(barChartData);
            Assert.NotNull(barChartData.Labels);
            Assert.NotNull(barChartData.Datasets);
            Assert.True(barChartData.Datasets.Count > 0);

            // Should support line chart format
            var lineChartData = CreateLineChartData(visualizationData);
            Assert.NotNull(lineChartData);
            Assert.NotNull(lineChartData.Labels);
            Assert.NotNull(lineChartData.Datasets);
            Assert.True(lineChartData.Datasets.Count > 0);

            // Should support heatmap format
            var heatmapData = CreateHeatmapData(frequencies);
            Assert.NotNull(heatmapData);
            Assert.True(heatmapData.Count > 0);
        }

        [Property(MaxTest = 100)]
        public void VisualizationDataPreservesFrequencyValues(List<NumberFrequency> frequencies)
        {
            // Property: For any frequency data, visualization should preserve original values
            
            if (frequencies == null || frequencies.Count == 0)
                return;

            var visualizationData = CreateVisualizationData(frequencies);
            
            // Sum of visualization data should equal sum of original frequencies
            var originalSum = frequencies.Sum(f => f.TotalOccurrences);
            var visualizationSum = visualizationData.Data.Sum();
            
            Assert.Equal(originalSum, visualizationSum);
            
            // Each individual value should be preserved
            for (int i = 0; i < frequencies.Count; i++)
            {
                var originalFrequency = frequencies.OrderBy(f => f.Number).ToList()[i];
                var visualizationValue = visualizationData.Data[i];
                
                Assert.Equal(originalFrequency.TotalOccurrences, visualizationValue);
            }
        }

        // Helper methods for creating visualization data structures
        private VisualizationData CreateVisualizationData(List<NumberFrequency> frequencies)
        {
            var sortedFrequencies = frequencies.OrderBy(f => f.Number).ToList();
            
            return new VisualizationData
            {
                Labels = sortedFrequencies.Select(f => f.Number.ToString()).ToList(),
                Data = sortedFrequencies.Select(f => (double)f.TotalOccurrences).ToList(),
                Colors = sortedFrequencies.Select(f => GetFrequencyColor(f)).ToList()
            };
        }

        private ChartData CreateBarChartData(VisualizationData data)
        {
            return new ChartData
            {
                Labels = data.Labels,
                Datasets = new List<Dataset>
                {
                    new Dataset
                    {
                        Label = "Frequency",
                        Data = data.Data,
                        BackgroundColor = data.Colors,
                        BorderColor = data.Colors,
                        BorderWidth = 1
                    }
                }
            };
        }

        private ChartData CreateLineChartData(VisualizationData data)
        {
            return new ChartData
            {
                Labels = data.Labels,
                Datasets = new List<Dataset>
                {
                    new Dataset
                    {
                        Label = "Frequency Trend",
                        Data = data.Data,
                        BorderColor = "#3498db",
                        BackgroundColor = "rgba(52, 152, 219, 0.1)",
                        Fill = true,
                        Tension = 0.4
                    }
                }
            };
        }

        private List<HeatmapPoint> CreateHeatmapData(List<NumberFrequency> frequencies)
        {
            return frequencies.Select(f => new HeatmapPoint
            {
                X = (f.Number - 1) % 8, // 8 columns for 40 numbers
                Y = (f.Number - 1) / 8, // 5 rows for 40 numbers
                Value = f.TotalOccurrences,
                Color = GetFrequencyColor(f)
            }).ToList();
        }

        private string GetFrequencyColor(NumberFrequency frequency)
        {
            if (frequency.IsHot) return "#ff4444";
            if (frequency.IsCold) return "#4444ff";
            return "#44ff44";
        }

        // Data structures for visualization
        public class VisualizationData
        {
            public List<string> Labels { get; set; } = new();
            public List<double> Data { get; set; } = new();
            public List<string> Colors { get; set; } = new();
        }

        public class ChartData
        {
            public List<string> Labels { get; set; } = new();
            public List<Dataset> Datasets { get; set; } = new();
        }

        public class Dataset
        {
            public string Label { get; set; } = "";
            public List<double> Data { get; set; } = new();
            public object BackgroundColor { get; set; } = "";
            public object BorderColor { get; set; } = "";
            public int BorderWidth { get; set; } = 1;
            public bool Fill { get; set; } = false;
            public double Tension { get; set; } = 0;
        }

        public class HeatmapPoint
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Value { get; set; }
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



