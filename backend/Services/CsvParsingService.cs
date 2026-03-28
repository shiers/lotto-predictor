using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using PredictLottoNZ.Models;

namespace PredictLottoNZ.Services;

public interface ICsvParsingService
{
    Task<IEnumerable<LottoDraw>> ParseCsvAsync(Stream csvStream);
}

public class CsvParsingService : ICsvParsingService
{
    private readonly ILogger<CsvParsingService> _logger;
    
    // Header mapping dictionary for transforming CSV headers to property names
    private static readonly Dictionary<string, string> HeaderMappings = new()
    {
        // Core fields
        { "Draw", "Draw" },
        { "Date", "Date" },
        
        // Support both formats for winning numbers
        { "Winning Number 1", "WinningNumber1" },
        { "Winning Number 2", "WinningNumber2" },
        { "Winning Number 3", "WinningNumber3" },
        { "Winning Number 4", "WinningNumber4" },
        { "Winning Number 5", "WinningNumber5" },
        { "Winning Number 6", "WinningNumber6" },
        { "WinningNumber1", "WinningNumber1" },
        { "WinningNumber2", "WinningNumber2" },
        { "WinningNumber3", "WinningNumber3" },
        { "WinningNumber4", "WinningNumber4" },
        { "WinningNumber5", "WinningNumber5" },
        { "WinningNumber6", "WinningNumber6" },
        
        { "Bonus Number", "BonusNumber" },
        { "BonusNumber", "BonusNumber" },
        { "Powerball", "Powerball" },
        { "From Last", "FromLast" },
        { "FromLast", "FromLast" },
        
        // Statistical interval mappings - support multiple formats
        { "1-10", "OneToTen" },
        { "1-Oct", "OneToTen" },
        { "OneToTen", "OneToTen" },
        { "11-20", "ElevenToTwenty" },
        { "Nov-20", "ElevenToTwenty" },
        { "ElevenToTwenty", "ElevenToTwenty" },
        { "21-30", "TwentyOneToThirty" },
        { "TwentyOneToThirty", "TwentyOneToThirty" },
        { "ntyOneToThirty", "TwentyOneToThirty" }, // Handle the typo in the CSV
        { "31-40", "ThirtyOneToForty" },
        { "ThirtyOneToForty", "ThirtyOneToForty" },
        { "Low", "Low" },
        { "High", "High" },
        { "Odd", "Odd" },
        { "Even", "Even" },
        
        // Prize division mappings - support both formats
        { "Division 1 Prize", "Division1Prize" },
        { "Division1Prize", "Division1Prize" },
        { "Division 1 Winners", "Division1Winners" },
        { "Division1Winners", "Division1Winners" },
        { "Division 2 Prize", "Division2Prize" },
        { "Division2Prize", "Division2Prize" },
        { "Division 2 Winners", "Division2Winners" },
        { "Division2Winners", "Division2Winners" },
        { "Division 3 Prize", "Division3Prize" },
        { "Division3Prize", "Division3Prize" },
        { "Division 3 Winners", "Division3Winners" },
        { "Division3Winners", "Division3Winners" },
        { "Division 4 Prize", "Division4Prize" },
        { "Division4Prize", "Division4Prize" },
        { "Division 4 Winners", "Division4Winners" },
        { "Division4Winners", "Division4Winners" },
        { "Division 5 Prize", "Division5Prize" },
        { "Division5Prize", "Division5Prize" },
        { "Division 5 Winners", "Division5Winners" },
        { "Division5Winners", "Division5Winners" },
        { "Division 6 Prize", "Division6Prize" },
        { "Division6Prize", "Division6Prize" },
        { "Division 6 Winners", "Division6Winners" },
        { "Division6Winners", "Division6Winners" },
        { "Division 7 Prize", "Division7Prize" },
        { "Division7Prize", "Division7Prize" },
        { "Division 7 Winners", "Division7Winners" },
        { "Division7Winners", "Division7Winners" }
    };

    public CsvParsingService(ILogger<CsvParsingService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<LottoDraw>> ParseCsvAsync(Stream csvStream)
    {
        var results = new List<LottoDraw>();
        
        using var reader = new StreamReader(csvStream);
        
        // Read all content first to handle potential formatting issues
        var content = await reader.ReadToEndAsync();
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogWarning("CSV file is empty");
            return results;
        }

        _logger.LogInformation("CSV content length: {Length} characters", content.Length);
        
        // Split content into lines, handling different line endings and malformed headers
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        _logger.LogInformation("Found {LineCount} lines in CSV", lines.Length);
        
        // Handle case where header and data might be concatenated in a single line
        if (lines.Length == 1 && lines[0].Contains("Division7Prize"))
        {
            var singleLine = lines[0];
            
            // Look for the pattern where Division7Prize is followed immediately by a number (draw number)
            var pattern = @"Division7Prize(\d+)";
            var match = Regex.Match(singleLine, pattern);
            
            if (match.Success)
            {
                var splitPoint = match.Groups[1].Index; // Start of the draw number
                var headerPart = singleLine.Substring(0, splitPoint);
                var dataPart = singleLine.Substring(splitPoint);
                
                _logger.LogInformation("Detected concatenated header and data. Header ends at position {SplitPoint}", splitPoint);
                _logger.LogInformation("Header part: {HeaderPart}", headerPart.Length > 100 ? headerPart.Substring(0, 100) + "..." : headerPart);
                _logger.LogInformation("Data part: {DataPart}", dataPart.Length > 100 ? dataPart.Substring(0, 100) + "..." : dataPart);
                
                // Now split the data part into individual rows
                // We'll use a simple approach: split by patterns that look like draw numbers followed by dates
                var dataLines = new List<string>();
                var drawPattern = @"(\d{4}),(\d{1,2}/\d{1,2}/\d{4})";
                var matches = Regex.Matches(dataPart, drawPattern);
                
                if (matches.Count > 0)
                {
                    for (int i = 0; i < matches.Count; i++)
                    {
                        var currentMatch = matches[i];
                        var startIndex = currentMatch.Index;
                        var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : dataPart.Length;
                        
                        var rowData = dataPart.Substring(startIndex, endIndex - startIndex).TrimEnd(',');
                        if (!string.IsNullOrWhiteSpace(rowData))
                        {
                            dataLines.Add(rowData);
                        }
                    }
                }
                else
                {
                    // Fallback: treat the entire data part as one line
                    dataLines.Add(dataPart.TrimEnd(','));
                }
                
                // Combine header and data lines
                var allLines = new List<string> { headerPart };
                allLines.AddRange(dataLines);
                lines = allLines.ToArray();
                
                _logger.LogInformation("Fixed malformed CSV: split into header + {DataLineCount} data lines", dataLines.Count);
            }
        }
        
        if (lines.Length == 0)
        {
            _logger.LogWarning("No lines found in CSV file");
            return results;
        }

        // First line should be headers
        var headerLine = lines[0];
        _logger.LogInformation("Header line: {HeaderLine}", headerLine.Length > 200 ? headerLine.Substring(0, 200) + "..." : headerLine);
        
        var headers = ParseCsvLine(headerLine);
        _logger.LogInformation("Parsed {HeaderCount} headers: {Headers}", headers.Length, string.Join(", ", headers.Take(10)));
        
        var propertyMappings = CreatePropertyMappings(headers);
        _logger.LogInformation("Created {MappingCount} property mappings", propertyMappings.Count);
        
        // Process data lines
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;
                
            try
            {
                var values = ParseCsvLine(line);
                _logger.LogDebug("Line {LineNumber}: Parsed {ValueCount} values", i + 1, values.Length);
                
                var lottoDraw = ParseLottoDrawFromValues(values, propertyMappings, i + 1);
                
                if (lottoDraw != null)
                {
                    results.Add(lottoDraw);
                    _logger.LogDebug("Successfully parsed draw {DrawNumber}", lottoDraw.Draw);
                }
                else
                {
                    _logger.LogWarning("Failed to create LottoDraw from line {LineNumber}", i + 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse line {LineNumber}: {Line}", i + 1, line.Length > 100 ? line.Substring(0, 100) + "..." : line);
                // Continue processing other lines
            }
        }
        
        _logger.LogInformation("Successfully parsed {Count} lottery draws from CSV", results.Count);
        return results;
    }

    private string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var inQuotes = false;
        var currentValue = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.Trim());
                currentValue = "";
            }
            else
            {
                currentValue += c;
            }
        }
        
        // Add the last value
        values.Add(currentValue.Trim());
        
        return values.ToArray();
    }

    private Dictionary<string, int> CreatePropertyMappings(string[] headers)
    {
        var mappings = new Dictionary<string, int>();
        
        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i].Trim();
            
            // Special handling for the specific CSV format where winning numbers are "Winning Number 1,2,3,4,5,6"
            if (header == "Winning Number 1")
            {
                mappings["WinningNumber1"] = i;
                _logger.LogDebug("Mapped header '{Header}' to property 'WinningNumber1' at index {Index}", header, i);
                continue;
            }
            else if (header == "2" && i > 0 && headers[i-1] == "Winning Number 1")
            {
                mappings["WinningNumber2"] = i;
                _logger.LogDebug("Mapped header '{Header}' to property 'WinningNumber2' at index {Index}", header, i);
                continue;
            }
            else if (header == "3" && i > 1 && headers[i-2] == "Winning Number 1")
            {
                mappings["WinningNumber3"] = i;
                _logger.LogDebug("Mapped header '{Header}' to property 'WinningNumber3' at index {Index}", header, i);
                continue;
            }
            else if (header == "4" && i > 2 && headers[i-3] == "Winning Number 1")
            {
                mappings["WinningNumber4"] = i;
                _logger.LogDebug("Mapped header '{Header}' to property 'WinningNumber4' at index {Index}", header, i);
                continue;
            }
            else if (header == "5" && i > 3 && headers[i-4] == "Winning Number 1")
            {
                mappings["WinningNumber5"] = i;
                _logger.LogDebug("Mapped header '{Header}' to property 'WinningNumber5' at index {Index}", header, i);
                continue;
            }
            else if (header == "6" && i > 4 && headers[i-5] == "Winning Number 1")
            {
                mappings["WinningNumber6"] = i;
                _logger.LogDebug("Mapped header '{Header}' to property 'WinningNumber6' at index {Index}", header, i);
                continue;
            }
            
            // Try direct mapping first
            if (HeaderMappings.ContainsKey(header))
            {
                var propertyName = HeaderMappings[header];
                mappings[propertyName] = i;
                _logger.LogDebug("Mapped header '{Header}' to property '{PropertyName}' at index {Index}", header, propertyName, i);
                continue;
            }
            
            // Try space removal and PascalCase conversion
            var transformedHeader = TransformHeaderToPascalCase(header);
            if (HasProperty<LottoDraw>(transformedHeader))
            {
                mappings[transformedHeader] = i;
                _logger.LogDebug("Transformed and mapped header '{Header}' to property '{PropertyName}' at index {Index}", header, transformedHeader, i);
                continue;
            }
            
            _logger.LogWarning("Unmapped CSV header: '{Header}' at index {Index}", header, i);
        }
        
        _logger.LogInformation("Created mappings for required fields: Draw={Draw}, Date={Date}, WinningNumber1={WN1}, BonusNumber={Bonus}, Powerball={PB}", 
            mappings.ContainsKey("Draw"), mappings.ContainsKey("Date"), mappings.ContainsKey("WinningNumber1"), 
            mappings.ContainsKey("BonusNumber"), mappings.ContainsKey("Powerball"));
        
        return mappings;
    }

    private string TransformHeaderToPascalCase(string header)
    {
        if (string.IsNullOrEmpty(header))
            return header;
            
        // Remove spaces and convert to PascalCase
        var words = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = "";
        
        foreach (var word in words)
        {
            if (!string.IsNullOrEmpty(word))
            {
                result += char.ToUpper(word[0]) + word.Substring(1).ToLower();
            }
        }
        
        return result;
    }

    private bool HasProperty<T>(string propertyName)
    {
        return typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance) != null;
    }

    private LottoDraw? ParseLottoDrawFromValues(string[] values, Dictionary<string, int> propertyMappings, int lineNumber)
    {
        var lottoDraw = new LottoDraw();
        var properties = typeof(LottoDraw).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            if (!propertyMappings.ContainsKey(property.Name))
                continue;
                
            var columnIndex = propertyMappings[property.Name];
            if (columnIndex >= values.Length)
                continue;
                
            var value = values[columnIndex];
            
            try
            {
                SetPropertyValue(lottoDraw, property, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set property {PropertyName} with value '{Value}' on line {LineNumber}", 
                    property.Name, value, lineNumber);
                
                // For required fields, return null to skip this record
                if (IsRequiredProperty(property))
                {
                    return null;
                }
            }
        }
        
        // Validate required fields are set
        if (!ValidateRequiredFields(lottoDraw))
        {
            _logger.LogWarning("Missing required fields on line {LineNumber}", lineNumber);
            return null;
        }
        
        return lottoDraw;
    }

    private void SetPropertyValue(LottoDraw lottoDraw, PropertyInfo property, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Only set null for nullable properties
            if (IsNullableProperty(property))
            {
                property.SetValue(lottoDraw, null);
            }
            return;
        }

        // Clean the value - remove quotes and trim
        var cleanValue = value.Trim('"').Trim();
        
        var propertyType = property.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        object? convertedValue = underlyingType.Name switch
        {
            nameof(Int32) => int.Parse(cleanValue),
            nameof(Decimal) => decimal.Parse(cleanValue, CultureInfo.InvariantCulture),
            nameof(DateTime) => ParseDateTime(cleanValue),
            nameof(String) => cleanValue,
            _ => throw new NotSupportedException($"Property type {underlyingType.Name} is not supported")
        };

        property.SetValue(lottoDraw, convertedValue);
    }

    private DateTime ParseDateTime(string value)
    {
        // Try multiple date formats commonly used in CSV files
        var formats = new[]
        {
            "dd/MM/yyyy",
            "MM/dd/yyyy", 
            "yyyy-MM-dd",
            "dd-MM-yyyy",
            "MM-dd-yyyy",
            "dd/MM/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                // Convert to UTC for PostgreSQL compatibility
                return DateTime.SpecifyKind(result, DateTimeKind.Utc);
            }
        }

        // Fallback to general parsing
        if (DateTime.TryParse(value, out var fallbackResult))
        {
            // Convert to UTC for PostgreSQL compatibility
            return DateTime.SpecifyKind(fallbackResult, DateTimeKind.Utc);
        }

        throw new FormatException($"Unable to parse date: {value}");
    }

    private bool IsNullableProperty(PropertyInfo property)
    {
        return Nullable.GetUnderlyingType(property.PropertyType) != null || 
               !property.PropertyType.IsValueType;
    }

    private bool IsRequiredProperty(PropertyInfo property)
    {
        return property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>() != null;
    }

    private bool ValidateRequiredFields(LottoDraw lottoDraw)
    {
        var validationErrors = new List<string>();
        
        // Check essential fields that must be present
        if (lottoDraw.Draw <= 0) validationErrors.Add($"Draw number invalid: {lottoDraw.Draw}");
        if (lottoDraw.Date == default) validationErrors.Add("Date is default/empty");
        if (lottoDraw.WinningNumber1 <= 0) validationErrors.Add($"WinningNumber1 invalid: {lottoDraw.WinningNumber1}");
        if (lottoDraw.WinningNumber2 <= 0) validationErrors.Add($"WinningNumber2 invalid: {lottoDraw.WinningNumber2}");
        if (lottoDraw.WinningNumber3 <= 0) validationErrors.Add($"WinningNumber3 invalid: {lottoDraw.WinningNumber3}");
        if (lottoDraw.WinningNumber4 <= 0) validationErrors.Add($"WinningNumber4 invalid: {lottoDraw.WinningNumber4}");
        if (lottoDraw.WinningNumber5 <= 0) validationErrors.Add($"WinningNumber5 invalid: {lottoDraw.WinningNumber5}");
        if (lottoDraw.WinningNumber6 <= 0) validationErrors.Add($"WinningNumber6 invalid: {lottoDraw.WinningNumber6}");
        if (lottoDraw.BonusNumber <= 0) validationErrors.Add($"BonusNumber invalid: {lottoDraw.BonusNumber}");
        if (lottoDraw.Powerball <= 0) validationErrors.Add($"Powerball invalid: {lottoDraw.Powerball}");
        
        if (validationErrors.Any())
        {
            _logger.LogWarning("Validation failed for draw {Draw}: {Errors}", lottoDraw.Draw, string.Join(", ", validationErrors));
            return false;
        }
        
        return true;
    }
}