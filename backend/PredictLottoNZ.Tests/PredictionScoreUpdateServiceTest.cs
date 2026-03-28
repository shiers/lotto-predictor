using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Services;
using Xunit;

namespace PredictLottoNZ.Tests;

public class PredictionScoreUpdateServiceTest : IDisposable
{
    private readonly LottoDbContext _context;
    private readonly Mock<IAccuracyAnalysisService> _mockAccuracyService;
    private readonly Mock<ILogger<PredictionScoreUpdateService>> _mockLogger;
    private readonly PredictionScoreUpdateService _service;

    public PredictionScoreUpdateServiceTest()
    {
        var options = new DbContextOptionsBuilder<LottoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LottoDbContext(options);
        _mockAccuracyService = new Mock<IAccuracyAnalysisService>();
        _mockLogger = new Mock<ILogger<PredictionScoreUpdateService>>();
        
        _service = new PredictionScoreUpdateService(
            _context,
            _mockAccuracyService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task UpdateScoresForSingleDrawAsync_WithValidDraw_UpdatesPredictionScores()
    {
        // Arrange
        var prediction = new Prediction
        {
            Id = 1,
            Source = "TestProvider",
            Number1 = 1, Number2 = 2, Number3 = 3,
            Number4 = 4, Number5 = 5, Number6 = 6,
            Score = 0.5,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var draw = new LottoDraw
        {
            Draw = 100,
            Date = DateTime.UtcNow,
            WinningNumber1 = 1, WinningNumber2 = 2, WinningNumber3 = 7,
            WinningNumber4 = 8, WinningNumber5 = 9, WinningNumber6 = 10,
            BonusNumber = 11,
            Powerball = 5
        };

        _context.Predictions.Add(prediction);
        _context.LottoDraws.Add(draw);
        await _context.SaveChangesAsync();

        var accuracyMetrics = new AccuracyMetrics
        {
            AccuracyResults = new List<PredictionAccuracyResult>
            {
                new PredictionAccuracyResult
                {
                    PredictionId = prediction.Id,
                    ProviderName = prediction.Source,
                    ExactMatches = 2,
                    PartialMatches = 1,
                    ProximityScore = 0.7,
                    OverallAccuracy = 0.6
                }
            }
        };

        _mockAccuracyService
            .Setup(x => x.AnalyzePredictionAccuracyAsync(It.IsAny<LottoDraw>()))
            .ReturnsAsync(accuracyMetrics);

        // Act
        var result = await _service.UpdateScoresForSingleDrawAsync(draw);

        // Assert
        Assert.Equal(1, result.TotalPredictionsProcessed);
        Assert.Equal(1, result.PredictionsUpdated);
        Assert.Equal(1, result.ScoreHistoryRecordsCreated);
        Assert.Empty(result.Errors);

        // Verify prediction was updated
        var updatedPrediction = await _context.Predictions.FindAsync(prediction.Id);
        Assert.NotNull(updatedPrediction);
        Assert.NotNull(updatedPrediction.UpdatedScore);
        Assert.NotNull(updatedPrediction.LastScoreUpdate);
        Assert.NotEqual(prediction.Score, updatedPrediction.UpdatedScore);

        // Verify score history was created
        var scoreHistory = await _context.PredictionScoreHistories
            .FirstOrDefaultAsync(psh => psh.PredictionId == prediction.Id);
        Assert.NotNull(scoreHistory);
        Assert.Equal(prediction.Score, scoreHistory.OriginalScore);
        Assert.Equal(updatedPrediction.UpdatedScore, scoreHistory.UpdatedScore);
        Assert.Equal(draw.Draw, scoreHistory.TriggeringDrawId);
        Assert.Equal(2, scoreHistory.ExactMatches);
    }

    [Fact]
    public async Task GetScoreHistoryAsync_WithValidPredictionId_ReturnsHistory()
    {
        // Arrange
        var prediction = new Prediction
        {
            Id = 1,
            Source = "TestProvider",
            Number1 = 1, Number2 = 2, Number3 = 3,
            Number4 = 4, Number5 = 5, Number6 = 6,
            Score = 0.5
        };

        var draw = new LottoDraw
        {
            Draw = 100,
            Date = DateTime.UtcNow,
            WinningNumber1 = 1, WinningNumber2 = 2, WinningNumber3 = 7,
            WinningNumber4 = 8, WinningNumber5 = 9, WinningNumber6 = 10,
            BonusNumber = 11,
            Powerball = 5
        };

        var scoreHistory = new PredictionScoreHistory
        {
            PredictionId = prediction.Id,
            OriginalScore = 0.5,
            UpdatedScore = 0.6,
            UpdatedAt = DateTime.UtcNow,
            UpdateReason = "Test update",
            TriggeringDrawId = draw.Draw,
            ExactMatches = 2,
            PartialMatches = 1,
            ProximityScore = 0.7,
            OverallAccuracy = 0.6
        };

        _context.Predictions.Add(prediction);
        _context.LottoDraws.Add(draw);
        _context.PredictionScoreHistories.Add(scoreHistory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetScoreHistoryAsync(prediction.Id);

        // Assert
        Assert.Single(result);
        var historyItem = result.First();
        Assert.Equal(scoreHistory.OriginalScore, historyItem.OriginalScore);
        Assert.Equal(scoreHistory.UpdatedScore, historyItem.UpdatedScore);
        Assert.Equal(scoreHistory.UpdateReason, historyItem.UpdateReason);
        Assert.Equal(scoreHistory.ExactMatches, historyItem.ExactMatches);
    }

    [Fact]
    public async Task GetLatestScoresAsync_WithValidPredictionIds_ReturnsScoreInfo()
    {
        // Arrange
        var prediction = new Prediction
        {
            Id = 1,
            Source = "TestProvider",
            Number1 = 1, Number2 = 2, Number3 = 3,
            Number4 = 4, Number5 = 5, Number6 = 6,
            Score = 0.5,
            UpdatedScore = 0.6,
            LastScoreUpdate = DateTime.UtcNow
        };

        _context.Predictions.Add(prediction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetLatestScoresAsync(new[] { prediction.Id });

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey(prediction.Id));
        
        var scoreInfo = result[prediction.Id];
        Assert.Equal(prediction.Id, scoreInfo.PredictionId);
        Assert.Equal(prediction.Score, scoreInfo.OriginalScore);
        Assert.Equal(prediction.UpdatedScore, scoreInfo.UpdatedScore);
        Assert.Equal(prediction.Source, scoreInfo.ProviderName);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}