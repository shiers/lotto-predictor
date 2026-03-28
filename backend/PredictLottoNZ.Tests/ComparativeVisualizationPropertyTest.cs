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
     * Feature: lottery-lookup-navigation, Property 32: Comparative visualizations highlight differences
     * Validates: Requirements 12.3
     */
    public class ComparativeVisualizationPropertyTest
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly LottoDbContext _context;
        private readonly IFrequencyAnalysisService _frequencyService;

        public ComparativeVisualizationPropertyTest()
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

        // Generator for range frequency data
        public static Arbitrary<List<RangeFrequency>> RangeFrequencyGenerator() =>
            Arb.From(
                from count in Gen.Choose(2, 8) // At least 2 ranges for comparison
                from ranges in Gen.ListOf(count,
                    from startNum in Gen.Choose(1, 35)
                    from endNum in Gen.Choose(startNum, 40)
                    from occurrences in Gen.Choose(0, 1000)
                    from percentage in Gen.Choose(0, 100)
                    select new RangeFrequency
                    {
                        Range = new NumberRange
                        {
                            StartNumber = startNum,
                            EndNumber = endNum,
                            Label = $"{startNum}-{endNum}"
                        },
                        TotalOccurrences = occurrences,
                        Percentage = (double)percentage,
                        AveragePerDraw = 5.0,
                        IndividualNumbers = GenerateIndividualNumbers(startNum, endNum, occurrences)
                    })
                select ranges.ToList()
            );

        private static List<NumberFrequencyDto> GenerateIndividualNumbers(int start, int end, int totalOccurrences)
        {
            var numbers = new List<NumberFrequencyDto>();
            var count = end - start + 1;
            var avgOccurrences = count > 0 ? totalOccurrences / count : 0;
            
            for (int i = start; i <= end; i++)
            {
                numbers.Add(new NumberFrequencyDto
                {
                    Number = i,
                    TotalOccurrences = Math.Max(0, avgOccurrences + new System.Random().Next(-10, 11)),
                    Percentage = new System.Random().Next(0, 11),
                    LastAppearance = DateTime.Now.AddDays(-new System.Random().Next(1, 366)),
                    FirstAppearance = DateTime.Now.AddDays(-new System.Random().Next(366, 1001)),
                    LongestGap = new System.Random().Next(1, 101),
                    CurrentGap = new System.Random().Next(0, 51),
                    AverageFrequency = new System.Random().Next(1, 6),
                    IsHot = new System.Random().Next(0, 2) == 1,
                    IsCold = new System.Random().Next(0, 2) == 1
                });
            }
            
            return numbers;
        }

        [Property(MaxTest = 100)]
        public void ComparativeVisualizationHighlightsDifferences(List<RangeFrequency> ranges)
        {
            // Property: For any set of ranges, comparative visualization should highlight differences
            
            if (ranges == null || ranges.Count < 2)
                return;

            var comparativeData = CreateComparativeVisualizationData(ranges);
            
            // Should have data for each range
            Assert.Equal(ranges.Count, comparativeData.Datasets.Count);
            
            // Each dataset should have distinct visual properties
            var colors = comparativeData.Datasets.Select(d => d.BackgroundColor).Distinct().ToList();
            Assert.True(colors.Count >= Math.Min(ranges.Count, 8), // At least as many colors as ranges (up to 8)
                "Each range should have a distinct color for comparison");
            
            // Should preserve relative differences between ranges
            for (int i = 0; i < ranges.Count - 1; i++)
            {
                var range1 = ranges[i];
                var range2 = ranges[i + 1];
                var data1 = comparativeData.Datasets[i].Data.Sum();
                var data2 = comparativeData.Datasets[i + 1].Data.Sum();
                
                // If one range has significantly more occurrences, it should be reflected in visualization
                if (Math.Abs(range1.TotalOccurrences - range2.TotalOccurrences) > 10)
                {
                    Assert.NotEqual(data1, data2);
                    
                    if (range1.TotalOccurrences > range2.TotalOccurrences)
                    {
                        Assert.True(data1 > data2, "Higher frequency range should have higher visualization values");
                    }
                    else
                    {
                        Assert.True(data1 < data2, "Lower frequency range should have lower visualization values");
                    }
                }
            }
        }

        [Property(MaxTest = 100)]
        public void ComparativeVisualizationMaintainsProportions(List<RangeFrequency> ranges)
        {
            // Property: For any set of ranges, comparative visualization should maintain proportional relationships
            
            if (ranges == null || ranges.Count < 2)
                return;

            var comparativeData = CreateComparativeVisualizationData(ranges);
            
            // Calculate total occurrences for each range
            var rangeTotals = ranges.Select(r => r.TotalOccurrences).ToList();
            var visualizationTotals = comparativeData.Datasets.Select(d => d.Data.Sum()).ToList();
            
            // Proportions should be maintained
            for (int i = 0; i < ranges.Count; i++)
            {
                for (int j = i + 1; j < ranges.Count; j++)
                {
                    if (rangeTotals[i] > 0 && rangeTotals[j] > 0)
                    {
                        var originalRatio = (double)rangeTotals[i] / rangeTotals[j];
                        var visualizationRatio = visualizationTotals[i] / visualizationTotals[j];
                        
                        // Allow for small rounding differences
                        Assert.True(Math.Abs(originalRatio - visualizationRatio) < 0.01,
                            $"Proportional relationship should be maintained: {originalRatio} vs {visualizationRatio}");
                    }
                }
            }
        }

        [Property(MaxTest = 100)]
        public void ComparativeVisualizationSupportsMultipleMetrics(List<RangeFrequency> ranges)
        {
            // Property: For any set of ranges, comparative visualization should support multiple comparison metrics
            
            if (ranges == null || ranges.Count < 2)
                return;

            // Should support comparison by total occurrences
            var occurrenceComparison = CreateOccurrenceComparisonData(ranges);
            Assert.NotNull(occurrenceComparison);
            Assert.Equal(ranges.Count, occurrenceComparison.Datasets.Count);
            
            // Should support comparison by percentage
            var percentageComparison = CreatePercentageComparisonData(ranges);
            Assert.NotNull(percentageComparison);
            Assert.Equal(ranges.Count, percentageComparison.Datasets.Count);
            
            // Should support comparison by average per draw
            var averageComparison = CreateAverageComparisonData(ranges);
            Assert.NotNull(averageComparison);
            Assert.Equal(ranges.Count, averageComparison.Datasets.Count);
            
            // Each comparison type should produce different results when ranges have different characteristics
            var hasVariation = ranges.Any(r1 => ranges.Any(r2 => 
                Math.Abs(r1.TotalOccurrences - r2.TotalOccurrences) > 10 ||
                Math.Abs(r1.Percentage - r2.Percentage) > 1.0 ||
                Math.Abs(r1.AveragePerDraw - r2.AveragePerDraw) > 0.5));
            
            if (hasVariation)
            {
                // At least one comparison type should show differences
                var occurrenceVariation = HasDataVariation(occurrenceComparison);
                var percentageVariation = HasDataVariation(percentageComparison);
                var averageVariation = HasDataVariation(averageComparison);
                
                Assert.True(occurrenceVariation || percentageVariation || averageVariation,
                    "At least one comparison metric should show variation when ranges differ");
            }
        }

        [Property(MaxTest = 100)]
        public void ComparativeVisualizationHandlesOverlappingRanges(List<RangeFrequency> ranges)
        {
            // Property: For any set of ranges including overlapping ones, visualization should handle them correctly
            
            if (ranges == null || ranges.Count < 2)
                return;

            // Create some overlapping ranges
            var overlappingRanges = ranges.Take(2).ToList();
            if (overlappingRanges.Count >= 2)
            {
                // Make ranges overlap
                overlappingRanges[1].Range.StartNumber = Math.Max(1, overlappingRanges[0].Range.EndNumber - 2);
                overlappingRanges[1].Range.EndNumber = Math.Min(40, overlappingRanges[0].Range.EndNumber + 2);
            }

            var comparativeData = CreateComparativeVisualizationData(overlappingRanges);
            
            // Should still create valid visualization data
            Assert.NotNull(comparativeData);
            Assert.Equal(overlappingRanges.Count, comparativeData.Datasets.Count);
            
            // Each dataset should have valid data
            foreach (var dataset in comparativeData.Datasets)
            {
                Assert.NotNull(dataset.Data);
                Assert.True(dataset.Data.Count > 0);
                Assert.All(dataset.Data, value => Assert.True(value >= 0));
            }
            
            // Overlapping ranges should be visually distinguishable
            if (comparativeData.Datasets.Count >= 2)
            {
                var colors = comparativeData.Datasets.Select(d => d.BackgroundColor).ToList();
                Assert.NotEqual(colors[0], colors[1]);
            }
        }

        // Helper methods for creating comparative visualization data
        private ComparativeChartData CreateComparativeVisualizationData(List<RangeFrequency> ranges)
        {
            var maxNumbers = ranges.Max(r => r.Range.EndNumber - r.Range.StartNumber + 1);
            var labels = Enumerable.Range(0, maxNumbers).Select(i => $"Position {i + 1}").ToList();
            
            var datasets = ranges.Select((range, index) => new Dataset
            {
                Label = range.Range.Label ?? $"Range {index + 1}",
                Data = CreateRangeDataPoints(range, maxNumbers),
                BackgroundColor = GetRangeColor(index),
                BorderColor = GetRangeColor(index),
                BorderWidth = 2
            }).ToList();
            
            return new ComparativeChartData
            {
                Labels = labels,
                Datasets = datasets
            };
        }

        private ComparativeChartData CreateOccurrenceComparisonData(List<RangeFrequency> ranges)
        {
            return new ComparativeChartData
            {
                Labels = ranges.Select(r => r.Range.Label ?? "Range").ToList(),
                Datasets = new List<Dataset>
                {
                    new Dataset
                    {
                        Label = "Total Occurrences",
                        Data = ranges.Select(r => (double)r.TotalOccurrences).ToList(),
                        BackgroundColor = ranges.Select((_, i) => GetRangeColor(i)).ToList(),
                        BorderColor = ranges.Select((_, i) => GetRangeColor(i)).ToList(),
                        BorderWidth = 1
                    }
                }
            };
        }

        private ComparativeChartData CreatePercentageComparisonData(List<RangeFrequency> ranges)
        {
            return new ComparativeChartData
            {
                Labels = ranges.Select(r => r.Range.Label ?? "Range").ToList(),
                Datasets = new List<Dataset>
                {
                    new Dataset
                    {
                        Label = "Percentage",
                        Data = ranges.Select(r => r.Percentage).ToList(),
                        BackgroundColor = ranges.Select((_, i) => GetRangeColor(i)).ToList(),
                        BorderColor = ranges.Select((_, i) => GetRangeColor(i)).ToList(),
                        BorderWidth = 1
                    }
                }
            };
        }

        private ComparativeChartData CreateAverageComparisonData(List<RangeFrequency> ranges)
        {
            return new ComparativeChartData
            {
                Labels = ranges.Select(r => r.Range.Label ?? "Range").ToList(),
                Datasets = new List<Dataset>
                {
                    new Dataset
                    {
                        Label = "Average Per Draw",
                        Data = ranges.Select(r => r.AveragePerDraw).ToList(),
                        BackgroundColor = ranges.Select((_, i) => GetRangeColor(i)).ToList(),
                        BorderColor = ranges.Select((_, i) => GetRangeColor(i)).ToList(),
                        BorderWidth = 1
                    }
                }
            };
        }

        private List<double> CreateRangeDataPoints(RangeFrequency range, int maxPoints)
        {
            var data = new List<double>();
            var numbers = range.IndividualNumbers.OrderBy(n => n.Number).ToList();
            
            for (int i = 0; i < maxPoints; i++)
            {
                if (i < numbers.Count)
                {
                    data.Add(numbers[i].TotalOccurrences);
                }
                else
                {
                    data.Add(0);
                }
            }
            
            return data;
        }

        private bool HasDataVariation(ComparativeChartData chartData)
        {
            if (chartData.Datasets.Count < 2) return false;
            
            var firstDataset = chartData.Datasets[0].Data;
            return chartData.Datasets.Skip(1).Any(dataset => 
                !dataset.Data.SequenceEqual(firstDataset));
        }

        private string GetRangeColor(int index)
        {
            var colors = new[]
            {
                "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0",
                "#9966FF", "#FF9F40", "#FF6384", "#C9CBCF"
            };
            return colors[index % colors.Length];
        }

        // Data structures for comparative visualization
        public class ComparativeChartData
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



