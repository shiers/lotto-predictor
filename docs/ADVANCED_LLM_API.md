# Advanced LLM API Documentation

This document provides comprehensive API documentation for the advanced Large Language Model (LLM) features in PredictLottoNZ.

## Table of Contents

- [Overview](#overview)
- [Authentication](#authentication)
- [Enhanced Prediction Endpoints](#enhanced-prediction-endpoints)
- [Provider Management](#provider-management)
- [Model Retraining](#model-retraining)
- [Accuracy Analysis](#accuracy-analysis)
- [Local LLM Service](#local-llm-service)
- [Error Handling](#error-handling)
- [Rate Limiting](#rate-limiting)

## Overview

The Advanced LLM API extends the core PredictLottoNZ API with sophisticated AI-powered prediction capabilities, including:

- **Multi-Provider Architecture**: Amazon Bedrock AgentCore, Local LLM, FastAPI, and Frequency-based providers
- **Intelligent Fallback**: Automatic provider switching based on health and confidence
- **Continuous Learning**: Automated model retraining based on accuracy analysis
- **Confidence Scoring**: Reliability metrics for all predictions
- **Reasoning Explanations**: Detailed AI reasoning for prediction decisions

## Authentication

All API endpoints use the same authentication as the core API. For production deployments, ensure proper API key or JWT token authentication is configured.

```http
Authorization: Bearer <your-api-token>
Content-Type: application/json
```

## Enhanced Prediction Endpoints

### Generate Enhanced Predictions

Generate predictions with advanced LLM features including confidence scoring and reasoning.

**Endpoint:** `GET /api/predictions/enhanced`

**Parameters:**
- `count` (integer, 1-10): Number of predictions to generate
- `minConfidence` (decimal, 0.0-1.0, optional): Minimum confidence threshold
- `provider` (string, optional): Preferred provider (`BedrockAgentCore`, `LocalLLM`, `FastAPI`, `FrequencyBased`)
- `includeReasoning` (boolean, optional): Include detailed reasoning explanations
- `targetDate` (datetime, optional): Target draw date for predictions

**Example Request:**
```http
GET /api/predictions/enhanced?count=5&minConfidence=0.8&provider=BedrockAgentCore&includeReasoning=true
```

**Example Response:**
```json
{
  "predictions": [
    {
      "numbers": [5, 12, 18, 25, 33, 40],
      "source": "BedrockAgentCore (Claude-3-Sonnet)",
      "confidenceScore": 0.92,
      "reasoningExplanation": "Based on frequency analysis of the last 100 draws, these numbers show strong temporal correlation with a 23% increase in occurrence over the past month. The combination exhibits optimal distribution across number ranges with historical precedent in similar seasonal patterns.",
      "keyFactors": [
        "frequency_analysis",
        "temporal_correlation", 
        "seasonal_patterns",
        "range_distribution"
      ],
      "targetDrawDate": "2024-12-15T00:00:00Z",
      "createdAt": "2024-12-12T10:30:00Z",
      "providerMetadata": {
        "modelVersion": "claude-3-sonnet-20240229",
        "processingTimeMs": 1250,
        "requestId": "req_abc123"
      }
    }
  ],
  "metadata": {
    "totalRequested": 5,
    "totalGenerated": 5,
    "averageConfidence": 0.87,
    "providersUsed": ["BedrockAgentCore"],
    "processingTimeMs": 1250
  }
}
```

### Compare Predictions Across Providers

Get predictions from multiple providers for comparison and consensus analysis.

**Endpoint:** `POST /api/predictions/compare`

**Request Body:**
```json
{
  "count": 3,
  "providers": ["BedrockAgentCore", "LocalLLM", "FastAPI"],
  "includeConsensus": true,
  "minConfidence": 0.7
}
```

**Example Response:**
```json
{
  "providerPredictions": {
    "BedrockAgentCore": [
      {
        "numbers": [5, 12, 18, 25, 33, 40],
        "confidenceScore": 0.92,
        "reasoning": "Strong temporal correlation detected"
      }
    ],
    "LocalLLM": [
      {
        "numbers": [8, 15, 22, 29, 36, 39],
        "confidenceScore": 0.87,
        "reasoning": "Frequency-based optimization with pattern analysis"
      }
    ]
  },
  "consensus": {
    "commonNumbers": [12, 25, 33],
    "recommendedProvider": "BedrockAgentCore",
    "diversityScore": 0.73,
    "confidenceWeightedAverage": 0.89
  },
  "metadata": {
    "providersResponded": 2,
    "providersRequested": 3,
    "failedProviders": ["FastAPI"],
    "totalProcessingTimeMs": 2100
  }
}
```

## Provider Management

### Get Provider Health Status

Monitor the health and performance of all prediction providers.

**Endpoint:** `GET /api/predictions/providers/health`

**Example Response:**
```json
{
  "providers": {
    "BedrockAgentCore": {
      "isHealthy": true,
      "lastChecked": "2024-12-12T10:30:00Z",
      "successRate": 0.95,
      "averageResponseTimeMs": 1200,
      "lastSuccess": "2024-12-12T10:29:45Z",
      "lastFailure": null,
      "consecutiveFailures": 0,
      "circuitBreakerOpen": false,
      "supportsConfidenceScores": true,
      "supportsReasoningChains": true
    },
    "LocalLLM": {
      "isHealthy": true,
      "lastChecked": "2024-12-12T10:30:00Z",
      "successRate": 0.87,
      "averageResponseTimeMs": 800,
      "lastSuccess": "2024-12-12T10:28:30Z",
      "lastFailure": "2024-12-12T09:15:22Z",
      "consecutiveFailures": 0,
      "circuitBreakerOpen": false,
      "modelVersion": "lotto-predictor-v2.1",
      "gpuEnabled": false,
      "memoryUsagePercent": 45
    },
    "FastAPI": {
      "isHealthy": false,
      "lastChecked": "2024-12-12T10:30:00Z",
      "successRate": 0.23,
      "averageResponseTimeMs": 5000,
      "lastSuccess": "2024-12-12T08:45:12Z",
      "lastFailure": "2024-12-12T10:25:33Z",
      "consecutiveFailures": 5,
      "circuitBreakerOpen": true,
      "lastError": "Connection timeout after 30 seconds"
    }
  },
  "summary": {
    "totalProviders": 4,
    "healthyProviders": 3,
    "recommendedProvider": "BedrockAgentCore",
    "overallSystemHealth": "healthy"
  }
}
```

### Configure Provider Settings

Update provider-specific configuration and thresholds.

**Endpoint:** `PUT /api/predictions/providers/{providerName}/config`

**Request Body:**
```json
{
  "enabled": true,
  "priority": 1,
  "confidenceThreshold": 0.75,
  "timeoutSeconds": 30,
  "maxRetries": 3,
  "circuitBreakerThreshold": 5,
  "healthCheckIntervalSeconds": 60
}
```

## Model Retraining

### Trigger Model Retraining

Initiate retraining for one or more LLM providers.

**Endpoint:** `POST /api/training/retrain`

**Request Body:**
```json
{
  "trigger": "manual",
  "providers": ["LocalLLM", "BedrockAgentCore"],
  "trainingParameters": {
    "epochs": 10,
    "learningRate": 0.001,
    "batchSize": 32,
    "validationSplit": 0.2
  },
  "dataFilters": {
    "fromDate": "2024-01-01T00:00:00Z",
    "minAccuracy": 0.1,
    "includeProviders": ["all"]
  }
}
```

**Example Response:**
```json
{
  "retrainingJobs": [
    {
      "jobId": "retrain_local_20241212_103000",
      "provider": "LocalLLM",
      "status": "initiated",
      "estimatedDurationMinutes": 45,
      "trainingDataSamples": 1250,
      "startedAt": "2024-12-12T10:30:00Z"
    },
    {
      "jobId": "retrain_bedrock_20241212_103001", 
      "provider": "BedrockAgentCore",
      "status": "initiated",
      "estimatedDurationMinutes": 120,
      "trainingDataSamples": 1250,
      "startedAt": "2024-12-12T10:30:01Z"
    }
  ],
  "metadata": {
    "totalJobsStarted": 2,
    "totalTrainingData": 1250,
    "expectedCompletionTime": "2024-12-12T12:30:00Z"
  }
}
```

### Get Retraining Status

Monitor the progress of active retraining jobs.

**Endpoint:** `GET /api/training/status`

**Query Parameters:**
- `jobId` (string, optional): Specific job ID to check
- `provider` (string, optional): Filter by provider

**Example Response:**
```json
{
  "activeJobs": [
    {
      "jobId": "retrain_local_20241212_103000",
      "provider": "LocalLLM", 
      "status": "training",
      "progress": 65,
      "currentEpoch": 7,
      "totalEpochs": 10,
      "currentLoss": 0.23,
      "validationAccuracy": 0.78,
      "estimatedTimeRemaining": "15 minutes",
      "startedAt": "2024-12-12T10:30:00Z"
    }
  ],
  "recentCompletedJobs": [
    {
      "jobId": "retrain_bedrock_20241212_083000",
      "provider": "BedrockAgentCore",
      "status": "completed",
      "finalAccuracy": 0.82,
      "improvementPercent": 8.5,
      "completedAt": "2024-12-12T09:45:00Z",
      "newModelId": "bedrock-lotto-v2.3"
    }
  ]
}
```

### Get Training Data Summary

View available training data for model retraining.

**Endpoint:** `GET /api/training/data/summary`

**Example Response:**
```json
{
  "trainingDataSummary": {
    "totalDraws": 1250,
    "totalPredictions": 3400,
    "accuracyRecords": 3400,
    "dateRange": {
      "earliest": "2020-01-01T00:00:00Z",
      "latest": "2024-12-12T00:00:00Z"
    },
    "providerBreakdown": {
      "BedrockAgentCore": 850,
      "LocalLLM": 920,
      "FastAPI": 1200,
      "FrequencyBased": 430
    },
    "accuracyDistribution": {
      "exactMatches": 0,
      "fourMatches": 45,
      "threeMatches": 340,
      "twoMatches": 1200,
      "oneMatch": 1815
    }
  },
  "dataQuality": {
    "completenessScore": 0.95,
    "consistencyScore": 0.98,
    "recentDataPercent": 0.25
  }
}
```

## Accuracy Analysis

### Get Comprehensive Accuracy Analysis

Analyze prediction accuracy across providers and time periods.

**Endpoint:** `GET /api/accuracy/analysis`

**Query Parameters:**
- `fromDate` (datetime, optional): Start date for analysis
- `toDate` (datetime, optional): End date for analysis  
- `provider` (string, optional): Specific provider or "all"
- `groupBy` (string, optional): Group results by "day", "week", "month"

**Example Response:**
```json
{
  "overallMetrics": {
    "totalPredictions": 3400,
    "exactMatches": 0,
    "partialMatches": 1585,
    "averageAccuracy": 0.156,
    "improvementTrend": 0.023,
    "bestPerformingProvider": "BedrockAgentCore"
  },
  "providerMetrics": {
    "BedrockAgentCore": {
      "totalPredictions": 850,
      "exactMatches": 0,
      "partialMatches": 165,
      "averageAccuracy": 0.194,
      "confidenceCalibration": 0.87,
      "averageConfidence": 0.89,
      "trend": "improving"
    },
    "LocalLLM": {
      "totalPredictions": 920,
      "exactMatches": 0,
      "partialMatches": 142,
      "averageAccuracy": 0.154,
      "confidenceCalibration": 0.82,
      "averageConfidence": 0.81,
      "trend": "stable"
    }
  },
  "temporalAnalysis": {
    "weeklyTrends": [
      {
        "week": "2024-12-02",
        "accuracy": 0.145,
        "predictions": 85
      },
      {
        "week": "2024-12-09", 
        "accuracy": 0.167,
        "predictions": 92
      }
    ],
    "seasonalPatterns": {
      "bestMonth": "March",
      "worstMonth": "August",
      "seasonalVariation": 0.034
    }
  },
  "recommendations": [
    "Increase confidence threshold for LocalLLM to 0.85",
    "Consider retraining BedrockAgentCore with recent seasonal data",
    "FastAPI provider showing consistent underperformance - investigate"
  ]
}
```

### Get Prediction Accuracy for Specific Draw

Analyze how well providers predicted a specific lottery draw.

**Endpoint:** `GET /api/accuracy/draw/{drawNumber}`

**Example Response:**
```json
{
  "drawInfo": {
    "drawNumber": 1999,
    "drawDate": "2024-12-07T00:00:00Z",
    "winningNumbers": [7, 14, 21, 28, 35, 42],
    "bonusNumber": 10,
    "powerball": 5
  },
  "predictions": [
    {
      "provider": "BedrockAgentCore",
      "predictedNumbers": [5, 14, 18, 28, 33, 42],
      "matches": 3,
      "matchingNumbers": [14, 28, 42],
      "accuracy": 0.5,
      "confidence": 0.92,
      "proximityScore": 0.73
    },
    {
      "provider": "LocalLLM",
      "predictedNumbers": [8, 15, 22, 29, 36, 39],
      "matches": 0,
      "matchingNumbers": [],
      "accuracy": 0.0,
      "confidence": 0.87,
      "proximityScore": 0.45
    }
  ],
  "summary": {
    "bestProvider": "BedrockAgentCore",
    "totalPredictions": 4,
    "averageAccuracy": 0.25,
    "highestConfidence": 0.92
  }
}
```

## Local LLM Service

### Get Model Information

Retrieve information about the currently loaded model.

**Endpoint:** `GET /api/local-llm/model/info`

**Example Response:**
```json
{
  "modelInfo": {
    "modelName": "lotto-predictor-v2.1",
    "modelPath": "/models/lotto-predictor-v2.1",
    "architecture": "transformer",
    "parameters": 1200000000,
    "loadedAt": "2024-12-12T08:00:00Z",
    "version": "2.1.0",
    "trainingData": {
      "samples": 15000,
      "lastTrainingDate": "2024-12-10T00:00:00Z"
    }
  },
  "systemInfo": {
    "gpuEnabled": false,
    "memoryUsageGB": 3.2,
    "maxMemoryGB": 8.0,
    "cpuCores": 8,
    "modelLoadTimeSeconds": 45
  },
  "performance": {
    "averageInferenceTimeMs": 800,
    "requestsProcessed": 1250,
    "uptime": "4 hours 30 minutes"
  }
}
```

### Initiate Model Hot-Swap

Perform zero-downtime model updates.

**Endpoint:** `POST /api/local-llm/hot-swap`

**Request Body:**
```json
{
  "newModelPath": "/models/lotto-predictor-v2.2",
  "newModelName": "lotto-predictor-v2.2",
  "validationRequired": true,
  "rollbackOnFailure": true,
  "testPredictions": 5,
  "maxSwapTimeMinutes": 10
}
```

**Example Response:**
```json
{
  "swapId": "swap_20241212_103000",
  "status": "initiated",
  "currentModel": "lotto-predictor-v2.1",
  "targetModel": "lotto-predictor-v2.2",
  "estimatedDuration": "5 minutes",
  "validationTests": 5,
  "rollbackEnabled": true,
  "startedAt": "2024-12-12T10:30:00Z"
}
```

### Check Hot-Swap Status

Monitor the progress of a model hot-swap operation.

**Endpoint:** `GET /api/local-llm/hot-swap/status/{swapId}`

**Example Response:**
```json
{
  "swapId": "swap_20241212_103000",
  "status": "validating",
  "progress": 75,
  "currentPhase": "Running validation tests",
  "phasesCompleted": [
    "Model loading",
    "Compatibility check",
    "Memory allocation"
  ],
  "validationResults": {
    "testsCompleted": 4,
    "testsTotal": 5,
    "allTestsPassed": true,
    "averageResponseTime": 750
  },
  "rollbackAvailable": true,
  "estimatedTimeRemaining": "1 minute"
}
```

### Rollback Model

Rollback to the previous model version.

**Endpoint:** `POST /api/local-llm/hot-swap/rollback`

**Request Body:**
```json
{
  "swapId": "swap_20241212_103000",
  "reason": "Performance degradation detected"
}
```

## Error Handling

### Standard Error Response Format

All API endpoints return errors in a consistent format:

```json
{
  "error": {
    "code": "PROVIDER_UNAVAILABLE",
    "message": "BedrockAgentCore provider is currently unavailable",
    "details": {
      "provider": "BedrockAgentCore",
      "lastError": "AWS credentials not configured",
      "suggestedAction": "Configure AWS credentials and retry"
    },
    "timestamp": "2024-12-12T10:30:00Z",
    "requestId": "req_abc123"
  }
}
```

### Common Error Codes

| Code | Description | HTTP Status |
|------|-------------|-------------|
| `PROVIDER_UNAVAILABLE` | Requested provider is not available | 503 |
| `CONFIDENCE_TOO_LOW` | No predictions meet minimum confidence threshold | 422 |
| `INVALID_PROVIDER` | Specified provider does not exist | 400 |
| `RETRAINING_IN_PROGRESS` | Model retraining is already in progress | 409 |
| `MODEL_SWAP_FAILED` | Hot-swap operation failed | 500 |
| `INSUFFICIENT_TRAINING_DATA` | Not enough data for model retraining | 422 |
| `AWS_CREDENTIALS_INVALID` | AWS credentials are invalid or expired | 401 |
| `MODEL_NOT_FOUND` | Specified model file not found | 404 |

## Rate Limiting

### Rate Limits by Endpoint Category

| Category | Requests per Minute | Burst Limit |
|----------|-------------------|-------------|
| Enhanced Predictions | 60 | 10 |
| Provider Health | 120 | 20 |
| Model Retraining | 5 | 2 |
| Accuracy Analysis | 30 | 5 |
| Local LLM Management | 20 | 5 |

### Rate Limit Headers

All responses include rate limiting information:

```http
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1702377600
X-RateLimit-Category: enhanced-predictions
```

### Rate Limit Exceeded Response

```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded for enhanced predictions",
    "details": {
      "limit": 60,
      "resetTime": "2024-12-12T10:35:00Z",
      "retryAfter": 300
    }
  }
}
```

## SDK and Client Libraries

### JavaScript/TypeScript Client

```typescript
import { AdvancedLLMClient } from '@predict-lotto-nz/client';

const client = new AdvancedLLMClient({
  baseUrl: 'http://localhost:5000',
  apiKey: 'your-api-key'
});

// Generate enhanced predictions
const predictions = await client.predictions.enhanced({
  count: 5,
  minConfidence: 0.8,
  provider: 'BedrockAgentCore'
});

// Check provider health
const health = await client.providers.health();

// Trigger retraining
const retraining = await client.training.retrain({
  providers: ['LocalLLM'],
  trigger: 'manual'
});
```

### Python Client

```python
from predict_lotto_nz import AdvancedLLMClient

client = AdvancedLLMClient(
    base_url="http://localhost:5000",
    api_key="your-api-key"
)

# Generate enhanced predictions
predictions = client.predictions.enhanced(
    count=5,
    min_confidence=0.8,
    provider="BedrockAgentCore"
)

# Check provider health
health = client.providers.health()

# Trigger retraining
retraining = client.training.retrain(
    providers=["LocalLLM"],
    trigger="manual"
)
```

## Advanced Configuration and Deployment

### Environment Variables for Production

#### Amazon Bedrock AgentCore Configuration
```env
# AWS Authentication
AWS_REGION=us-west-2
AWS_ACCESS_KEY_ID=your_access_key_id
AWS_SECRET_ACCESS_KEY=your_secret_access_key

# Bedrock Model Configuration
BEDROCK_MODEL_ID=anthropic.claude-3-sonnet-20240229-v1:0
BEDROCK_FALLBACK_MODEL_ID=anthropic.claude-3-haiku-20240307-v1:0
BEDROCK_TIMEOUT_SECONDS=30
BEDROCK_MAX_RETRIES=3
BEDROCK_RETRY_DELAY_MS=1000

# Advanced Bedrock Settings
BEDROCK_MAX_TOKENS=4096
BEDROCK_TEMPERATURE=0.7
BEDROCK_TOP_P=0.9
BEDROCK_ENABLE_STREAMING=true
BEDROCK_COST_THRESHOLD_USD=100.00
BEDROCK_ENABLE_CACHING=true
BEDROCK_CACHE_TTL_MINUTES=60

# Security and Compliance
BEDROCK_ENABLE_LOGGING=true
BEDROCK_LOG_LEVEL=INFO
BEDROCK_ENCRYPT_PAYLOADS=true
BEDROCK_VPC_ENDPOINT_ID=vpce-12345678
```

#### Local LLM Service Configuration
```env
# Service Settings
LOCAL_LLM_BASE_URL=http://localhost:8002
LOCAL_LLM_SERVICE_PORT=8002
LOCAL_LLM_WORKERS=1
LOCAL_LLM_TIMEOUT_SECONDS=60

# Model Configuration
LOCAL_LLM_MODEL_PATH=/models/fine-tuned/lotto-predictor-v2.0
LOCAL_LLM_MODEL_NAME=lotto-predictor-v2.0
LOCAL_LLM_FALLBACK_MODEL_PATH=/models/fine-tuned/lotto-predictor-v1.1

# Hardware Configuration
LOCAL_LLM_GPU_ENABLED=false
LOCAL_LLM_GPU_DEVICE=0
LOCAL_LLM_GPU_MEMORY_FRACTION=0.8
LOCAL_LLM_MAX_MEMORY_GB=8

# Performance Settings
LOCAL_LLM_BATCH_SIZE=1
LOCAL_LLM_MAX_SEQUENCE_LENGTH=2048
LOCAL_LLM_ENABLE_CACHING=true
LOCAL_LLM_QUANTIZATION=none

# Hot-Swapping Configuration
LOCAL_LLM_ENABLE_HOT_SWAP=true
LOCAL_LLM_SWAP_VALIDATION_SAMPLES=5
LOCAL_LLM_SWAP_TIMEOUT_MINUTES=10
LOCAL_LLM_ENABLE_ROLLBACK=true
```

#### Retraining Pipeline Configuration
```env
# Retraining Settings
RETRAINING_ENABLED=true
RETRAINING_ACCURACY_THRESHOLD=0.85
RETRAINING_MIN_DATA_POINTS=50
RETRAINING_SCHEDULE_CRON="0 2 * * 0"
RETRAINING_MAX_MODEL_AGE_DAYS=90
RETRAINING_PARALLEL_PROVIDERS=true
RETRAINING_AUTO_DEPLOY=false

# Training Parameters
TRAINING_GPU_ENABLED=true
TRAINING_MAX_EPOCHS=20
TRAINING_EARLY_STOPPING_PATIENCE=5
TRAINING_LEARNING_RATE=2e-5
TRAINING_BATCH_SIZE=4
```

### Deployment Architecture

#### Production Docker Compose Configuration
```yaml
version: '3.8'
services:
  backend:
    build: ./backend
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DATABASE_URL}
      - AWS_REGION=${AWS_REGION}
      - BEDROCK_MODEL_ID=${BEDROCK_MODEL_ID}
      - LOCAL_LLM_BASE_URL=${LOCAL_LLM_BASE_URL}
    depends_on:
      - postgres
      - local-llm
    restart: unless-stopped

  local-llm:
    build:
      context: ./predictor
      dockerfile: Dockerfile.llm
    ports:
      - "8002:8002"
      - "8003:8003"  # Metrics port
    volumes:
      - ./models:/models:ro
      - llm-cache:/cache
    environment:
      - LOCAL_LLM_GPU_ENABLED=${LOCAL_LLM_GPU_ENABLED}
      - LOCAL_LLM_MODEL_PATH=${LOCAL_LLM_MODEL_PATH}
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
    restart: unless-stopped

  training-pipeline:
    build:
      context: ./backend
      dockerfile: Dockerfile.training
    volumes:
      - ./models:/models
      - ./training-data:/training-data
    environment:
      - RETRAINING_ENABLED=${RETRAINING_ENABLED}
      - TRAINING_GPU_ENABLED=${TRAINING_GPU_ENABLED}
    depends_on:
      - postgres
      - redis
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    volumes:
      - redis-data:/data
    restart: unless-stopped

volumes:
  llm-cache:
  redis-data:
```

### Monitoring and Observability

#### Prometheus Metrics Configuration
```yaml
# prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'predict-lotto-backend'
    static_configs:
      - targets: ['backend:5000']
    metrics_path: '/metrics'
    
  - job_name: 'local-llm-service'
    static_configs:
      - targets: ['local-llm:8003']
    metrics_path: '/metrics'
    
  - job_name: 'training-pipeline'
    static_configs:
      - targets: ['training-pipeline:9090']
    metrics_path: '/metrics'
```

#### Custom Metrics Exposed
```
# Provider Health Metrics
provider_health_status{provider="BedrockAgentCore"} 1
provider_response_time_seconds{provider="LocalLLM"} 0.85
provider_success_rate{provider="FastAPI"} 0.87

# Prediction Metrics
prediction_requests_total{provider="BedrockAgentCore"} 1250
prediction_accuracy{provider="LocalLLM"} 0.194
prediction_confidence_score{provider="BedrockAgentCore"} 0.89

# Training Metrics
training_job_duration_seconds{provider="LocalLLM"} 2700
training_accuracy{job_id="retrain_local_20241212"} 0.82
model_version_active{provider="LocalLLM",version="v2.1"} 1
```

## Webhooks and Notifications

### Retraining Completion Webhook

Configure webhooks to receive notifications when model retraining completes:

**Webhook Configuration:**
```env
RETRAINING_WEBHOOK_URL=https://hooks.slack.com/services/your/webhook/url
RETRAINING_WEBHOOK_SECRET=your_webhook_secret
RETRAINING_WEBHOOK_EVENTS=completed,failed,started
```

**Webhook Payload:**
```json
{
  "event": "retraining.completed",
  "timestamp": "2024-12-12T12:30:00Z",
  "signature": "sha256=abc123...",
  "data": {
    "jobId": "retrain_local_20241212_103000",
    "provider": "LocalLLM",
    "status": "completed",
    "finalAccuracy": 0.82,
    "improvementPercent": 8.5,
    "newModelVersion": "lotto-predictor-v2.2",
    "trainingDurationMinutes": 45,
    "validationResults": {
      "testAccuracy": 0.78,
      "confidenceCalibration": 0.85,
      "performanceImprovement": true
    }
  }
}
```

### Provider Health Change Webhook

Receive notifications when provider health status changes:

**Webhook Payload:**
```json
{
  "event": "provider.health_changed",
  "timestamp": "2024-12-12T10:30:00Z",
  "signature": "sha256=def456...",
  "data": {
    "provider": "FastAPI",
    "previousStatus": "healthy",
    "currentStatus": "unhealthy",
    "reason": "Circuit breaker opened due to consecutive failures",
    "consecutiveFailures": 5,
    "lastSuccessfulRequest": "2024-12-12T09:45:00Z",
    "recommendedAction": "Check FastAPI service logs and restart if necessary"
  }
}
```

### Accuracy Alert Webhook

Get notified when prediction accuracy drops below thresholds:

**Webhook Payload:**
```json
{
  "event": "accuracy.threshold_breached",
  "timestamp": "2024-12-12T14:30:00Z",
  "signature": "sha256=ghi789...",
  "data": {
    "provider": "BedrockAgentCore",
    "currentAccuracy": 0.12,
    "thresholdAccuracy": 0.15,
    "accuracyDrop": 0.03,
    "timeframe": "last_7_days",
    "totalPredictions": 156,
    "recommendedActions": [
      "Consider model retraining",
      "Review recent prediction patterns",
      "Check for data quality issues"
    ]
  }
}
```

### Model Hot-Swap Notification

Receive updates on model hot-swap operations:

**Webhook Payload:**
```json
{
  "event": "model.hot_swap_completed",
  "timestamp": "2024-12-12T16:30:00Z",
  "signature": "sha256=jkl012...",
  "data": {
    "swapId": "swap_20241212_163000",
    "provider": "LocalLLM",
    "previousModel": "lotto-predictor-v2.1",
    "newModel": "lotto-predictor-v2.2",
    "swapDurationMinutes": 8,
    "validationResults": {
      "testsRun": 5,
      "testsPassed": 5,
      "averageResponseTime": 750,
      "accuracyImprovement": 0.05
    },
    "rollbackAvailable": true,
    "status": "success"
  }
}
```