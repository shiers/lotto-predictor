# LangSmith Integration Guide

## Overview

LangSmith provides observability, debugging, and evaluation tools for your LLM-powered lottery prediction system. This guide covers setup, usage, and best practices.

## What is LangSmith?

LangSmith is LangChain's platform for:

- **Tracing**: Automatic logging of all LLM calls with inputs, outputs, and metadata
- **Debugging**: Visual inspection of prediction chains and reasoning steps
- **Monitoring**: Performance metrics, error rates, and cost tracking
- **Evaluation**: Dataset management and prediction quality assessment
- **Collaboration**: Team-wide visibility into model behavior

## Quick Start

### 1. Environment Setup

Your `.env` file is already configured with:

```bash
LANGCHAIN_TRACING_V2=true
LANGCHAIN_ENDPOINT=https://api.smith.langchain.com
LANGCHAIN_API_KEY=lsv2_pt_your_langchain_api_key_here
LANGCHAIN_PROJECT=predict-lotto-nz
```

### 2. Install Dependencies

```bash
cd predictor
pip install -r requirements.txt
```

This installs:

- `langsmith` - Core LangSmith client
- `langchain` - LangChain framework
- `langchain-groq` - GroqCloud integration
- `langchain-community` - Community integrations
- `langchain-openai` - OpenAI integration

### 3. Verify Setup

```python
import os
from langsmith import Client

# Initialize client
client = Client(
    api_key=os.getenv("LANGCHAIN_API_KEY"),
    api_url=os.getenv("LANGCHAIN_ENDPOINT")
)

# Test connection
print(f"Connected to project: {os.getenv('LANGCHAIN_PROJECT')}")
```

## Automatic Tracing

Once environment variables are set, all LangChain operations are automatically traced:

```python
import os
from langchain_groq import ChatGroq

# Set environment variables (already in .env)
os.environ["LANGCHAIN_TRACING_V2"] = "true"
os.environ["LANGCHAIN_API_KEY"] = os.getenv("LANGCHAIN_API_KEY")
os.environ["LANGCHAIN_PROJECT"] = "predict-lotto-nz"

# All calls are now automatically traced
llm = ChatGroq(
    groq_api_key=os.getenv("GROQCLOUD_API_KEY"),
    model_name="llama-3.3-70b-versatile"
)

# This call will appear in LangSmith dashboard
response = llm.invoke("Generate lottery prediction")
```

## Manual Tracing

For non-LangChain code, use manual tracing:

```python
from langsmith import traceable
from langsmith.run_helpers import get_current_run_tree

@traceable(name="generate_lottery_numbers")
def generate_prediction(historical_data, count=6):
    """Generate lottery numbers with LangSmith tracing"""

    # Your prediction logic here
    numbers = [1, 2, 3, 4, 5, 6]

    # Add metadata to trace
    run = get_current_run_tree()
    if run:
        run.add_metadata({
            "prediction_count": count,
            "data_points": len(historical_data),
            "model_version": "v1.0"
        })

    return numbers

# Call will be traced
result = generate_prediction(historical_data=[...], count=6)
```

## Viewing Traces

### Access Dashboard

1. Go to [smith.langchain.com](https://smith.langchain.com)
2. Select project: **predict-lotto-nz**
3. View traces in real-time

### Trace Information

Each trace includes:

- **Input**: Prompt and parameters
- **Output**: Generated predictions
- **Metadata**: Model, temperature, tokens
- **Timing**: Latency breakdown
- **Cost**: Token usage and estimated cost
- **Tags**: Custom labels for filtering

## Advanced Features

### 1. Custom Tags and Metadata

```python
from langchain_groq import ChatGroq

llm = ChatGroq(
    groq_api_key=os.getenv("GROQCLOUD_API_KEY"),
    model_name="llama-3.3-70b-versatile",
    tags=["production", "lottery-prediction"],
    metadata={
        "version": "v2.0",
        "provider": "groqcloud",
        "draw_date": "2026-01-15"
    }
)
```

### 2. Feedback Collection

```python
from langsmith import Client

client = Client()

# After prediction is evaluated
client.create_feedback(
    run_id="<trace_run_id>",
    key="accuracy",
    score=0.85,
    comment="3 out of 6 numbers matched"
)
```

### 3. Dataset Management

```python
from langsmith import Client

client = Client()

# Create evaluation dataset
dataset = client.create_dataset(
    dataset_name="lottery-test-cases",
    description="Historical draws for evaluation"
)

# Add examples
client.create_example(
    dataset_id=dataset.id,
    inputs={"historical_draws": [...]},
    outputs={"predicted_numbers": [1, 2, 3, 4, 5, 6]}
)
```

### 4. Run Evaluation

```python
from langsmith.evaluation import evaluate

def accuracy_evaluator(run, example):
    """Custom evaluator for prediction accuracy"""
    predicted = run.outputs["numbers"]
    actual = example.outputs["actual_draw"]

    matches = len(set(predicted) & set(actual))
    return {"score": matches / 6}

# Run evaluation
results = evaluate(
    predict_function,
    data="lottery-test-cases",
    evaluators=[accuracy_evaluator],
    experiment_prefix="groqcloud-v1"
)
```

## Integration with Existing Services

### GroqCloud Provider

Update `backend/Services/GroqCloudPredictionProvider.cs` to log trace URLs:

```csharp
// After successful prediction
_logger.LogInformation(
    "Prediction traced to LangSmith: https://smith.langchain.com/o/{org}/projects/p/{project}/r/{run_id}",
    orgId, projectId, runId
);
```

### FastAPI Service

Update `predictor/main.py`:

```python
import os
from fastapi import FastAPI
from langsmith import Client

# Initialize LangSmith
os.environ["LANGCHAIN_TRACING_V2"] = "true"

app = FastAPI()

@app.post("/predict")
async def predict(request: PredictRequest):
    # Predictions are automatically traced
    result = await generate_predictions(request)
    return result
```

## Monitoring and Alerts

### Key Metrics to Track

1. **Latency**: Response time per prediction
2. **Error Rate**: Failed predictions / total predictions
3. **Token Usage**: Cost tracking
4. **Accuracy**: Feedback scores over time

### Setting Up Alerts

1. Go to Project Settings in LangSmith
2. Configure alerts for:
   - High error rates (> 5%)
   - Slow responses (> 5 seconds)
   - High costs (> $X per day)

## Best Practices

### 1. Use Descriptive Names

```python
@traceable(name="lottery_prediction_with_frequency_analysis")
def predict_with_analysis(data):
    pass
```

### 2. Add Context with Metadata

```python
metadata = {
    "draw_date": "2026-01-15",
    "provider": "groqcloud",
    "model": "llama-3.3-70b",
    "temperature": 0.7,
    "historical_days": 90
}
```

### 3. Tag by Environment

```python
tags = ["production"]  # or ["development", "testing"]
```

### 4. Collect Feedback

After each draw, update predictions with actual results:

```python
client.create_feedback(
    run_id=prediction_run_id,
    key="matches",
    score=matches_count,
    comment=f"Matched {matches_count}/6 numbers"
)
```

### 5. Regular Evaluation

Run weekly evaluations against historical data:

```bash
python scripts/evaluate_predictions.py --dataset=last-30-days
```

## Troubleshooting

### Traces Not Appearing

1. Check environment variables are set:

   ```bash
   echo $LANGCHAIN_TRACING_V2
   echo $LANGCHAIN_API_KEY
   ```

2. Verify API key is valid:

   ```python
   from langsmith import Client
   client = Client()
   print(client.info())
   ```

3. Check network connectivity:
   ```bash
   curl https://api.smith.langchain.com/info
   ```

### High Latency

- LangSmith adds ~10-50ms overhead
- Use async tracing for production:
  ```python
  os.environ["LANGCHAIN_TRACING_ASYNC"] = "true"
  ```

### Cost Concerns

- LangSmith is free for up to 5,000 traces/month
- Paid plans start at $39/month for 50,000 traces
- Current usage visible in dashboard

## Next Steps

1. **Install dependencies**: `pip install -r predictor/requirements.txt`
2. **Restart services**: `docker-compose restart predictor`
3. **Generate predictions**: Make API calls to trigger tracing
4. **View dashboard**: Check [smith.langchain.com](https://smith.langchain.com)
5. **Add feedback**: Update traces with actual draw results

## Resources

- [LangSmith Documentation](https://docs.smith.langchain.com)
- [LangChain Documentation](https://python.langchain.com)
- [GroqCloud Integration](./GROQCLOUD_SETUP.md)
- [API Reference](https://api.smith.langchain.com/docs)

## Support

For issues or questions:

- LangSmith Discord: [discord.gg/langchain](https://discord.gg/langchain)
- Documentation: [docs.smith.langchain.com](https://docs.smith.langchain.com)
- GitHub Issues: [github.com/langchain-ai/langsmith-sdk](https://github.com/langchain-ai/langsmith-sdk)
