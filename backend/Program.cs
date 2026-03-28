using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Services;
using System.Text.Json.Serialization;
using StackExchange.Redis;

// Load .env file from parent directory
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}
else
{
    // Try current directory as fallback
    DotNetEnv.Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Configure global model validation
    options.SuppressAsyncSuffixInActionNames = false;
})
.AddJsonOptions(options =>
{
    // Configure JSON serialization
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "PredictLottoNZ API", 
        Version = "v1",
        Description = "API for New Zealand Lotto prediction and analysis system",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "PredictLottoNZ Team",
            Email = "support@predictlottonz.com"
        }
    });
    
    // Enable XML documentation comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, true);
    }
    
    // Add security definitions if needed
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

// Configure Entity Framework with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<LottoDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// Register database services
builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
builder.Services.AddScoped<IDatabaseSeedingService, DatabaseSeedingService>();

// Register CSV parsing and import services
builder.Services.AddScoped<ICsvParsingService, CsvParsingService>();
builder.Services.AddScoped<ILottoImportService, LottoImportService>();

// Register query services
builder.Services.AddScoped<ILottoQueryService, LottoQueryService>();

// Register file parsing and combination services
builder.Services.AddScoped<IFileParsingService, FileParsingService>();
builder.Services.AddScoped<ICombinationService, CombinationService>();

// Register frequency calculation and prediction services
builder.Services.AddMemoryCache(); // Add memory cache for frequency caching
builder.Services.AddScoped<IFrequencyCalculationService, FrequencyCalculationService>();

// Configure Redis for distributed caching
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "PredictLottoNZ";
});

// Register Redis connection multiplexer
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(provider =>
{
    return StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);
});

// Register lookup and navigation services
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
builder.Services.AddScoped<ICacheWarmupService, CacheWarmupService>();
builder.Services.AddScoped<IPerformanceMonitoringService, PerformanceMonitoringService>();
builder.Services.AddScoped<INumberLookupService, NumberLookupService>();
builder.Services.AddScoped<IFrequencyAnalysisService, FrequencyAnalysisService>();
builder.Services.AddScoped<IDrawNavigationService, DrawNavigationService>();
builder.Services.AddScoped<IBookmarkService, BookmarkService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IAutoCompletionService, AutoCompletionService>();

// Register prediction providers in priority order
builder.Services.AddScoped<IPredictionProvider, AwsLlmPredictionProvider>();

// Register GroqCloud prediction provider
builder.Services.AddHttpClient<GroqCloudPredictionProvider>();
builder.Services.Configure<GroqCloudPredictionProviderOptions>(options =>
{
    options.ApiKey = Environment.GetEnvironmentVariable("GROQCLOUD_API_KEY") ?? "";
    options.Model = Environment.GetEnvironmentVariable("GROQCLOUD_MODEL") ?? "llama-3.3-70b-versatile";
    options.BaseUrl = Environment.GetEnvironmentVariable("GROQCLOUD_BASE_URL") ?? "https://api.groq.com/openai/v1";
    options.Temperature = double.TryParse(Environment.GetEnvironmentVariable("GROQCLOUD_TEMPERATURE"), out var temp) ? temp : 0.7;
    options.MaxTokens = int.TryParse(Environment.GetEnvironmentVariable("GROQCLOUD_MAX_TOKENS"), out var tokens) ? tokens : 4096;
    options.TimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("GROQCLOUD_TIMEOUT_SECONDS"), out var timeout) ? timeout : 30;
    options.MaxRetries = int.TryParse(Environment.GetEnvironmentVariable("GROQCLOUD_MAX_RETRIES"), out var retries) ? retries : 3;
});
builder.Services.AddScoped<IPredictionProvider, GroqCloudPredictionProvider>();

builder.Services.AddHttpClient<FastApiPredictionProvider>(); // Register HttpClient for FastAPI provider
builder.Services.Configure<FastApiPredictionProviderOptions>(options =>
{
    options.BaseUrl = Environment.GetEnvironmentVariable("FASTAPI_BASE_URL") ?? "http://localhost:8001";
    options.TimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("FASTAPI_TIMEOUT_SECONDS"), out var timeout) ? timeout : 30;
    options.MaxRetries = int.TryParse(Environment.GetEnvironmentVariable("FASTAPI_MAX_RETRIES"), out var retries) ? retries : 3;
    options.RetryDelayMs = int.TryParse(Environment.GetEnvironmentVariable("FASTAPI_RETRY_DELAY_MS"), out var delay) ? delay : 1000;
});
builder.Services.AddScoped<IPredictionProvider, FastApiPredictionProvider>();
builder.Services.AddScoped<IPredictionProvider, FrequencyPredictionProvider>();

// Register main prediction service
builder.Services.AddScoped<IPredictionService, PredictionService>();

// Register training data service for comprehensive data preservation
builder.Services.AddScoped<ITrainingDataService, TrainingDataService>();

// Register accuracy analysis and score update services
builder.Services.AddScoped<IAccuracyAnalysisService, AccuracyAnalysisService>();
builder.Services.AddScoped<IPredictionScoreUpdateService, PredictionScoreUpdateService>();

// Register prediction matching service
builder.Services.AddScoped<IPredictionMatchingService, PredictionMatchingService>();

// Configure CORS for frontend integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow all origins for easier testing
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            var allowedOrigins = new List<string>
            {
                "http://localhost:3000",
                "http://localhost:8080",
                "http://localhost:5173", // Vite default port
                "https://localhost:3000",
                "https://localhost:8080",
                "https://localhost:5173"
            };

            // Add environment-specific frontend URL
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
            if (!string.IsNullOrEmpty(frontendUrl))
            {
                allowedOrigins.Add(frontendUrl);
            }

            policy.WithOrigins(allowedOrigins.ToArray())
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    });
});

// Configure request size limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

// Configure Kestrel server options
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Enable Swagger only in development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PredictLottoNZ API v1");
        c.RoutePrefix = "swagger"; // Access via /swagger
        c.DocumentTitle = "PredictLottoNZ API Documentation";
        c.DefaultModelsExpandDepth(-1); // Hide models section by default
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
        
        // Custom CSS for better appearance
        c.InjectStylesheet("/swagger-ui/custom.css");
    });
}

// Add request logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var startTime = DateTime.UtcNow;
    
    logger.LogInformation("Request {Method} {Path} started at {StartTime}", 
        context.Request.Method, context.Request.Path, startTime);
    
    try
    {
        await next();
        
        var duration = DateTime.UtcNow - startTime;
        logger.LogInformation("Request {Method} {Path} completed in {Duration}ms with status {StatusCode}", 
            context.Request.Method, context.Request.Path, duration.TotalMilliseconds, context.Response.StatusCode);
    }
    catch (Exception ex)
    {
        var duration = DateTime.UtcNow - startTime;
        logger.LogError(ex, "Request {Method} {Path} failed after {Duration}ms", 
            context.Request.Method, context.Request.Path, duration.TotalMilliseconds);
        throw;
    }
});

// Add global error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception occurred");
        
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "An internal server error occurred",
            requestId = context.TraceIdentifier
        };
        
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable static files for custom Swagger CSS
app.UseStaticFiles();

app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Add root endpoint - redirect to Swagger in development, return API info in production
app.MapGet("/", (IWebHostEnvironment env) => 
{
    if (env.IsDevelopment())
    {
        return Results.Redirect("/swagger");
    }
    else
    {
        return Results.Ok(new 
        { 
            name = "PredictLottoNZ API",
            version = "v1",
            status = "running",
            environment = env.EnvironmentName,
            timestamp = DateTime.UtcNow
        });
    }
});

// Initialize database with retry logic
using (var scope = app.Services.CreateScope())
{
    var databaseInitService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
    await databaseInitService.InitializeAsync();
    
    // Seed database in development environment
    if (app.Environment.IsDevelopment())
    {
        var seedingService = scope.ServiceProvider.GetRequiredService<IDatabaseSeedingService>();
        await seedingService.SeedAsync();
    }
}

app.Run();