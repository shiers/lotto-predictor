using PredictLottoNZ.Tests;

namespace PredictLottoNZ.Tests;

public class TestDataPreservation
{
    public static async Task RunTest(string[] args)
    {
        Console.WriteLine("Running Data Preservation Property Test...");
        try
        {
            await DataPreservationPropertyTest.RunDataPreservationPropertyTest();
            Console.WriteLine("Data preservation test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}