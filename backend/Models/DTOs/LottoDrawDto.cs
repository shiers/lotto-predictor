namespace PredictLottoNZ.Models.DTOs;

public class LottoDrawDto
{
    public int Draw { get; set; }
    public DateTime Date { get; set; }
    public int[] WinningNumbers { get; set; } = Array.Empty<int>();
    public int BonusNumber { get; set; }
    public int Powerball { get; set; }
    
    // Individual winning number properties for backward compatibility
    public int WinningNumber1 => WinningNumbers.Length > 0 ? WinningNumbers[0] : 0;
    public int WinningNumber2 => WinningNumbers.Length > 1 ? WinningNumbers[1] : 0;
    public int WinningNumber3 => WinningNumbers.Length > 2 ? WinningNumbers[2] : 0;
    public int WinningNumber4 => WinningNumbers.Length > 3 ? WinningNumbers[3] : 0;
    public int WinningNumber5 => WinningNumbers.Length > 4 ? WinningNumbers[4] : 0;
    public int WinningNumber6 => WinningNumbers.Length > 5 ? WinningNumbers[5] : 0;
    
    // Prize division properties
    public decimal? Division1Prize { get; set; }
    public decimal? Division2Prize { get; set; }
    public decimal? Division3Prize { get; set; }
    public decimal? Division4Prize { get; set; }
    public decimal? Division5Prize { get; set; }
    public decimal? Division6Prize { get; set; }
    public decimal? Division7Prize { get; set; }
    
    public static LottoDrawDto FromEntity(LottoDraw draw)
    {
        return new LottoDrawDto
        {
            Draw = draw.Draw,
            Date = draw.Date,
            WinningNumbers = new[] 
            { 
                draw.WinningNumber1, 
                draw.WinningNumber2, 
                draw.WinningNumber3, 
                draw.WinningNumber4, 
                draw.WinningNumber5, 
                draw.WinningNumber6 
            },
            BonusNumber = draw.BonusNumber,
            Powerball = draw.Powerball
        };
    }
}