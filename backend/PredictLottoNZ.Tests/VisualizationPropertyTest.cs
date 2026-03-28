using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualizationTest
{
    /**
     * Feature: lottery-lookup-navigation, Property 31: Visualizations are provided for frequency data
     * Validates: Requirements 12.1, 12.2
     */
    public class VisualizationPropertyTest
    {
        // Simple data models for testing
        public class NumberFrequency
        {
            public int Number { get; set; }
            public int TotalOccurrences { get; set; }
            public double Percentage { get; set; }
            public DateTime LastAppearance { get; set; }
            public DateTime FirstAppearance { get; set; }
            public int LongestGap { get; set; }
            public int CurrentGap { get; set; }
            public double AverageFrequency { get; set; }
            public bool IsHot { get; set; }
            public bool IsCold { get; set; }
        }

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
                if (!(frequency.Number >= 1 && frequency.Number <= 40))
                    throw new Exception($"Number {frequency.Number} is outside valid range 1-40");

                // Each frequency must have non-negative occurrences
                if (!(frequency.TotalOccurrences >= 0))
                    throw new Exception($"Total occurrences {frequency.TotalOccurrences} cannot be negative");

                // Each frequency must have valid percentage (0-100)
                if (!(frequency.Percentage >= 0 && frequency.Percentage <= 100))
                    throw new Exception($"Percentage {frequency.Percentage} must be between 0 and 100");

                // Dates must be valid
                if (!(frequency.FirstAppearance <= frequency.LastAppearance))
                    throw new Exception("First appearance must be before or equal to last appearance");

                // Average frequency must be positive if there are occurrences
                if (frequency.TotalOccurrences > 0)
                {
                    if (!(frequency.AverageFrequency > 0))
                        throw new Exception("Average frequency must be positive when occurrences exist");
                }
            }

            // Visualization data structure should be complete
            var visualizationData = CreateVisualizationData(frequencies);
            
            // Must have labels for each data point
            if (frequencies.Count != visualizationData.Labels.Count)
                throw new Exception($"Expected {frequencies.Count} labels, got {visualizationData.Labels.Count}");
            
            // Must have data values for each label
            if (frequencies.Count != visualizationData.Data.Count)
                throw new Exception($"Expected {frequencies.Count} data values, got {visualizationData.Data.Count}");
            
            // All data values must be non-negative
            foreach (var value in visualizationData.Data)
            {
                if (!(value >= 0))
                    throw new Exception($"Data value {value} cannot be negative");
            }
        }

        public void VisualizationSupportsMultipleChartTypes(List<NumberFrequency> frequencies)
        {
            // Property: For any frequency data, multiple visualization types should be supported
            
            if (frequencies == null || frequencies.Count == 0)
                return;

            var visualizationData = CreateVisualizationData(frequencies);

            // Should support bar chart format
            var barChartData = CreateBarChartData(visualizationData);
            if (barChartData == null)
                throw new Exception("Bar chart data cannot be null");
            if (barChartData.Labels == null)
                throw new Exception("Bar chart labels cannot be null");
            if (barChartData.Datasets == null)
                throw new Exception("Bar chart datasets cannot be null");
            if (!(barChartData.Datasets.Count > 0))
                throw new Exception("Bar chart must have at least one dataset");

            // Should support line chart format
            var lineChartData = CreateLineChartData(visualizationData);
            if (lineChartData == null)
                throw new Exception("Line chart data cannot be null");
            if (lineChartData.Labels == null)
                throw new Exception("Line chart labels cannot be null");
            if (lineChartData.Datasets == null)
                throw new Exception("Line chart datasets cannot be null");
            if (!(lineChartData.Datasets.Count > 0))
                throw new Exception("Line chart must have at least one dataset");

            // Should support heatmap format
            var heatmapData = CreateHeatmapData(frequencies);
            if (heatmapData == null)
                throw new Exception("Heatmap data cannot be null");
            if (!(heatmapData.Count > 0))
                throw new Exception("Heatmap must have at least one data point");
        }

        public void VisualizationDataPreservesFrequencyValues(List<NumberFrequency> frequencies)
        {
            // Property: For any frequency data, visualization should preserve original values
            
            if (frequencies == null || frequencies.Count == 0)
                return;

            var visualizationData = CreateVisualizationData(frequencies);
            
            // Sum of visualization data should equal sum of original frequencies
            var originalSum = frequencies.Sum(f => f.TotalOccurrences);
            var visualizationSum = visualizationData.Data.Sum();
            
            if (originalSum != visualizationSum)
                throw new Exception($"Original sum {originalSum} does not match visualization sum {visualizationSum}");
            
            // Each individual value should be preserved
            var sortedFrequencies = frequencies.OrderBy(f => f.Number).ToList();
            for (int i = 0; i < sortedFrequencies.Count; i++)
            {
                var originalFrequency = sortedFrequencies[i];
                var visualizationValue = visualizationData.Data[i];
                
                if (originalFrequency.TotalOccurrences != visualizationValue)
                    throw new Exception($"Original frequency {originalFrequency.TotalOccurrences} does not match visualization value {visualizationValue}");
            }
        }

        public void VisualizationColorsReflectFrequencyClassification(List<NumberFrequency> frequencies)
        {
            // Property: For any frequency data, visualization colors should reflect hot/cold classification
            
            if (frequencies == null || frequencies.Count == 0)
                return;

            var visualizationData = CreateVisualizationData(frequencies);
            
            // Colors should be assigned based on frequency classification
            var sortedFrequencies = frequencies.OrderBy(f => f.Number).ToList();
            for (int i = 0; i < sortedFrequencies.Count; i++)
            {
                var frequency = sortedFrequencies[i];
                var color = visualizationData.Colors[i];
                
                // Hot numbers should have red-ish colors
                if (frequency.IsHot)
                {
                    if (!color.ToLower().Contains("ff4444"))
                        throw new Exception($"Hot number {frequency.Number} should have red color, got {color}");
                }
                // Cold numbers should have blue-ish colors
                else if (frequency.IsCold)
                {
                    if (!color.ToLower().Contains("4444ff"))
                        throw new Exception($"Cold number {frequency.Number} should have blue color, got {color}");
                }
                // Normal numbers should have green-ish colors
                else
                {
                    if (!color.ToLower().Contains("44ff44"))
                        throw new Exception($"Normal number {frequency.Number} should have green color, got {color}");
                }
            }
        }

        public void HeatmapDataMapsNumbersToGridPositions(List<NumberFrequency> frequencies)
        {
            // Property: For any frequency data, heatmap should map numbers to correct grid positions
            
            if (frequencies == null || frequencies.Count == 0)
                return;

            var heatmapData = CreateHeatmapData(frequencies);
            
            foreach (var point in heatmapData)
            {
                // Grid positions should be valid for 8x5 grid (40 numbers)
                if (!(point.X >= 0 && point.X < 8))
                    throw new Exception($"X position {point.X} is outside valid range 0-7");
                if (!(point.Y >= 0 && point.Y < 5))
                    throw new Exception($"Y position {point.Y} is outside valid range 0-4");
                
                // Value should be non-negative
                if (!(point.Value >= 0))
                    throw new Exception($"Heatmap value {point.Value} cannot be negative");
                
                // Color should be valid hex color
                if (point.Color == null || point.Color == "")
                    throw new Exception("Heatmap point color cannot be null or empty");
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
    }

    // Simple test runner
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Running Visualization Display Property Tests...");
            
            var test = new VisualizationPropertyTest();
            
            try
            {
                // Run property tests manually with sample data
                var sampleData = GenerateSampleFrequencyData();
                
                Console.WriteLine("Testing visualization data contains required fields...");
                test.VisualizationDataContainsRequiredFields(sampleData);
                Console.WriteLine("✓ Passed");
                
                Console.WriteLine("Testing visualization supports multiple chart types...");
                test.VisualizationSupportsMultipleChartTypes(sampleData);
                Console.WriteLine("✓ Passed");
                
                Console.WriteLine("Testing visualization data preserves frequency values...");
                test.VisualizationDataPreservesFrequencyValues(sampleData);
                Console.WriteLine("✓ Passed");
                
                Console.WriteLine("Testing visualization colors reflect frequency classification...");
                test.VisualizationColorsReflectFrequencyClassification(sampleData);
                Console.WriteLine("✓ Passed");
                
                Console.WriteLine("Testing heatmap data maps numbers to grid positions...");
                test.HeatmapDataMapsNumbersToGridPositions(sampleData);
                Console.WriteLine("✓ Passed");
                
                Console.WriteLine("\nAll visualization display property tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Test failed: {ex.Message}");
                Environment.Exit(1);
            }
        }
        
        private static List<VisualizationPropertyTest.NumberFrequency> GenerateSampleFrequencyData()
        {
            var random = new Random(42); // Fixed seed for reproducible tests
            var frequencies = new List<VisualizationPropertyTest.NumberFrequency>();
            
            for (int i = 1; i <= 20; i++) // Generate data for numbers 1-20
            {
                var occurrences = random.Next(0, 100);
                var isHot = occurrences > 70;
                var isCold = occurrences < 20;
                
                frequencies.Add(new VisualizationPropertyTest.NumberFrequency
                {
                    Number = i,
                    TotalOccurrences = occurrences,
                    Percentage = (double)occurrences / 100 * 100,
                    LastAppearance = DateTime.Now.AddDays(-random.Next(1, 365)),
                    FirstAppearance = DateTime.Now.AddDays(-random.Next(366, 1000)),
                    LongestGap = random.Next(1, 50),
                    CurrentGap = random.Next(0, 25),
                    AverageFrequency = (double)occurrences / 100,
                    IsHot = isHot,
                    IsCold = isCold
                });
            }
            
            return frequencies;
        }
    }
}