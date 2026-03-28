# GroqCloud Setup and Configuration Guide

This comprehensive guide covers the setup, configuration, and management of GroqCloud integration with PredictLottoNZ using LangChain.

## Table of Contents

- [Overview](#overview)
- [GroqCloud Account Setup](#groqcloud-account-setup)
- [LangChain Integration](#langchain-integration)
- [Environment Configuration](#environment-configuration)
- [Model Selection and Optimization](#model-selection-and-optimization)
- [Advanced LangChain Features](#advanced-langchain-features)
- [Performance Tuning](#performance-tuning)
- [Monitoring and Troubleshooting](#monitoring-and-troubleshooting)

## Overview

GroqCloud provides ultra-fast AI inference with hardware acceleration, integrated through LangChain for:

- **Lightning-Fast Inference**: Sub-second response times with specialized hardware
- **LangChain Integration**: Seamless integration using `langchain_groq` package
- **Multiple Model Support**: Access to Llama 3.1, Mixtral, Gemma, and other models
- **Advanced Prompting**: LangChain's prompt templates and chains
- **Memory Management**: Conversation memory and context handling
- **Tool Integration**: Function calling and external tool integration

## GroqCloud Account Setup

### Step 1: Create GroqCloud Account

1. Visit [GroqCloud Console](https://console.groq.com)
2. Sign up with email or GitHub account
3. Verify your email address
4. Complete account setup

### Step 2: Generate API Key

1. Navigate to API Keys section in console
2. Click "Create API Key"
3. Name your key (e.g., "PredictLottoNZ-Production")
4. Copy and securely store the API key
5. Set usage limits and restrictions as needed

### Step 3: Verify Account Access

```bash
# Test API key validity
curl -X GET "https://api.groq.com/openai/v1/models" \
  -H "Authorization: Bearer gsk_your_api_key_here"
```

## LangChain Integration

### Prerequisites

- Python 3.8+ with pip
- Virtual environment (recommended)
- GroqCloud API key

### Step 1: Install Dependencies

```bash
# Create virtual environment
python -m venv groq_env
source groq_env/bin/activate  # On Windows: groq_env\Scripts\activate

# Install required packages
pip install langchain-groq
pip install langchain
pip install langchain-community
pip install python-dotenv
pip install pydantic
pip install tiktoken
```
### Step 2: Basic LangChain Configuration

#### Create Environment Configuration
```python
# .env file
GROQCLOUD_API_KEY=gsk_your_groqcloud_api_key_here
GROQCLOUD_MODEL=llama-3.1-70b-versatile
GROQCLOUD_TEMPERATURE=0.7
GROQCLOUD_MAX_TOKENS=4096
GROQCLOUD_TOP_P=0.9
```

#### Basic LangChain Setup
```python
# groq_langchain_setup.py
import os
from dotenv import load_dotenv
from langchain_groq import ChatGroq
from langchain.schema import HumanMessage, SystemMessage

# Load environment variables
load_dotenv()

# Initialize GroqCloud LLM
llm = ChatGroq(
    groq_api_key=os.getenv("GROQCLOUD_API_KEY"),
    model_name=os.getenv("GROQCLOUD_MODEL", "llama-3.1-70b-versatile"),
    temperature=float(os.getenv("GROQCLOUD_TEMPERATURE", "0.7")),
    max_tokens=int(os.getenv("GROQCLOUD_MAX_TOKENS", "4096")),
    top_p=float(os.getenv("GROQCLOUD_TOP_P", "0.9"))
)

# Test basic functionality
def test_groq_connection():
    messages = [
        SystemMessage(content="You are a helpful AI assistant."),
        HumanMessage(content="Hello! Can you confirm the connection is working?")
    ]
    
    response = llm(messages)
    print(f"GroqCloud Response: {response.content}")
    return response

if __name__ == "__main__":
    test_groq_connection()
```

### Step 3: Lottery Prediction Integration

#### Create Lottery Prediction Chain
```python
# lottery_prediction_chain.py
from langchain_groq import ChatGroq
from langchain.prompts import ChatPromptTemplate
from langchain.schema.output_parser import StrOutputParser
from langchain.schema.runnable import RunnablePassthrough
from typing import List, Dict, Any
import json
import os

class GroqLotteryPredictor:
    def __init__(self):
        self.llm = ChatGroq(
            groq_api_key=os.getenv("GROQCLOUD_API_KEY"),
            model_name=os.getenv("GROQCLOUD_MODEL", "llama-3.1-70b-versatile"),
            temperature=0.7,
            max_tokens=2048
        )
        
        self.prediction_prompt = ChatPromptTemplate.from_messages([
            ("system", self._get_system_prompt()),
            ("human", self._get_human_prompt())
        ])
        
        self.chain = (
            RunnablePassthrough()
            | self.prediction_prompt
            | self.llm
            | StrOutputParser()
        )
    
    def _get_system_prompt(self) -> str:
        return """You are an advanced lottery prediction AI with expertise in:
        - Statistical analysis of historical lottery data
        - Pattern recognition in number sequences
        - Frequency analysis and trend identification
        - Probability calculations and risk assessment
        
        Your task is to analyze historical lottery data and generate predictions
        with detailed reasoning. Always provide confidence scores and explain
        your analytical approach."""
    
    def _get_human_prompt(self) -> str:
        return """Based on the following historical lottery data, generate {count} 
        lottery predictions for a 6-number lottery (numbers 1-40):

        Historical Data:
        {historical_data}

        Recent Trends:
        {recent_trends}

        Requirements:
        - Each prediction must contain exactly 6 unique numbers between 1-40
        - Provide confidence score (0.0-1.0) for each prediction
        - Include detailed reasoning for each prediction
        - Consider frequency patterns, gaps, and statistical trends
        - Format response as valid JSON

        Response format:
        {{
            "predictions": [
                {{
                    "numbers": [1, 2, 3, 4, 5, 6],
                    "confidence": 0.85,
                    "reasoning": "Detailed explanation of prediction logic"
                }}
            ]
        }}"""
    
    def generate_predictions(self, historical_data: List[Dict], count: int = 1) -> Dict[str, Any]:
        # Process historical data
        recent_trends = self._analyze_trends(historical_data)
        
        # Prepare input data
        input_data = {
            "count": count,
            "historical_data": json.dumps(historical_data[-20:], indent=2),
            "recent_trends": json.dumps(recent_trends, indent=2)
        }
        
        # Generate predictions
        response = self.chain.invoke(input_data)
        
        try:
            # Parse JSON response
            predictions = json.loads(response)
            return predictions
        except json.JSONDecodeError:
            # Fallback parsing if JSON is malformed
            return self._parse_fallback_response(response)
    
    def _analyze_trends(self, historical_data: List[Dict]) -> Dict[str, Any]:
        """Analyze recent trends in historical data"""
        if not historical_data:
            return {}
        
        # Calculate frequency of each number
        frequency = {}
        for draw in historical_data[-10:]:  # Last 10 draws
            for number in draw.get('numbers', []):
                frequency[number] = frequency.get(number, 0) + 1
        
        # Identify hot and cold numbers
        sorted_freq = sorted(frequency.items(), key=lambda x: x[1], reverse=True)
        hot_numbers = [num for num, freq in sorted_freq[:10]]
        cold_numbers = [num for num, freq in sorted_freq[-10:]]
        
        return {
            "hot_numbers": hot_numbers,
            "cold_numbers": cold_numbers,
            "frequency_distribution": frequency,
            "total_draws_analyzed": len(historical_data[-10:])
        }
    
    def _parse_fallback_response(self, response: str) -> Dict[str, Any]:
        """Fallback parser for non-JSON responses"""
        # Implementation for parsing non-JSON responses
        return {
            "predictions": [],
            "error": "Failed to parse response",
            "raw_response": response
        }

# Usage example
if __name__ == "__main__":
    predictor = GroqLotteryPredictor()
    
    # Sample historical data
    sample_data = [
        {"draw": 1999, "numbers": [7, 14, 21, 28, 35, 42], "date": "2024-12-07"},
        {"draw": 2000, "numbers": [3, 11, 19, 27, 33, 39], "date": "2024-12-14"}
    ]
    
    predictions = predictor.generate_predictions(sample_data, count=3)
    print(json.dumps(predictions, indent=2))
```

## Environment Configuration

### Production Environment Variables
```env
# GroqCloud Authentication
GROQCLOUD_API_KEY=gsk_your_groqcloud_api_key_here
GROQCLOUD_BASE_URL=https://api.groq.com/openai/v1

# Model Configuration
GROQCLOUD_MODEL=llama-3.1-70b-versatile
GROQCLOUD_FALLBACK_MODEL=mixtral-8x7b-32768
GROQCLOUD_TEMPERATURE=0.7
GROQCLOUD_MAX_TOKENS=4096
GROQCLOUD_TOP_P=0.9
GROQCLOUD_TOP_K=50

# LangChain Configuration
LANGCHAIN_TRACING_V2=true
LANGCHAIN_ENDPOINT=https://api.smith.langchain.com
LANGCHAIN_API_KEY=your_langsmith_api_key
LANGCHAIN_PROJECT=predict-lotto-nz-groq

# Performance Settings
GROQCLOUD_TIMEOUT_SECONDS=30
GROQCLOUD_MAX_RETRIES=3
GROQCLOUD_RETRY_DELAY_MS=1000
GROQCLOUD_STREAMING=true

# Caching Configuration
GROQCLOUD_ENABLE_CACHING=true
GROQCLOUD_CACHE_TTL_SECONDS=1800
GROQCLOUD_CACHE_MAX_SIZE=1000

# Rate Limiting
GROQCLOUD_REQUESTS_PER_MINUTE=6000
GROQCLOUD_TOKENS_PER_DAY=1000000
GROQCLOUD_MAX_CONCURRENT=128

# Monitoring and Logging
GROQCLOUD_LOG_LEVEL=INFO
GROQCLOUD_ENABLE_METRICS=true
GROQCLOUD_METRICS_INTERVAL=60
```

### Development Configuration
```python
# config/groq_config.py
import os
from typing import Optional
from pydantic import BaseSettings, Field

class GroqCloudConfig(BaseSettings):
    # Authentication
    api_key: str = Field(..., env="GROQCLOUD_API_KEY")
    base_url: str = Field("https://api.groq.com/openai/v1", env="GROQCLOUD_BASE_URL")
    
    # Model Settings
    model: str = Field("llama-3.1-70b-versatile", env="GROQCLOUD_MODEL")
    fallback_model: str = Field("mixtral-8x7b-32768", env="GROQCLOUD_FALLBACK_MODEL")
    temperature: float = Field(0.7, env="GROQCLOUD_TEMPERATURE")
    max_tokens: int = Field(4096, env="GROQCLOUD_MAX_TOKENS")
    top_p: float = Field(0.9, env="GROQCLOUD_TOP_P")
    
    # Performance
    timeout_seconds: int = Field(30, env="GROQCLOUD_TIMEOUT_SECONDS")
    max_retries: int = Field(3, env="GROQCLOUD_MAX_RETRIES")
    streaming: bool = Field(True, env="GROQCLOUD_STREAMING")
    
    # Caching
    enable_caching: bool = Field(True, env="GROQCLOUD_ENABLE_CACHING")
    cache_ttl_seconds: int = Field(1800, env="GROQCLOUD_CACHE_TTL_SECONDS")
    
    # Rate Limiting
    requests_per_minute: int = Field(6000, env="GROQCLOUD_REQUESTS_PER_MINUTE")
    max_concurrent: int = Field(128, env="GROQCLOUD_MAX_CONCURRENT")
    
    class Config:
        env_file = ".env"
        case_sensitive = False

# Global configuration instance
groq_config = GroqCloudConfig()
```
## Model Selection and Optimization

### Available Models and Use Cases

#### Llama 3.1 70B Versatile
```python
# Best for: High accuracy, complex reasoning, detailed analysis
model_config = {
    "model_name": "llama-3.1-70b-versatile",
    "context_length": 131072,
    "strengths": ["complex_reasoning", "detailed_analysis", "high_accuracy"],
    "use_cases": ["detailed_predictions", "complex_pattern_analysis"],
    "cost_per_1m_tokens": 0.59,
    "avg_latency_ms": 250
}
```

#### Mixtral 8x7B
```python
# Best for: Balanced performance, cost-effectiveness
model_config = {
    "model_name": "mixtral-8x7b-32768",
    "context_length": 32768,
    "strengths": ["balanced_performance", "cost_effective", "good_reasoning"],
    "use_cases": ["batch_predictions", "general_analysis"],
    "cost_per_1m_tokens": 0.27,
    "avg_latency_ms": 180
}
```

#### Gemma 2 9B IT
```python
# Best for: Speed, high-volume processing
model_config = {
    "model_name": "gemma2-9b-it",
    "context_length": 8192,
    "strengths": ["fast_inference", "efficient", "instruction_following"],
    "use_cases": ["real_time_predictions", "high_volume_processing"],
    "cost_per_1m_tokens": 0.20,
    "avg_latency_ms": 120
}
```

### Dynamic Model Selection
```python
# dynamic_model_selector.py
from langchain_groq import ChatGroq
from typing import Dict, Any, Optional
import time

class DynamicModelSelector:
    def __init__(self, config: GroqCloudConfig):
        self.config = config
        self.models = {
            "high_accuracy": "llama-3.1-70b-versatile",
            "balanced": "mixtral-8x7b-32768",
            "fast": "gemma2-9b-it"
        }
        self.performance_cache = {}
    
    def select_model(self, 
                    prediction_count: int,
                    complexity: str = "medium",
                    priority: str = "balanced") -> str:
        """Select optimal model based on requirements"""
        
        if priority == "accuracy" or complexity == "high":
            return self.models["high_accuracy"]
        elif priority == "speed" or prediction_count > 10:
            return self.models["fast"]
        else:
            return self.models["balanced"]
    
    def get_llm(self, model_name: Optional[str] = None) -> ChatGroq:
        """Get configured LLM instance"""
        model = model_name or self.config.model
        
        return ChatGroq(
            groq_api_key=self.config.api_key,
            model_name=model,
            temperature=self.config.temperature,
            max_tokens=self.config.max_tokens,
            top_p=self.config.top_p,
            timeout=self.config.timeout_seconds,
            max_retries=self.config.max_retries,
            streaming=self.config.streaming
        )
    
    def benchmark_model(self, model_name: str, test_prompts: list) -> Dict[str, float]:
        """Benchmark model performance"""
        llm = self.get_llm(model_name)
        
        latencies = []
        for prompt in test_prompts:
            start_time = time.time()
            try:
                response = llm.invoke(prompt)
                latency = (time.time() - start_time) * 1000  # ms
                latencies.append(latency)
            except Exception as e:
                print(f"Error benchmarking {model_name}: {e}")
                continue
        
        if latencies:
            return {
                "avg_latency_ms": sum(latencies) / len(latencies),
                "min_latency_ms": min(latencies),
                "max_latency_ms": max(latencies),
                "success_rate": len(latencies) / len(test_prompts)
            }
        return {}
```

## Advanced LangChain Features

### Memory and Context Management
```python
# memory_management.py
from langchain.memory import ConversationBufferWindowMemory, ConversationSummaryMemory
from langchain.schema import BaseMessage
from langchain_groq import ChatGroq
from typing import List, Dict, Any

class GroqMemoryManager:
    def __init__(self, llm: ChatGroq):
        self.llm = llm
        
        # Different memory types for different use cases
        self.window_memory = ConversationBufferWindowMemory(
            k=10,  # Keep last 10 interactions
            return_messages=True
        )
        
        self.summary_memory = ConversationSummaryMemory(
            llm=llm,
            return_messages=True
        )
    
    def add_prediction_context(self, 
                             historical_data: List[Dict],
                             prediction_result: Dict[str, Any]):
        """Add prediction context to memory"""
        context = f"""
        Historical Analysis: {len(historical_data)} draws analyzed
        Prediction Generated: {prediction_result.get('numbers', [])}
        Confidence: {prediction_result.get('confidence', 0)}
        Reasoning: {prediction_result.get('reasoning', 'N/A')}
        """
        
        self.window_memory.save_context(
            {"input": "Generate lottery prediction"},
            {"output": context}
        )
    
    def get_context_for_prediction(self) -> str:
        """Get relevant context for new predictions"""
        messages = self.window_memory.chat_memory.messages
        
        # Extract relevant prediction history
        context_parts = []
        for message in messages[-6:]:  # Last 3 interactions
            if hasattr(message, 'content'):
                context_parts.append(message.content)
        
        return "\n".join(context_parts)
```

### Chain Composition and Workflows
```python
# advanced_chains.py
from langchain.chains import LLMChain, SequentialChain
from langchain.prompts import PromptTemplate
from langchain_groq import ChatGroq
from langchain.schema.runnable import RunnableParallel, RunnableLambda
from typing import Dict, Any, List

class AdvancedPredictionChains:
    def __init__(self, llm: ChatGroq):
        self.llm = llm
        self.setup_chains()
    
    def setup_chains(self):
        # Analysis Chain
        analysis_prompt = PromptTemplate(
            input_variables=["historical_data"],
            template="""
            Analyze the following lottery data for patterns:
            {historical_data}
            
            Provide:
            1. Frequency analysis of numbers
            2. Pattern identification
            3. Trend analysis
            4. Statistical insights
            
            Format as structured analysis.
            """
        )
        
        self.analysis_chain = LLMChain(
            llm=self.llm,
            prompt=analysis_prompt,
            output_key="analysis"
        )
        
        # Prediction Chain
        prediction_prompt = PromptTemplate(
            input_variables=["analysis", "count"],
            template="""
            Based on this analysis:
            {analysis}
            
            Generate {count} lottery predictions with:
            - 6 numbers each (1-40)
            - Confidence scores
            - Detailed reasoning
            
            Format as JSON array.
            """
        )
        
        self.prediction_chain = LLMChain(
            llm=self.llm,
            prompt=prediction_prompt,
            output_key="predictions"
        )
        
        # Validation Chain
        validation_prompt = PromptTemplate(
            input_variables=["predictions"],
            template="""
            Validate these lottery predictions:
            {predictions}
            
            Check for:
            - Number range validity (1-40)
            - Duplicate numbers within predictions
            - Reasonable confidence scores
            - Logical reasoning
            
            Return validation results and corrected predictions if needed.
            """
        )
        
        self.validation_chain = LLMChain(
            llm=self.llm,
            prompt=validation_prompt,
            output_key="validated_predictions"
        )
        
        # Sequential Chain combining all steps
        self.full_chain = SequentialChain(
            chains=[self.analysis_chain, self.prediction_chain, self.validation_chain],
            input_variables=["historical_data", "count"],
            output_variables=["analysis", "predictions", "validated_predictions"],
            verbose=True
        )
    
    def parallel_analysis_chain(self):
        """Create parallel analysis for different aspects"""
        
        frequency_analysis = RunnableLambda(
            lambda x: self._analyze_frequency(x["historical_data"])
        )
        
        pattern_analysis = RunnableLambda(
            lambda x: self._analyze_patterns(x["historical_data"])
        )
        
        trend_analysis = RunnableLambda(
            lambda x: self._analyze_trends(x["historical_data"])
        )
        
        return RunnableParallel(
            frequency=frequency_analysis,
            patterns=pattern_analysis,
            trends=trend_analysis
        )
    
    def _analyze_frequency(self, data: List[Dict]) -> Dict[str, Any]:
        """Analyze number frequency"""
        frequency = {}
        for draw in data:
            for number in draw.get('numbers', []):
                frequency[number] = frequency.get(number, 0) + 1
        return {"frequency_analysis": frequency}
    
    def _analyze_patterns(self, data: List[Dict]) -> Dict[str, Any]:
        """Analyze number patterns"""
        # Implementation for pattern analysis
        return {"pattern_analysis": "Pattern analysis results"}
    
    def _analyze_trends(self, data: List[Dict]) -> Dict[str, Any]:
        """Analyze trends over time"""
        # Implementation for trend analysis
        return {"trend_analysis": "Trend analysis results"}
```

### Function Calling and Tool Integration
```python
# tool_integration.py
from langchain.tools import BaseTool
from langchain.agents import initialize_agent, AgentType
from langchain_groq import ChatGroq
from typing import Dict, Any, List
import json
import statistics

class LotteryAnalysisTool(BaseTool):
    name = "lottery_analysis"
    description = "Analyze lottery data for statistical insights"
    
    def _run(self, historical_data: str) -> str:
        """Analyze lottery data"""
        try:
            data = json.loads(historical_data)
            
            # Calculate statistics
            all_numbers = []
            for draw in data:
                all_numbers.extend(draw.get('numbers', []))
            
            if not all_numbers:
                return "No data to analyze"
            
            analysis = {
                "total_draws": len(data),
                "total_numbers": len(all_numbers),
                "mean": statistics.mean(all_numbers),
                "median": statistics.median(all_numbers),
                "mode": statistics.mode(all_numbers) if all_numbers else None,
                "std_dev": statistics.stdev(all_numbers) if len(all_numbers) > 1 else 0
            }
            
            return json.dumps(analysis, indent=2)
            
        except Exception as e:
            return f"Error analyzing data: {str(e)}"
    
    async def _arun(self, historical_data: str) -> str:
        """Async version"""
        return self._run(historical_data)

class FrequencyAnalysisTool(BaseTool):
    name = "frequency_analysis"
    description = "Calculate number frequency in lottery draws"
    
    def _run(self, historical_data: str, top_n: int = 10) -> str:
        """Calculate frequency analysis"""
        try:
            data = json.loads(historical_data)
            frequency = {}
            
            for draw in data:
                for number in draw.get('numbers', []):
                    frequency[number] = frequency.get(number, 0) + 1
            
            # Sort by frequency
            sorted_freq = sorted(frequency.items(), key=lambda x: x[1], reverse=True)
            
            result = {
                "hot_numbers": sorted_freq[:top_n],
                "cold_numbers": sorted_freq[-top_n:],
                "total_unique_numbers": len(frequency)
            }
            
            return json.dumps(result, indent=2)
            
        except Exception as e:
            return f"Error in frequency analysis: {str(e)}"
    
    async def _arun(self, historical_data: str, top_n: int = 10) -> str:
        return self._run(historical_data, top_n)

class GroqLotteryAgent:
    def __init__(self, llm: ChatGroq):
        self.llm = llm
        self.tools = [
            LotteryAnalysisTool(),
            FrequencyAnalysisTool()
        ]
        
        self.agent = initialize_agent(
            tools=self.tools,
            llm=llm,
            agent=AgentType.ZERO_SHOT_REACT_DESCRIPTION,
            verbose=True,
            handle_parsing_errors=True
        )
    
    def analyze_and_predict(self, historical_data: List[Dict], count: int = 1) -> Dict[str, Any]:
        """Use agent to analyze data and generate predictions"""
        
        prompt = f"""
        Using the available tools, analyze the lottery data and generate {count} predictions.
        
        Historical data: {json.dumps(historical_data)}
        
        Steps:
        1. Use lottery_analysis tool to get statistical insights
        2. Use frequency_analysis tool to identify hot/cold numbers
        3. Based on the analysis, generate {count} lottery predictions
        4. Provide confidence scores and reasoning for each prediction
        
        Format the final response as JSON with predictions array.
        """
        
        try:
            response = self.agent.run(prompt)
            return {"agent_response": response}
        except Exception as e:
            return {"error": str(e), "agent_response": None}
```
## Performance Tuning

### Streaming and Async Processing
```python
# streaming_async.py
import asyncio
from langchain_groq import ChatGroq
from langchain.callbacks.streaming_stdout import StreamingStdOutCallbackHandler
from langchain.callbacks.base import AsyncCallbackHandler
from typing import Dict, Any, List, AsyncGenerator
import json

class GroqStreamingHandler(AsyncCallbackHandler):
    def __init__(self):
        self.tokens = []
        self.current_prediction = {}
    
    async def on_llm_new_token(self, token: str, **kwargs) -> None:
        """Handle new token from streaming"""
        self.tokens.append(token)
        
        # Try to parse partial JSON for real-time updates
        current_text = "".join(self.tokens)
        try:
            # Look for complete JSON objects
            if current_text.count('{') == current_text.count('}'):
                parsed = json.loads(current_text)
                if 'numbers' in parsed:
                    self.current_prediction = parsed
        except json.JSONDecodeError:
            pass  # Continue streaming

class AsyncGroqPredictor:
    def __init__(self, config: GroqCloudConfig):
        self.config = config
        self.streaming_handler = GroqStreamingHandler()
        
        self.llm = ChatGroq(
            groq_api_key=config.api_key,
            model_name=config.model,
            temperature=config.temperature,
            streaming=True,
            callbacks=[self.streaming_handler]
        )
    
    async def stream_predictions(self, 
                               historical_data: List[Dict],
                               count: int = 1) -> AsyncGenerator[Dict[str, Any], None]:
        """Stream predictions in real-time"""
        
        prompt = self._build_prediction_prompt(historical_data, count)
        
        async for chunk in self.llm.astream(prompt):
            if hasattr(chunk, 'content'):
                yield {
                    "type": "token",
                    "content": chunk.content,
                    "partial_prediction": self.streaming_handler.current_prediction
                }
        
        # Final prediction
        yield {
            "type": "complete",
            "prediction": self.streaming_handler.current_prediction
        }
    
    async def batch_predictions_async(self, 
                                    requests: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        """Process multiple prediction requests concurrently"""
        
        tasks = []
        for request in requests:
            task = self._generate_single_prediction_async(
                request.get('historical_data', []),
                request.get('count', 1),
                request.get('id', f"batch_{len(tasks)}")
            )
            tasks.append(task)
        
        results = await asyncio.gather(*tasks, return_exceptions=True)
        
        # Process results
        processed_results = []
        for i, result in enumerate(results):
            if isinstance(result, Exception):
                processed_results.append({
                    "id": requests[i].get('id', f"batch_{i}"),
                    "status": "error",
                    "error": str(result)
                })
            else:
                processed_results.append({
                    "id": requests[i].get('id', f"batch_{i}"),
                    "status": "success",
                    "prediction": result
                })
        
        return processed_results
    
    async def _generate_single_prediction_async(self, 
                                              historical_data: List[Dict],
                                              count: int,
                                              request_id: str) -> Dict[str, Any]:
        """Generate single prediction asynchronously"""
        
        prompt = self._build_prediction_prompt(historical_data, count)
        response = await self.llm.ainvoke(prompt)
        
        try:
            prediction = json.loads(response.content)
            prediction['request_id'] = request_id
            return prediction
        except json.JSONDecodeError:
            return {
                "request_id": request_id,
                "error": "Failed to parse prediction",
                "raw_response": response.content
            }
    
    def _build_prediction_prompt(self, historical_data: List[Dict], count: int) -> str:
        """Build prediction prompt"""
        return f"""
        Generate {count} lottery predictions based on this data:
        {json.dumps(historical_data[-10:], indent=2)}
        
        Return JSON format:
        {{
            "predictions": [
                {{
                    "numbers": [1,2,3,4,5,6],
                    "confidence": 0.85,
                    "reasoning": "explanation"
                }}
            ]
        }}
        """

# Usage example
async def main():
    config = GroqCloudConfig()
    predictor = AsyncGroqPredictor(config)
    
    sample_data = [
        {"draw": 1999, "numbers": [7, 14, 21, 28, 35, 42], "date": "2024-12-07"}
    ]
    
    # Stream predictions
    print("Streaming predictions...")
    async for chunk in predictor.stream_predictions(sample_data, count=1):
        print(f"Chunk: {chunk}")
    
    # Batch predictions
    batch_requests = [
        {"id": "req1", "historical_data": sample_data, "count": 2},
        {"id": "req2", "historical_data": sample_data, "count": 1}
    ]
    
    batch_results = await predictor.batch_predictions_async(batch_requests)
    print(f"Batch results: {json.dumps(batch_results, indent=2)}")

if __name__ == "__main__":
    asyncio.run(main())
```

### Caching and Performance Optimization
```python
# caching_optimization.py
from functools import lru_cache
import hashlib
import json
import time
from typing import Dict, Any, List, Optional
import redis
from langchain_groq import ChatGroq

class GroqCacheManager:
    def __init__(self, redis_url: str = "redis://localhost:6379"):
        self.redis_client = redis.from_url(redis_url)
        self.default_ttl = 1800  # 30 minutes
    
    def _generate_cache_key(self, 
                          historical_data: List[Dict],
                          model: str,
                          count: int,
                          temperature: float) -> str:
        """Generate cache key for prediction request"""
        
        # Create deterministic hash of input parameters
        cache_input = {
            "historical_data": historical_data[-10:],  # Last 10 draws
            "model": model,
            "count": count,
            "temperature": temperature
        }
        
        cache_string = json.dumps(cache_input, sort_keys=True)
        return f"groq_prediction:{hashlib.md5(cache_string.encode()).hexdigest()}"
    
    def get_cached_prediction(self, 
                            historical_data: List[Dict],
                            model: str,
                            count: int,
                            temperature: float) -> Optional[Dict[str, Any]]:
        """Get cached prediction if available"""
        
        cache_key = self._generate_cache_key(historical_data, model, count, temperature)
        
        try:
            cached_data = self.redis_client.get(cache_key)
            if cached_data:
                return json.loads(cached_data)
        except Exception as e:
            print(f"Cache retrieval error: {e}")
        
        return None
    
    def cache_prediction(self, 
                        historical_data: List[Dict],
                        model: str,
                        count: int,
                        temperature: float,
                        prediction: Dict[str, Any],
                        ttl: Optional[int] = None) -> bool:
        """Cache prediction result"""
        
        cache_key = self._generate_cache_key(historical_data, model, count, temperature)
        ttl = ttl or self.default_ttl
        
        try:
            # Add metadata to cached prediction
            cached_prediction = {
                **prediction,
                "cached_at": time.time(),
                "cache_key": cache_key
            }
            
            self.redis_client.setex(
                cache_key,
                ttl,
                json.dumps(cached_prediction)
            )
            return True
        except Exception as e:
            print(f"Cache storage error: {e}")
            return False
    
    def invalidate_cache_pattern(self, pattern: str = "groq_prediction:*"):
        """Invalidate cache entries matching pattern"""
        try:
            keys = self.redis_client.keys(pattern)
            if keys:
                self.redis_client.delete(*keys)
                return len(keys)
        except Exception as e:
            print(f"Cache invalidation error: {e}")
        return 0

class OptimizedGroqPredictor:
    def __init__(self, config: GroqCloudConfig, cache_manager: GroqCacheManager):
        self.config = config
        self.cache_manager = cache_manager
        self.llm = ChatGroq(
            groq_api_key=config.api_key,
            model_name=config.model,
            temperature=config.temperature,
            max_tokens=config.max_tokens
        )
        
        # Performance metrics
        self.metrics = {
            "cache_hits": 0,
            "cache_misses": 0,
            "total_requests": 0,
            "avg_response_time": 0
        }
    
    def generate_predictions_cached(self, 
                                  historical_data: List[Dict],
                                  count: int = 1,
                                  use_cache: bool = True) -> Dict[str, Any]:
        """Generate predictions with caching"""
        
        start_time = time.time()
        self.metrics["total_requests"] += 1
        
        # Check cache first
        if use_cache:
            cached_result = self.cache_manager.get_cached_prediction(
                historical_data, self.config.model, count, self.config.temperature
            )
            
            if cached_result:
                self.metrics["cache_hits"] += 1
                response_time = (time.time() - start_time) * 1000
                self._update_avg_response_time(response_time)
                
                return {
                    **cached_result,
                    "cache_hit": True,
                    "response_time_ms": response_time
                }
        
        # Generate new prediction
        self.metrics["cache_misses"] += 1
        
        prediction = self._generate_new_prediction(historical_data, count)
        
        # Cache the result
        if use_cache:
            self.cache_manager.cache_prediction(
                historical_data, self.config.model, count, 
                self.config.temperature, prediction
            )
        
        response_time = (time.time() - start_time) * 1000
        self._update_avg_response_time(response_time)
        
        return {
            **prediction,
            "cache_hit": False,
            "response_time_ms": response_time
        }
    
    def _generate_new_prediction(self, 
                               historical_data: List[Dict],
                               count: int) -> Dict[str, Any]:
        """Generate new prediction using LLM"""
        
        prompt = f"""
        Based on lottery data: {json.dumps(historical_data[-10:])}
        Generate {count} predictions with 6 numbers each (1-40).
        Include confidence and reasoning.
        Return as JSON.
        """
        
        response = self.llm.invoke(prompt)
        
        try:
            return json.loads(response.content)
        except json.JSONDecodeError:
            return {
                "predictions": [],
                "error": "Failed to parse response",
                "raw_response": response.content
            }
    
    def _update_avg_response_time(self, response_time: float):
        """Update average response time metric"""
        current_avg = self.metrics["avg_response_time"]
        total_requests = self.metrics["total_requests"]
        
        self.metrics["avg_response_time"] = (
            (current_avg * (total_requests - 1) + response_time) / total_requests
        )
    
    def get_performance_metrics(self) -> Dict[str, Any]:
        """Get performance metrics"""
        total_requests = self.metrics["total_requests"]
        cache_hit_rate = (
            self.metrics["cache_hits"] / total_requests 
            if total_requests > 0 else 0
        )
        
        return {
            **self.metrics,
            "cache_hit_rate": cache_hit_rate,
            "cache_miss_rate": 1 - cache_hit_rate
        }
```

## Monitoring and Troubleshooting

### LangSmith Integration
```python
# langsmith_monitoring.py
import os
from langchain.callbacks.tracers import LangChainTracer
from langchain.callbacks.manager import CallbackManager
from langchain_groq import ChatGroq

# Configure LangSmith
os.environ["LANGCHAIN_TRACING_V2"] = "true"
os.environ["LANGCHAIN_ENDPOINT"] = "https://api.smith.langchain.com"
os.environ["LANGCHAIN_API_KEY"] = "your_langsmith_api_key"
os.environ["LANGCHAIN_PROJECT"] = "predict-lotto-nz-groq"

class MonitoredGroqPredictor:
    def __init__(self, config: GroqCloudConfig):
        # Setup tracing
        tracer = LangChainTracer(project_name="predict-lotto-nz-groq")
        callback_manager = CallbackManager([tracer])
        
        self.llm = ChatGroq(
            groq_api_key=config.api_key,
            model_name=config.model,
            temperature=config.temperature,
            callback_manager=callback_manager
        )
    
    def generate_with_tracing(self, prompt: str) -> str:
        """Generate prediction with full tracing"""
        return self.llm.invoke(prompt).content
```

### Error Handling and Retry Logic
```python
# error_handling.py
import time
import random
from typing import Dict, Any, List, Optional, Callable
from langchain_groq import ChatGroq
import logging

class GroqErrorHandler:
    def __init__(self, config: GroqCloudConfig):
        self.config = config
        self.logger = logging.getLogger(__name__)
        
        # Error patterns and retry strategies
        self.retry_errors = [
            "rate_limit_exceeded",
            "timeout",
            "internal_server_error",
            "service_unavailable"
        ]
        
        self.fallback_models = [
            "llama-3.1-70b-versatile",
            "mixtral-8x7b-32768",
            "gemma2-9b-it"
        ]
    
    def with_retry(self, 
                   func: Callable,
                   max_retries: int = 3,
                   backoff_factor: float = 2.0,
                   jitter: bool = True) -> Any:
        """Execute function with retry logic"""
        
        for attempt in range(max_retries + 1):
            try:
                return func()
            
            except Exception as e:
                error_type = self._classify_error(e)
                
                if attempt == max_retries or error_type not in self.retry_errors:
                    self.logger.error(f"Final attempt failed: {e}")
                    raise e
                
                # Calculate backoff delay
                delay = backoff_factor ** attempt
                if jitter:
                    delay *= (0.5 + random.random() * 0.5)  # Add jitter
                
                self.logger.warning(
                    f"Attempt {attempt + 1} failed: {e}. "
                    f"Retrying in {delay:.2f} seconds..."
                )
                time.sleep(delay)
        
        raise Exception("Max retries exceeded")
    
    def with_fallback(self, 
                     primary_func: Callable,
                     fallback_funcs: List[Callable]) -> Any:
        """Execute with fallback functions"""
        
        try:
            return primary_func()
        except Exception as e:
            self.logger.warning(f"Primary function failed: {e}")
            
            for i, fallback_func in enumerate(fallback_funcs):
                try:
                    self.logger.info(f"Trying fallback {i + 1}")
                    return fallback_func()
                except Exception as fallback_error:
                    self.logger.warning(f"Fallback {i + 1} failed: {fallback_error}")
                    continue
            
            raise Exception("All fallback options exhausted")
    
    def _classify_error(self, error: Exception) -> str:
        """Classify error type for retry strategy"""
        error_str = str(error).lower()
        
        if "rate limit" in error_str or "429" in error_str:
            return "rate_limit_exceeded"
        elif "timeout" in error_str:
            return "timeout"
        elif "500" in error_str or "internal server" in error_str:
            return "internal_server_error"
        elif "503" in error_str or "unavailable" in error_str:
            return "service_unavailable"
        else:
            return "unknown_error"

class RobustGroqPredictor:
    def __init__(self, config: GroqCloudConfig):
        self.config = config
        self.error_handler = GroqErrorHandler(config)
        self.primary_llm = self._create_llm(config.model)
        self.fallback_llms = [
            self._create_llm(model) for model in self.error_handler.fallback_models
            if model != config.model
        ]
    
    def _create_llm(self, model_name: str) -> ChatGroq:
        """Create LLM instance for specific model"""
        return ChatGroq(
            groq_api_key=self.config.api_key,
            model_name=model_name,
            temperature=self.config.temperature,
            max_tokens=self.config.max_tokens,
            timeout=self.config.timeout_seconds,
            max_retries=0  # Handle retries manually
        )
    
    def generate_predictions_robust(self, 
                                  historical_data: List[Dict],
                                  count: int = 1) -> Dict[str, Any]:
        """Generate predictions with full error handling"""
        
        def primary_prediction():
            return self._generate_with_llm(self.primary_llm, historical_data, count)
        
        fallback_predictions = [
            lambda llm=llm: self._generate_with_llm(llm, historical_data, count)
            for llm in self.fallback_llms
        ]
        
        # Try with retry and fallback
        try:
            return self.error_handler.with_retry(
                lambda: self.error_handler.with_fallback(
                    primary_prediction,
                    fallback_predictions
                )
            )
        except Exception as e:
            return {
                "predictions": [],
                "error": str(e),
                "fallback_attempted": True,
                "model_used": "none"
            }
    
    def _generate_with_llm(self, 
                          llm: ChatGroq,
                          historical_data: List[Dict],
                          count: int) -> Dict[str, Any]:
        """Generate prediction with specific LLM"""
        
        prompt = f"""
        Generate {count} lottery predictions based on:
        {json.dumps(historical_data[-10:])}
        
        Return JSON with predictions array.
        """
        
        response = llm.invoke(prompt)
        
        try:
            result = json.loads(response.content)
            result["model_used"] = llm.model_name
            return result
        except json.JSONDecodeError:
            raise Exception(f"Failed to parse response from {llm.model_name}")
```

### Health Monitoring and Alerts
```python
# health_monitoring.py
import time
import asyncio
from typing import Dict, Any, List
from dataclasses import dataclass
from enum import Enum

class HealthStatus(Enum):
    HEALTHY = "healthy"
    DEGRADED = "degraded"
    UNHEALTHY = "unhealthy"

@dataclass
class HealthMetric:
    name: str
    value: float
    threshold: float
    status: HealthStatus
    timestamp: float

class GroqHealthMonitor:
    def __init__(self, predictor: RobustGroqPredictor):
        self.predictor = predictor
        self.metrics_history = []
        self.alert_callbacks = []
    
    async def continuous_monitoring(self, interval_seconds: int = 60):
        """Run continuous health monitoring"""
        
        while True:
            try:
                health_metrics = await self.check_health()
                self.metrics_history.append(health_metrics)
                
                # Keep only last 100 metrics
                if len(self.metrics_history) > 100:
                    self.metrics_history.pop(0)
                
                # Check for alerts
                await self._check_alerts(health_metrics)
                
                await asyncio.sleep(interval_seconds)
                
            except Exception as e:
                print(f"Health monitoring error: {e}")
                await asyncio.sleep(interval_seconds)
    
    async def check_health(self) -> Dict[str, HealthMetric]:
        """Check current health status"""
        
        metrics = {}
        
        # Test response time
        start_time = time.time()
        try:
            test_data = [{"draw": 1, "numbers": [1,2,3,4,5,6], "date": "2024-01-01"}]
            result = self.predictor.generate_predictions_robust(test_data, count=1)
            response_time = (time.time() - start_time) * 1000
            
            metrics["response_time"] = HealthMetric(
                name="response_time_ms",
                value=response_time,
                threshold=5000,  # 5 seconds
                status=HealthStatus.HEALTHY if response_time < 5000 else HealthStatus.DEGRADED,
                timestamp=time.time()
            )
            
            # Check if prediction was successful
            success = "error" not in result
            metrics["success_rate"] = HealthMetric(
                name="prediction_success",
                value=1.0 if success else 0.0,
                threshold=0.95,
                status=HealthStatus.HEALTHY if success else HealthStatus.UNHEALTHY,
                timestamp=time.time()
            )
            
        except Exception as e:
            metrics["response_time"] = HealthMetric(
                name="response_time_ms",
                value=float('inf'),
                threshold=5000,
                status=HealthStatus.UNHEALTHY,
                timestamp=time.time()
            )
        
        return metrics
    
    async def _check_alerts(self, metrics: Dict[str, HealthMetric]):
        """Check metrics for alert conditions"""
        
        for metric in metrics.values():
            if metric.status == HealthStatus.UNHEALTHY:
                await self._trigger_alert(f"CRITICAL: {metric.name} is unhealthy", metric)
            elif metric.status == HealthStatus.DEGRADED:
                await self._trigger_alert(f"WARNING: {metric.name} is degraded", metric)
    
    async def _trigger_alert(self, message: str, metric: HealthMetric):
        """Trigger alert notifications"""
        
        alert_data = {
            "message": message,
            "metric": metric,
            "timestamp": time.time()
        }
        
        for callback in self.alert_callbacks:
            try:
                await callback(alert_data)
            except Exception as e:
                print(f"Alert callback error: {e}")
    
    def add_alert_callback(self, callback):
        """Add alert callback function"""
        self.alert_callbacks.append(callback)

# Usage example
async def slack_alert_callback(alert_data):
    """Example Slack alert callback"""
    print(f"SLACK ALERT: {alert_data['message']}")
    # Implement actual Slack notification here

async def main():
    config = GroqCloudConfig()
    predictor = RobustGroqPredictor(config)
    monitor = GroqHealthMonitor(predictor)
    
    # Add alert callbacks
    monitor.add_alert_callback(slack_alert_callback)
    
    # Start monitoring
    await monitor.continuous_monitoring(interval_seconds=30)

if __name__ == "__main__":
    asyncio.run(main())
```

This completes the comprehensive GroqCloud Setup and Configuration Guide with full LangChain integration using `langchain_groq`. The guide covers everything from basic setup to advanced features like streaming, caching, error handling, and monitoring.