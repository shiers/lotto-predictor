# GroqCloud API Documentation

This document provides comprehensive API documentation for integrating GroqCloud's high-performance inference platform with PredictLottoNZ.

## Table of Contents

- [Overview](#overview)
- [Authentication](#authentication)
- [GroqCloud Prediction Endpoints](#groqcloud-prediction-endpoints)
- [Model Management](#model-management)
- [Performance Optimization](#performance-optimization)
- [Monitoring and Analytics](#monitoring-and-analytics)
- [Error Handling](#error-handling)
- [Rate Limiting](#rate-limiting)

## Overview

GroqCloud integration provides ultra-fast AI inference capabilities with:

- **Lightning-Fast Inference**: Hardware-accelerated inference with sub-second response times
- **LangChain Integration**: Seamless integration using `langchain_groq` package
- **Multiple Model Support**: Access to Llama 3.1, Mixtral, Gemma, and other state-of-the-art models
- **Advanced Prompting**: LangChain's prompt templates, chains, and agents
- **Memory Management**: Conversation memory and context handling
- **Cost-Effective Scaling**: Pay-per-use pricing with no infrastructure overhead
- **High Availability**: 99.9% uptime SLA with global edge deployment
- **Real-time Streaming**: Support for streaming responses and real-time predictions

## Authentication

GroqCloud uses API key authentication. All requests must include your API key in the Authorization header.

```http
Authorization: Bearer gsk_your_groqcloud_api_key_here
Content-Type: application/json
```

### API Key Management

```bash
# Set API key as environment variable
export GROQCLOUD_API_KEY="gsk_your_groqcloud_api_key_here"

# Verify API key validity
curl -X GET "https://api.groq.com/openai/v1/models" \
  -H "Authorization: Bearer $GROQCLOUD_API_KEY"
```

## GroqCloud Prediction Endpoints

### Generate GroqCloud-Powered Predictions

Generate lottery predictions using GroqCloud's high-performance inference.

**Endpoint:** `GET /api/predictions/groqcloud`

**Parameters:**
- `count` (integer, 1-10): Number of predictions to generate
- `model` (string, optional): Specific GroqCloud model (`llama-3.1-70b-versatile`, `mixtral-8x7b-32768`, `gemma2-9b-it`)
- `temperature` (decimal, 0.0-2.0, optional): Creativity level for predictions
- `maxTokens` (integer, optional): Maximum tokens for response generation
- `includeReasoning` (boolean, optional): Include detailed AI reasoning
- `streamResponse` (boolean, optional): Enable streaming response
- `seed` (integer, optional): Seed for reproducible results

**Example Request:**
```http
GET /api/predictions/groqcloud?count=3&model=llama-3.1-70b-versatile&temperature=0.7&includeReasoning=true&streamResponse=false
```

**Example Response:**
```json
{
  "predictions": [
    {
      "numbers": [8, 15, 23, 31, 37, 44],
      "source": "GroqCloud (Llama-3.1-70B-Versatile)",
      "confidenceScore": 0.89,
      "reasoningExplanation": "Analysis of 500+ historical draws reveals strong correlation patterns. Numbers 8, 15, and 31 show 34% higher frequency in December draws. The combination balances high/low distribution (3:3) and maintains optimal sum range (158) based on statistical modeling.",
      "keyFactors": [
        "seasonal_frequency_boost",
        "balanced_distribution",
        "optimal_sum_range",
        "historical_correlation"
      ],
      "targetDrawDate": "2024-12-15T00:00:00Z",
      "createdAt": "2024-12-12T10:30:00Z",
      "providerMetadata": {
        "modelName": "llama-3.1-70b-versatile",
        "inferenceTimeMs": 245,
        "tokensGenerated": 156,
        "requestId": "groq_req_abc123",
        "processingNode": "groq-us-west-1"
      }
    }
  ],
  "metadata": {
    "totalRequested": 3,
    "totalGenerated": 3,
    "averageConfidence": 0.87,
    "totalInferenceTimeMs": 720,
    "tokensUsed": 468,
    "costEstimateUSD": 0.0023
  }
}
```

### Streaming Predictions

Get real-time streaming predictions for immediate feedback.

**Endpoint:** `GET /api/predictions/groqcloud/stream`

**Parameters:**
- Same as standard predictions endpoint
- `streamResponse` is automatically set to `true`

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/predictions/groqcloud/stream?count=1&model=llama-3.1-70b-versatile" \
  -H "Accept: text/event-stream"
```

**Example Streaming Response:**
```
data: {"type": "start", "requestId": "groq_stream_123", "model": "llama-3.1-70b-versatile"}

data: {"type": "reasoning", "content": "Analyzing historical patterns for December draws..."}

data: {"type": "reasoning", "content": "Frequency analysis shows numbers 8, 15, 31 trending upward..."}

data: {"type": "numbers", "content": [8, 15, 23, 31, 37, 44]}

data: {"type": "confidence", "content": 0.89}

data: {"type": "complete", "prediction": {...}, "metadata": {...}}
```

### Batch Predictions

Generate multiple prediction sets efficiently with batch processing.

**Endpoint:** `POST /api/predictions/groqcloud/batch`

**Request Body:**
```json
{
  "requests": [
    {
      "id": "batch_1",
      "count": 5,
      "model": "llama-3.1-70b-versatile",
      "temperature": 0.7,
      "includeReasoning": true
    },
    {
      "id": "batch_2", 
      "count": 3,
      "model": "mixtral-8x7b-32768",
      "temperature": 0.5,
      "includeReasoning": false
    }
  ],
  "parallel": true,
  "maxConcurrency": 5
}
```

**Example Response:**
```json
{
  "batchId": "batch_20241212_103000",
  "status": "completed",
  "results": [
    {
      "id": "batch_1",
      "status": "success",
      "predictions": [...],
      "processingTimeMs": 1200
    },
    {
      "id": "batch_2",
      "status": "success", 
      "predictions": [...],
      "processingTimeMs": 800
    }
  ],
  "summary": {
    "totalRequests": 2,
    "successfulRequests": 2,
    "failedRequests": 0,
    "totalPredictions": 8,
    "totalProcessingTimeMs": 2000,
    "totalCostUSD": 0.0156
  }
}
```

### Model Comparison

Compare predictions across different GroqCloud models.

**Endpoint:** `POST /api/predictions/groqcloud/compare`

**Request Body:**
```json
{
  "count": 3,
  "models": [
    "llama-3.1-70b-versatile",
    "mixtral-8x7b-32768", 
    "gemma2-9b-it"
  ],
  "temperature": 0.7,
  "includeConsensus": true,
  "includePerformanceMetrics": true
}
```

**Example Response:**
```json
{
  "modelPredictions": {
    "llama-3.1-70b-versatile": {
      "predictions": [...],
      "averageConfidence": 0.89,
      "inferenceTimeMs": 245,
      "reasoning": "Advanced pattern recognition with seasonal adjustments"
    },
    "mixtral-8x7b-32768": {
      "predictions": [...],
      "averageConfidence": 0.84,
      "inferenceTimeMs": 180,
      "reasoning": "Multi-expert ensemble approach with frequency weighting"
    },
    "gemma2-9b-it": {
      "predictions": [...],
      "averageConfidence": 0.81,
      "inferenceTimeMs": 120,
      "reasoning": "Efficient pattern matching with statistical validation"
    }
  },
  "consensus": {
    "commonNumbers": [15, 23, 31],
    "diversityScore": 0.72,
    "recommendedModel": "llama-3.1-70b-versatile",
    "confidenceWeightedAverage": 0.85
  },
  "performanceMetrics": {
    "fastestModel": "gemma2-9b-it",
    "mostConfidentModel": "llama-3.1-70b-versatile",
    "mostCostEffective": "gemma2-9b-it",
    "bestAccuracyHistorical": "llama-3.1-70b-versatile"
  }
}
```

## Model Management

### Available Models

Get list of available GroqCloud models and their capabilities.

**Endpoint:** `GET /api/groqcloud/models`

**Example Response:**
```json
{
  "models": [
    {
      "id": "llama-3.1-70b-versatile",
      "name": "Llama 3.1 70B Versatile",
      "description": "Meta's flagship model with exceptional reasoning capabilities",
      "contextLength": 131072,
      "maxTokens": 8192,
      "costPer1MTokens": 0.59,
      "averageInferenceTimeMs": 250,
      "capabilities": [
        "complex_reasoning",
        "pattern_analysis", 
        "statistical_modeling",
        "creative_generation"
      ],
      "recommendedFor": [
        "high_accuracy_predictions",
        "detailed_reasoning",
        "complex_analysis"
      ]
    },
    {
      "id": "mixtral-8x7b-32768",
      "name": "Mixtral 8x7B",
      "description": "Mistral's mixture-of-experts model for balanced performance",
      "contextLength": 32768,
      "maxTokens": 4096,
      "costPer1MTokens": 0.27,
      "averageInferenceTimeMs": 180,
      "capabilities": [
        "multi_expert_reasoning",
        "efficient_processing",
        "balanced_accuracy"
      ],
      "recommendedFor": [
        "cost_effective_predictions",
        "batch_processing",
        "balanced_performance"
      ]
    },
    {
      "id": "gemma2-9b-it",
      "name": "Gemma 2 9B Instruct",
      "description": "Google's efficient instruction-tuned model",
      "contextLength": 8192,
      "maxTokens": 2048,
      "costPer1MTokens": 0.20,
      "averageInferenceTimeMs": 120,
      "capabilities": [
        "fast_inference",
        "instruction_following",
        "efficient_reasoning"
      ],
      "recommendedFor": [
        "high_volume_predictions",
        "real_time_applications",
        "cost_optimization"
      ]
    }
  ],
  "defaultModel": "llama-3.1-70b-versatile",
  "recommendedModel": "llama-3.1-70b-versatile"
}
```

### Model Performance Analytics

Get detailed performance analytics for GroqCloud models.

**Endpoint:** `GET /api/groqcloud/models/analytics`

**Query Parameters:**
- `timeframe` (string): Analysis timeframe (`1d`, `7d`, `30d`)
- `model` (string, optional): Specific model or "all"

**Example Response:**
```json
{
  "timeframe": "7d",
  "modelAnalytics": {
    "llama-3.1-70b-versatile": {
      "totalRequests": 1250,
      "successRate": 0.998,
      "averageInferenceTimeMs": 245,
      "averageConfidence": 0.89,
      "accuracyMetrics": {
        "exactMatches": 0,
        "partialMatches": 89,
        "averageAccuracy": 0.187,
        "trend": "improving"
      },
      "costMetrics": {
        "totalCostUSD": 12.45,
        "costPerPrediction": 0.00996,
        "tokensUsed": 2100000
      },
      "performanceTrend": "stable"
    },
    "mixtral-8x7b-32768": {
      "totalRequests": 890,
      "successRate": 0.995,
      "averageInferenceTimeMs": 180,
      "averageConfidence": 0.84,
      "accuracyMetrics": {
        "exactMatches": 0,
        "partialMatches": 62,
        "averageAccuracy": 0.174,
        "trend": "stable"
      },
      "costMetrics": {
        "totalCostUSD": 6.78,
        "costPerPrediction": 0.00762,
        "tokensUsed": 1500000
      },
      "performanceTrend": "stable"
    }
  },
  "summary": {
    "bestPerformingModel": "llama-3.1-70b-versatile",
    "mostCostEffective": "gemma2-9b-it",
    "fastestModel": "gemma2-9b-it",
    "recommendedForAccuracy": "llama-3.1-70b-versatile",
    "recommendedForSpeed": "gemma2-9b-it",
    "recommendedForCost": "gemma2-9b-it"
  }
}
```

## Performance Optimization

### Inference Optimization

Configure GroqCloud settings for optimal performance.

**Endpoint:** `PUT /api/groqcloud/config/optimization`

**Request Body:**
```json
{
  "defaultModel": "llama-3.1-70b-versatile",
  "fallbackModel": "mixtral-8x7b-32768",
  "performanceMode": "balanced",
  "caching": {
    "enabled": true,
    "ttlMinutes": 30,
    "maxCacheSize": "1GB"
  },
  "batching": {
    "enabled": true,
    "maxBatchSize": 10,
    "batchTimeoutMs": 500
  },
  "streaming": {
    "enabled": true,
    "bufferSize": 1024,
    "flushIntervalMs": 100
  },
  "retryPolicy": {
    "maxRetries": 3,
    "backoffMultiplier": 2,
    "initialDelayMs": 100
  }
}
```

### Load Balancing

Configure intelligent load balancing across GroqCloud models.

**Endpoint:** `PUT /api/groqcloud/config/load-balancing`

**Request Body:**
```json
{
  "strategy": "weighted_round_robin",
  "models": [
    {
      "id": "llama-3.1-70b-versatile",
      "weight": 50,
      "maxConcurrency": 10,
      "priority": 1
    },
    {
      "id": "mixtral-8x7b-32768", 
      "weight": 30,
      "maxConcurrency": 15,
      "priority": 2
    },
    {
      "id": "gemma2-9b-it",
      "weight": 20,
      "maxConcurrency": 20,
      "priority": 3
    }
  ],
  "healthCheck": {
    "enabled": true,
    "intervalSeconds": 30,
    "timeoutMs": 5000
  },
  "circuitBreaker": {
    "enabled": true,
    "failureThreshold": 5,
    "recoveryTimeSeconds": 60
  }
}
```

### Cost Optimization

Monitor and optimize GroqCloud usage costs.

**Endpoint:** `GET /api/groqcloud/cost/analysis`

**Query Parameters:**
- `timeframe` (string): Analysis period (`1d`, `7d`, `30d`)
- `breakdown` (string): Cost breakdown type (`model`, `feature`, `time`)

**Example Response:**
```json
{
  "timeframe": "7d",
  "totalCostUSD": 45.67,
  "costBreakdown": {
    "byModel": {
      "llama-3.1-70b-versatile": 28.90,
      "mixtral-8x7b-32768": 12.45,
      "gemma2-9b-it": 4.32
    },
    "byFeature": {
      "standard_predictions": 35.20,
      "streaming_predictions": 6.80,
      "batch_predictions": 3.67
    },
    "byDay": [
      {"date": "2024-12-06", "cost": 6.45},
      {"date": "2024-12-07", "cost": 7.23},
      {"date": "2024-12-08", "cost": 5.89}
    ]
  },
  "projectedMonthlyCost": 195.43,
  "costOptimizationRecommendations": [
    {
      "type": "model_selection",
      "description": "Switch 30% of requests to Gemma2-9B for 25% cost reduction",
      "potentialSavingsUSD": 11.42
    },
    {
      "type": "batching",
      "description": "Enable batch processing for bulk predictions",
      "potentialSavingsUSD": 8.90
    },
    {
      "type": "caching",
      "description": "Increase cache TTL to reduce duplicate requests",
      "potentialSavingsUSD": 3.45
    }
  ]
}
```

## Monitoring and Analytics

### Real-time Monitoring

Monitor GroqCloud service health and performance in real-time.

**Endpoint:** `GET /api/groqcloud/monitoring/realtime`

**Example Response:**
```json
{
  "serviceStatus": {
    "overall": "healthy",
    "api": "operational",
    "inference": "operational",
    "streaming": "operational"
  },
  "currentMetrics": {
    "activeRequests": 23,
    "queuedRequests": 2,
    "averageResponseTimeMs": 245,
    "requestsPerMinute": 156,
    "successRate": 0.998,
    "errorRate": 0.002
  },
  "modelStatus": {
    "llama-3.1-70b-versatile": {
      "status": "healthy",
      "activeRequests": 12,
      "averageLatencyMs": 245,
      "queueDepth": 1
    },
    "mixtral-8x7b-32768": {
      "status": "healthy", 
      "activeRequests": 8,
      "averageLatencyMs": 180,
      "queueDepth": 0
    },
    "gemma2-9b-it": {
      "status": "healthy",
      "activeRequests": 3,
      "averageLatencyMs": 120,
      "queueDepth": 1
    }
  },
  "alerts": [],
  "lastUpdated": "2024-12-12T10:30:00Z"
}
```

### Usage Analytics

Get comprehensive usage analytics and insights.

**Endpoint:** `GET /api/groqcloud/analytics/usage`

**Query Parameters:**
- `timeframe` (string): Analysis period (`1h`, `1d`, `7d`, `30d`)
- `granularity` (string): Data granularity (`minute`, `hour`, `day`)

**Example Response:**
```json
{
  "timeframe": "7d",
  "granularity": "day",
  "usageMetrics": {
    "totalRequests": 8945,
    "totalTokens": 12500000,
    "totalCostUSD": 45.67,
    "averageRequestsPerDay": 1278,
    "peakRequestsPerHour": 234,
    "averageResponseTimeMs": 198
  },
  "dailyBreakdown": [
    {
      "date": "2024-12-06",
      "requests": 1234,
      "tokens": 1750000,
      "costUSD": 6.45,
      "averageLatencyMs": 205
    }
  ],
  "modelUsageDistribution": {
    "llama-3.1-70b-versatile": 0.55,
    "mixtral-8x7b-32768": 0.32,
    "gemma2-9b-it": 0.13
  },
  "featureUsageDistribution": {
    "standard_predictions": 0.72,
    "streaming_predictions": 0.18,
    "batch_predictions": 0.10
  },
  "trends": {
    "requestGrowth": 0.15,
    "costGrowth": 0.12,
    "latencyTrend": -0.05
  }
}
```

### Performance Benchmarks

Compare GroqCloud performance against other providers.

**Endpoint:** `GET /api/groqcloud/benchmarks`

**Example Response:**
```json
{
  "benchmarkResults": {
    "inferenceSpeed": {
      "groqcloud": {
        "averageLatencyMs": 198,
        "p95LatencyMs": 450,
        "p99LatencyMs": 800,
        "rank": 1
      },
      "bedrock": {
        "averageLatencyMs": 1250,
        "p95LatencyMs": 2100,
        "p99LatencyMs": 3500,
        "rank": 3
      },
      "localLLM": {
        "averageLatencyMs": 850,
        "p95LatencyMs": 1200,
        "p99LatencyMs": 1800,
        "rank": 2
      }
    },
    "accuracy": {
      "groqcloud": {
        "averageAccuracy": 0.182,
        "confidenceCalibration": 0.87,
        "rank": 2
      },
      "bedrock": {
        "averageAccuracy": 0.194,
        "confidenceCalibration": 0.89,
        "rank": 1
      },
      "localLLM": {
        "averageAccuracy": 0.154,
        "confidenceCalibration": 0.82,
        "rank": 3
      }
    },
    "costEfficiency": {
      "groqcloud": {
        "costPerPrediction": 0.0051,
        "accuracyPerDollar": 35.69,
        "rank": 1
      },
      "bedrock": {
        "costPerPrediction": 0.0187,
        "accuracyPerDollar": 10.37,
        "rank": 3
      },
      "localLLM": {
        "costPerPrediction": 0.0023,
        "accuracyPerDollar": 66.96,
        "rank": 2
      }
    }
  },
  "overallRanking": [
    {
      "provider": "GroqCloud",
      "score": 8.7,
      "strengths": ["speed", "cost_efficiency", "reliability"],
      "weaknesses": ["accuracy_vs_bedrock"]
    }
  ]
}
```

## Error Handling

### Standard Error Response Format

All GroqCloud API endpoints return errors in a consistent format:

```json
{
  "error": {
    "code": "GROQ_MODEL_UNAVAILABLE",
    "message": "The requested model llama-3.1-70b-versatile is temporarily unavailable",
    "details": {
      "model": "llama-3.1-70b-versatile",
      "reason": "High demand - model queue full",
      "estimatedWaitTimeSeconds": 45,
      "suggestedAlternatives": ["mixtral-8x7b-32768", "gemma2-9b-it"],
      "retryAfter": 30
    },
    "timestamp": "2024-12-12T10:30:00Z",
    "requestId": "groq_req_abc123",
    "groqRequestId": "req_groq_xyz789"
  }
}
```

### Common Error Codes

| Code | Description | HTTP Status | Retry Strategy |
|------|-------------|-------------|----------------|
| `GROQ_API_KEY_INVALID` | Invalid or expired API key | 401 | Update credentials |
| `GROQ_RATE_LIMIT_EXCEEDED` | Rate limit exceeded | 429 | Exponential backoff |
| `GROQ_MODEL_UNAVAILABLE` | Requested model unavailable | 503 | Try alternative model |
| `GROQ_QUOTA_EXCEEDED` | Monthly quota exceeded | 402 | Upgrade plan or wait |
| `GROQ_CONTEXT_LENGTH_EXCEEDED` | Input exceeds model context | 400 | Reduce input size |
| `GROQ_TIMEOUT` | Request timeout | 504 | Retry with backoff |
| `GROQ_INTERNAL_ERROR` | GroqCloud internal error | 500 | Retry after delay |
| `GROQ_MAINTENANCE` | Scheduled maintenance | 503 | Wait for completion |

### Error Recovery Strategies

#### Automatic Fallback
```json
{
  "fallbackStrategy": {
    "enabled": true,
    "fallbackChain": [
      "llama-3.1-70b-versatile",
      "mixtral-8x7b-32768", 
      "gemma2-9b-it",
      "localLLM"
    ],
    "maxFallbackAttempts": 3,
    "fallbackDelayMs": 1000
  }
}
```

#### Circuit Breaker Pattern
```json
{
  "circuitBreaker": {
    "enabled": true,
    "failureThreshold": 5,
    "recoveryTimeSeconds": 60,
    "halfOpenMaxRequests": 3
  }
}
```

## Rate Limiting

### Rate Limits by Plan

| Plan | Requests per Minute | Tokens per Day | Concurrent Requests |
|------|-------------------|----------------|-------------------|
| Free | 30 | 14,400 | 2 |
| Pay-as-you-go | 6,000 | Unlimited | 128 |
| Enterprise | Custom | Unlimited | Custom |

### Rate Limit Headers

All responses include rate limiting information:

```http
X-RateLimit-Limit-Requests: 6000
X-RateLimit-Remaining-Requests: 5847
X-RateLimit-Reset-Requests: 1702377600
X-RateLimit-Limit-Tokens: 1000000
X-RateLimit-Remaining-Tokens: 987654
X-RateLimit-Reset-Tokens: 1702464000
```

### Rate Limit Management

#### Adaptive Rate Limiting
```json
{
  "adaptiveRateLimiting": {
    "enabled": true,
    "baseRequestsPerMinute": 6000,
    "burstMultiplier": 1.5,
    "backoffStrategy": "exponential",
    "monitoringWindowMinutes": 5
  }
}
```

#### Request Queuing
```json
{
  "requestQueuing": {
    "enabled": true,
    "maxQueueSize": 100,
    "queueTimeoutSeconds": 30,
    "priorityLevels": ["high", "normal", "low"]
  }
}
```

## SDK and Client Libraries

### LangChain Integration

#### Python with langchain_groq
```python
from langchain_groq import ChatGroq
from langchain.prompts import ChatPromptTemplate
from langchain.schema.output_parser import StrOutputParser
from langchain.schema.runnable import RunnablePassthrough
import os

# Initialize GroqCloud LLM
llm = ChatGroq(
    groq_api_key=os.getenv("GROQCLOUD_API_KEY"),
    model_name="llama-3.1-70b-versatile",
    temperature=0.7,
    max_tokens=4096
)

# Create prediction chain
prediction_prompt = ChatPromptTemplate.from_messages([
    ("system", "You are an expert lottery prediction AI."),
    ("human", "Generate {count} lottery predictions based on: {historical_data}")
])

prediction_chain = (
    RunnablePassthrough()
    | prediction_prompt
    | llm
    | StrOutputParser()
)

# Generate predictions
result = prediction_chain.invoke({
    "count": 3,
    "historical_data": historical_data
})

# Streaming predictions
for chunk in llm.stream("Generate lottery prediction"):
    print(chunk.content, end="", flush=True)

# Async predictions
async def async_predict():
    response = await llm.ainvoke("Generate lottery prediction")
    return response.content
```

#### LangChain Agents with Tools
```python
from langchain.agents import initialize_agent, AgentType
from langchain.tools import BaseTool
from langchain_groq import ChatGroq

class LotteryAnalysisTool(BaseTool):
    name = "lottery_analysis"
    description = "Analyze lottery data for patterns"
    
    def _run(self, data: str) -> str:
        # Implement analysis logic
        return "Analysis results"

# Initialize agent
llm = ChatGroq(groq_api_key=os.getenv("GROQCLOUD_API_KEY"))
tools = [LotteryAnalysisTool()]

agent = initialize_agent(
    tools=tools,
    llm=llm,
    agent=AgentType.ZERO_SHOT_REACT_DESCRIPTION,
    verbose=True
)

# Use agent
result = agent.run("Analyze lottery data and generate predictions")
```

### JavaScript/TypeScript Client

```typescript
import { GroqCloudClient } from '@predict-lotto-nz/groqcloud-client';

const client = new GroqCloudClient({
  apiKey: process.env.GROQCLOUD_API_KEY,
  baseUrl: 'http://localhost:5000',
  defaultModel: 'llama-3.1-70b-versatile'
});

// Generate predictions
const predictions = await client.predictions.generate({
  count: 5,
  model: 'llama-3.1-70b-versatile',
  temperature: 0.7,
  includeReasoning: true
});

// Stream predictions
const stream = client.predictions.stream({
  count: 1,
  model: 'mixtral-8x7b-32768'
});

stream.on('reasoning', (data) => {
  console.log('Reasoning:', data.content);
});

stream.on('numbers', (data) => {
  console.log('Numbers:', data.content);
});

stream.on('complete', (prediction) => {
  console.log('Final prediction:', prediction);
});

// Batch predictions
const batchResults = await client.predictions.batch([
  { count: 3, model: 'llama-3.1-70b-versatile' },
  { count: 2, model: 'gemma2-9b-it' }
]);

// Model comparison
const comparison = await client.models.compare({
  count: 3,
  models: ['llama-3.1-70b-versatile', 'mixtral-8x7b-32768']
});
```

### Python Client with LangChain

```python
from predict_lotto_nz import GroqCloudClient
from langchain_groq import ChatGroq
from langchain.chains import LLMChain
from langchain.prompts import PromptTemplate
import os

# Standard client
client = GroqCloudClient(
    api_key=os.getenv("GROQCLOUD_API_KEY"),
    base_url="http://localhost:5000",
    default_model="llama-3.1-70b-versatile"
)

# LangChain integration
llm = ChatGroq(
    groq_api_key=os.getenv("GROQCLOUD_API_KEY"),
    model_name="llama-3.1-70b-versatile"
)

# Create prediction chain
prompt = PromptTemplate(
    input_variables=["historical_data", "count"],
    template="Generate {count} lottery predictions based on: {historical_data}"
)

chain = LLMChain(llm=llm, prompt=prompt)

# Generate predictions
predictions = chain.run(
    historical_data="[recent lottery draws]",
    count=5
)

# Stream predictions with LangChain
for chunk in llm.stream("Generate lottery prediction"):
    print(chunk.content, end="")

# Async predictions
import asyncio

async def async_predictions():
    response = await llm.ainvoke("Generate lottery predictions")
    return response.content

# Batch predictions
batch_results = client.predictions.batch([
    {"count": 3, "model": "llama-3.1-70b-versatile"},
    {"count": 2, "model": "gemma2-9b-it"}
])

# Model analytics
analytics = client.models.analytics(timeframe="7d")
print(f"Best performing model: {analytics.summary.best_performing_model}")
```

### CLI Tool

```bash
# Install CLI
npm install -g @predict-lotto-nz/groqcloud-cli

# Configure API key
groqcloud config set-api-key gsk_your_api_key_here

# Generate predictions
groqcloud predict --count 5 --model llama-3.1-70b-versatile --reasoning

# Stream predictions
groqcloud predict --count 1 --stream --model mixtral-8x7b-32768

# Compare models
groqcloud compare --models llama-3.1-70b-versatile,mixtral-8x7b-32768 --count 3

# View analytics
groqcloud analytics --timeframe 7d --model all

# Monitor real-time
groqcloud monitor --refresh 5s
```

## LangChain Advanced Features

### Chain Composition

Create complex prediction workflows using LangChain chains:

**Endpoint:** `POST /api/groqcloud/langchain/chain`

**Request Body:**
```json
{
  "chainType": "sequential",
  "steps": [
    {
      "name": "analysis",
      "prompt": "Analyze lottery data: {historical_data}",
      "model": "llama-3.1-70b-versatile"
    },
    {
      "name": "prediction",
      "prompt": "Based on analysis: {analysis}, generate {count} predictions",
      "model": "mixtral-8x7b-32768"
    }
  ],
  "input": {
    "historical_data": "[lottery data]",
    "count": 3
  }
}
```

### Memory Management

Manage conversation context and prediction history:

**Endpoint:** `POST /api/groqcloud/langchain/memory`

**Request Body:**
```json
{
  "memoryType": "conversation_buffer_window",
  "config": {
    "k": 10,
    "returnMessages": true
  },
  "operation": "add_context",
  "context": {
    "prediction_history": "[previous predictions]",
    "accuracy_feedback": "[accuracy results]"
  }
}
```

### Agent-Based Predictions

Use LangChain agents with tools for advanced analysis:

**Endpoint:** `POST /api/groqcloud/langchain/agent`

**Request Body:**
```json
{
  "agentType": "zero_shot_react",
  "tools": [
    "lottery_analysis",
    "frequency_calculator",
    "pattern_detector"
  ],
  "task": "Analyze data and generate 5 lottery predictions",
  "input": {
    "historical_data": "[lottery data]",
    "analysis_depth": "comprehensive"
  }
}
```

**Example Response:**
```json
{
  "agentResponse": {
    "thought": "I need to analyze the lottery data first",
    "action": "lottery_analysis",
    "actionInput": "[lottery data]",
    "observation": "Analysis shows patterns in numbers 7, 14, 21",
    "finalAnswer": {
      "predictions": [
        {
          "numbers": [7, 14, 21, 28, 35, 42],
          "confidence": 0.87,
          "reasoning": "Strong pattern correlation detected"
        }
      ]
    }
  },
  "toolsUsed": ["lottery_analysis", "pattern_detector"],
  "executionTimeMs": 1250
}
```

### Prompt Templates

Manage and version prediction prompts:

**Endpoint:** `GET /api/groqcloud/langchain/templates`

**Example Response:**
```json
{
  "templates": [
    {
      "id": "lottery_prediction_v1",
      "name": "Basic Lottery Prediction",
      "template": "Generate {count} lottery predictions based on: {data}",
      "variables": ["count", "data"],
      "version": "1.0",
      "performance": {
        "averageAccuracy": 0.18,
        "averageConfidence": 0.85
      }
    },
    {
      "id": "advanced_analysis_v2",
      "name": "Advanced Analysis Prediction",
      "template": "Analyze patterns in {data}. Consider {factors}. Generate {count} predictions with reasoning.",
      "variables": ["data", "factors", "count"],
      "version": "2.1",
      "performance": {
        "averageAccuracy": 0.22,
        "averageConfidence": 0.91
      }
    }
  ]
}
```

### Streaming with LangChain

Real-time streaming using LangChain callbacks:

**Endpoint:** `GET /api/groqcloud/langchain/stream`

**Parameters:**
- `chainId` (string): Chain identifier
- `input` (object): Chain input parameters
- `callbacks` (array): Callback types to enable

**Example Streaming Response:**
```
data: {"type": "chain_start", "chainId": "prediction_chain"}

data: {"type": "llm_start", "model": "llama-3.1-70b-versatile"}

data: {"type": "llm_new_token", "token": "Based"}

data: {"type": "llm_new_token", "token": " on"}

data: {"type": "tool_start", "tool": "lottery_analysis"}

data: {"type": "tool_end", "tool": "lottery_analysis", "result": "..."}

data: {"type": "chain_end", "result": {"predictions": [...]}}
```

## Advanced Configuration

### Environment Variables

```env
# GroqCloud Authentication
GROQCLOUD_API_KEY=gsk_your_groqcloud_api_key_here
GROQCLOUD_BASE_URL=https://api.groq.com/openai/v1

# Model Configuration
GROQCLOUD_DEFAULT_MODEL=llama-3.1-70b-versatile
GROQCLOUD_FALLBACK_MODEL=mixtral-8x7b-32768
GROQCLOUD_MAX_TOKENS=4096
GROQCLOUD_TEMPERATURE=0.7
GROQCLOUD_TOP_P=0.9

# Performance Settings
GROQCLOUD_TIMEOUT_SECONDS=30
GROQCLOUD_MAX_RETRIES=3
GROQCLOUD_RETRY_DELAY_MS=1000
GROQCLOUD_MAX_CONCURRENT_REQUESTS=10

# Caching Configuration
GROQCLOUD_ENABLE_CACHING=true
GROQCLOUD_CACHE_TTL_MINUTES=30
GROQCLOUD_CACHE_MAX_SIZE_MB=512

# Monitoring and Logging
GROQCLOUD_ENABLE_LOGGING=true
GROQCLOUD_LOG_LEVEL=INFO
GROQCLOUD_ENABLE_METRICS=true
GROQCLOUD_METRICS_INTERVAL_SECONDS=60

# Cost Management
GROQCLOUD_DAILY_COST_LIMIT_USD=50.00
GROQCLOUD_MONTHLY_COST_LIMIT_USD=1000.00
GROQCLOUD_COST_ALERT_THRESHOLD=0.8
```

### Docker Configuration

```yaml
# docker-compose.groqcloud.yml
version: '3.8'
services:
  groqcloud-service:
    build:
      context: ./backend
      dockerfile: Dockerfile.groqcloud
    ports:
      - "5001:80"
    environment:
      - GROQCLOUD_API_KEY=${GROQCLOUD_API_KEY}
      - GROQCLOUD_DEFAULT_MODEL=${GROQCLOUD_DEFAULT_MODEL}
      - GROQCLOUD_MAX_CONCURRENT_REQUESTS=${GROQCLOUD_MAX_CONCURRENT_REQUESTS}
    volumes:
      - groqcloud-cache:/cache
      - groqcloud-logs:/logs
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health/groqcloud"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  groqcloud-cache:
  groqcloud-logs:
```

This completes the comprehensive GroqCloud API Documentation, providing detailed information about integrating GroqCloud's high-performance inference platform with PredictLottoNZ.