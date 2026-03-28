using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Models;

namespace PredictLottoNZ.Data;

public class LottoDbContext : DbContext
{
    public LottoDbContext(DbContextOptions<LottoDbContext> options) : base(options)
    {
    }
    
    public DbSet<LottoDraw> LottoDraws { get; set; } = null!;
    public DbSet<NumberCombination> NumberCombinations { get; set; } = null!;
    public DbSet<Prediction> Predictions { get; set; } = null!;
    public DbSet<ExternalServiceCallLog> ExternalServiceCallLogs { get; set; } = null!;
    public DbSet<NumberFrequency> NumberFrequencies { get; set; } = null!;
    public DbSet<NumberOccurrence> NumberOccurrences { get; set; } = null!;
    public DbSet<ExportJob> ExportJobs { get; set; } = null!;
    public DbSet<SearchConfiguration> SearchConfigurations { get; set; } = null!;
    public DbSet<Bookmark> Bookmarks { get; set; } = null!;
    public DbSet<TrainingRun> TrainingRuns { get; set; } = null!;
    public DbSet<ModelVersion> ModelVersions { get; set; } = null!;
    public DbSet<PredictionAccuracy> PredictionAccuracies { get; set; } = null!;
    public DbSet<PredictionScoreHistory> PredictionScoreHistories { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure LottoDraw entity
        modelBuilder.Entity<LottoDraw>(entity =>
        {
            entity.HasKey(e => e.Draw);
            
            // Create indexes for common queries
            entity.HasIndex(e => e.Date)
                  .HasDatabaseName("IX_LottoDraws_Date");
                  
            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_LottoDraws_CreatedAt");
            
            // Configure decimal precision for prize fields
            entity.Property(e => e.Division1Prize)
                  .HasPrecision(18, 2);
            entity.Property(e => e.Division2Prize)
                  .HasPrecision(18, 2);
            entity.Property(e => e.Division3Prize)
                  .HasPrecision(18, 2);
            entity.Property(e => e.Division4Prize)
                  .HasPrecision(18, 2);
            entity.Property(e => e.Division5Prize)
                  .HasPrecision(18, 2);
            entity.Property(e => e.Division6Prize)
                  .HasPrecision(18, 2);
            entity.Property(e => e.Division7Prize)
                  .HasPrecision(18, 2);
        });
        
        // Configure NumberCombination entity
        modelBuilder.Entity<NumberCombination>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Create index for timestamp queries
            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_NumberCombinations_CreatedAt");
                  
            // Create composite index for duplicate detection
            entity.HasIndex(e => new { e.Number1, e.Number2, e.Number3, e.Number4, e.Number5, e.Number6 })
                  .HasDatabaseName("IX_NumberCombinations_Numbers");
        });
        
        // Configure Prediction entity
        modelBuilder.Entity<Prediction>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Create indexes for common queries
            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_Predictions_CreatedAt");
                  
            entity.HasIndex(e => e.Source)
                  .HasDatabaseName("IX_Predictions_Source");
                  
            // Create composite index for number queries (including Powerball)
            entity.HasIndex(e => new { e.Number1, e.Number2, e.Number3, e.Number4, e.Number5, e.Number6, e.Powerball })
                  .HasDatabaseName("IX_Predictions_Numbers_Powerball");
                  
            // Create index for matched predictions
            entity.HasIndex(e => e.IsMatched)
                  .HasDatabaseName("IX_Predictions_IsMatched");
                  
            entity.HasIndex(e => e.MatchedDrawId)
                  .HasDatabaseName("IX_Predictions_MatchedDrawId");
                  
            // Configure relationship with LottoDraw for matched predictions
            entity.HasOne<LottoDraw>()
                  .WithMany()
                  .HasForeignKey(e => e.MatchedDrawId)
                  .HasPrincipalKey(d => d.Draw)
                  .OnDelete(DeleteBehavior.SetNull);
        });
        
        // Configure PredictionScoreHistory entity
        modelBuilder.Entity<PredictionScoreHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Configure relationships
            entity.HasOne(e => e.Prediction)
                  .WithMany()
                  .HasForeignKey(e => e.PredictionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.TriggeringDraw)
                  .WithMany()
                  .HasForeignKey(e => e.TriggeringDrawId)
                  .HasPrincipalKey(d => d.Draw)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // Create indexes for common queries
            entity.HasIndex(e => e.PredictionId)
                  .HasDatabaseName("IX_PredictionScoreHistory_PredictionId");
                  
            entity.HasIndex(e => e.UpdatedAt)
                  .HasDatabaseName("IX_PredictionScoreHistory_UpdatedAt");
                  
            entity.HasIndex(e => e.TriggeringDrawId)
                  .HasDatabaseName("IX_PredictionScoreHistory_TriggeringDrawId");
        });
        
        // Configure ExternalServiceCallLog entity
        modelBuilder.Entity<ExternalServiceCallLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Create indexes for common queries
            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_ExternalServiceCallLogs_CreatedAt");
                  
            entity.HasIndex(e => e.ServiceName)
                  .HasDatabaseName("IX_ExternalServiceCallLogs_ServiceName");
                  
            entity.HasIndex(e => e.Success)
                  .HasDatabaseName("IX_ExternalServiceCallLogs_Success");
                  
            // Create composite index for service and date queries
            entity.HasIndex(e => new { e.ServiceName, e.CreatedAt })
                  .HasDatabaseName("IX_ExternalServiceCallLogs_ServiceName_CreatedAt");
        });
        
        // Configure NumberOccurrence entity
        modelBuilder.Entity<NumberOccurrence>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Configure the relationship between NumberOccurrence and LottoDraw
            entity.HasOne(e => e.Draw)
                  .WithMany()
                  .HasForeignKey(e => e.DrawNumber)
                  .HasPrincipalKey(d => d.Draw)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Create indexes for common queries
            entity.HasIndex(e => e.DrawNumber)
                  .HasDatabaseName("IX_NumberOccurrences_DrawNumber");
                  
            entity.HasIndex(e => e.Number)
                  .HasDatabaseName("IX_NumberOccurrences_Number");
                  
            entity.HasIndex(e => e.DrawDate)
                  .HasDatabaseName("IX_NumberOccurrences_DrawDate");
                  
            entity.HasIndex(e => new { e.Number, e.DrawDate })
                  .HasDatabaseName("IX_NumberOccurrences_Number_DrawDate");
        });
    }
    
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<LottoDraw>()
            .Where(e => e.State == EntityState.Modified);
            
        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}