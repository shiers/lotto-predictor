"""
Local LLM Prediction Processing

This module handles prediction requests, processes historical data,
and generates predictions with confidence scoring and reasoning.
"""

import json
import logging
from typing import List, Dict, Any, Optional, Tuple
from datetime import datetime
from pydantic import BaseModel, Field, validator
from local_llm_service import LocalLlmService, get_service_instance

# Configure logging
logger = logging.getLogger(__name__)


class LottoDrawData(BaseModel):
    """Historical lottery draw data"""
    draw: int = Field(..., description="Draw number")
    date: datetime = Field(..., description="Draw date")
    winning_numbers: List[int] = Field(..., description="Six winning numbers")
    bonus_number: int = Field(..., description="Bonus number")
    powerball: int = Field(..., description="Powerball number")
    
    @validator('winning_numbers')
    def validate_winning_numbers(cls, v):
        if len(v) != 6:
            raise ValueError('Must have exactly 6 winning numbers')
        if not all(1 <= num <= 40 for num in v):
            raise ValueError('All winning numbers must be between 1 and 40')
        if len(set(v)) != 6:
            raise ValueError('All winning numbers must be unique')
        return sorted(v)
    
    @validator('bonus_number', 'powerball')
    def validate_special_numbers(cls, v):
        if not (1 <= v <= 40):
            raise ValueError('Special numbers must be between 1 and 40')
        return v


class PredictionAccuracyData(BaseModel):
    """Prediction accuracy metrics"""
    provider_name: str = Field(..., description="Name of the prediction provider")
    exact_matches: int = Field(..., description="Number of exact matches")
    partial_matches: int = Field(..., description="Number of partial matches")
    proximity_score: float = Field(..., description="Proximity score (0.0 to 1.0)")
    overall_accuracy: float = Field(..., description="Overall accuracy score")
    prediction_count: int = Field(..., description="Total number of predictions")


class LocalLlmRequest(BaseModel):
    """Request for local LLM prediction"""
    historical_data: List[LottoDrawData] = Field(..., description="Historical lottery draws")
    performance_metrics: List[PredictionAccuracyData] = Field(
        default=[], 
        description="Previous prediction performance metrics"
    )
    prediction_count: int = Field(default=1, ge=1, le=10, description="Number of predictions to generate")
    target_draw_date: Optional[datetime] = Field(None, description="Target draw date for prediction")
    
    @validator('historical_data')
    def validate_historical_data(cls, v):
        if len(v) < 10:
            raise ValueError('Need at least 10 historical draws for prediction')
        return v


class LocalLlmPredictionResult(BaseModel):
    """Result from local LLM prediction"""
    numbers: List[int] = Field(..., description="Predicted lottery numbers")
    confidence_score: float = Field(..., description="Confidence score (0.0 to 1.0)")
    explanation: str = Field(..., description="Explanation of the prediction reasoning")
    key_factors: List[str] = Field(..., description="Key factors that influenced the prediction")
    metadata: Dict[str, Any] = Field(default_factory=dict, description="Additional metadata")
    
    @validator('numbers')
    def validate_numbers(cls, v):
        if len(v) != 6:
            raise ValueError('Must predict exactly 6 numbers')
        if not all(1 <= num <= 40 for num in v):
            raise ValueError('All numbers must be between 1 and 40')
        if len(set(v)) != 6:
            raise ValueError('All numbers must be unique')
        return sorted(v)
    
    @validator('confidence_score')
    def validate_confidence_score(cls, v):
        if not (0.0 <= v <= 1.0):
            raise ValueError('Confidence score must be between 0.0 and 1.0')
        return v


class LocalLlmPredictor:
    """
    Local LLM Prediction Processor
    
    Handles prediction requests using the local LLM service,
    processes historical data, and generates predictions with
    confidence scoring and reasoning explanations.
    """
    
    def __init__(self):
        self.service: Optional[LocalLlmService] = None
        logger.info("LocalLlmPredictor initialized")
    
    def _get_service(self) -> LocalLlmService:
        """Get the LLM service instance"""
        if self.service is None:
            self.service = get_service_instance()
        
        if self.service is None:
            raise RuntimeError("Local LLM service not initialized")
        
        if not self.service.is_model_loaded():
            raise RuntimeError("No model loaded in LLM service")
        
        return self.service
    
    def _analyze_historical_patterns(self, historical_data: List[LottoDrawData]) -> Dict[str, Any]:
        """
        Analyze patterns in historical lottery data
        
        Args:
            historical_data: List of historical draws
            
        Returns:
            Dict containing pattern analysis
        """
        try:
            # Number frequency analysis
            number_frequency = {}
            for draw in historical_data:
                for num in draw.winning_numbers:
                    number_frequency[num] = number_frequency.get(num, 0) + 1
            
            # Calculate frequency percentages
            total_draws = len(historical_data)
            frequency_percentages = {
                num: (count / total_draws) * 100 
                for num, count in number_frequency.items()
            }
            
            # Find hot and cold numbers
            sorted_by_frequency = sorted(frequency_percentages.items(), key=lambda x: x[1], reverse=True)
            hot_numbers = [num for num, _ in sorted_by_frequency[:10]]
            cold_numbers = [num for num, _ in sorted_by_frequency[-10:]]
            
            # Analyze number gaps and patterns
            recent_draws = historical_data[-20:] if len(historical_data) >= 20 else historical_data
            recent_numbers = []
            for draw in recent_draws:
                recent_numbers.extend(draw.winning_numbers)
            
            recent_frequency = {}
            for num in recent_numbers:
                recent_frequency[num] = recent_frequency.get(num, 0) + 1
            
            # Calculate trends
            trending_up = []
            trending_down = []
            
            for num in range(1, 41):
                overall_freq = frequency_percentages.get(num, 0)
                recent_freq = (recent_frequency.get(num, 0) / len(recent_draws)) * 100
                
                if recent_freq > overall_freq * 1.2:  # 20% above average
                    trending_up.append(num)
                elif recent_freq < overall_freq * 0.8:  # 20% below average
                    trending_down.append(num)
            
            return {
                "number_frequency": number_frequency,
                "frequency_percentages": frequency_percentages,
                "hot_numbers": hot_numbers,
                "cold_numbers": cold_numbers,
                "trending_up": trending_up,
                "trending_down": trending_down,
                "total_draws_analyzed": total_draws,
                "recent_draws_analyzed": len(recent_draws)
            }
            
        except Exception as e:
            logger.error(f"Error analyzing historical patterns: {e}")
            return {}
    
    def _generate_prediction_prompt(
        self, 
        historical_data: List[LottoDrawData],
        performance_metrics: List[PredictionAccuracyData],
        patterns: Dict[str, Any]
    ) -> str:
        """
        Generate a prompt for the LLM based on historical data and patterns
        
        Args:
            historical_data: Historical lottery draws
            performance_metrics: Previous prediction performance
            patterns: Analyzed patterns from historical data
            
        Returns:
            str: Generated prompt for the LLM
        """
        try:
            # Recent draws for context
            recent_draws = historical_data[-10:]
            recent_draws_text = "\n".join([
                f"Draw {draw.draw} ({draw.date.strftime('%Y-%m-%d')}): {draw.winning_numbers}"
                for draw in recent_draws
            ])
            
            # Performance context
            performance_text = ""
            if performance_metrics:
                avg_accuracy = sum(m.overall_accuracy for m in performance_metrics) / len(performance_metrics)
                performance_text = f"\nPrevious prediction accuracy: {avg_accuracy:.2%}"
            
            # Pattern insights
            hot_numbers = patterns.get("hot_numbers", [])[:5]
            cold_numbers = patterns.get("cold_numbers", [])[:5]
            trending_up = patterns.get("trending_up", [])[:3]
            
            prompt = f"""
You are an expert lottery number predictor analyzing New Zealand Lotto data.

RECENT DRAWS:
{recent_draws_text}

STATISTICAL PATTERNS:
- Most frequent numbers (hot): {hot_numbers}
- Least frequent numbers (cold): {cold_numbers}
- Currently trending up: {trending_up}
- Total draws analyzed: {patterns.get('total_draws_analyzed', 0)}

{performance_text}

TASK: Predict 6 unique numbers between 1-40 for the next New Zealand Lotto draw.

Consider:
1. Historical frequency patterns
2. Recent trends and gaps
3. Number distribution balance
4. Avoid obvious patterns (consecutive numbers, all even/odd)

Provide your prediction with confidence score (0.0-1.0) and reasoning.

Format your response as JSON:
{{
    "numbers": [6 unique numbers between 1-40],
    "confidence_score": 0.0-1.0,
    "explanation": "Brief explanation of reasoning",
    "key_factors": ["factor1", "factor2", "factor3"]
}}
"""
            return prompt.strip()
            
        except Exception as e:
            logger.error(f"Error generating prediction prompt: {e}")
            return "Generate 6 unique lottery numbers between 1-40 with confidence score and reasoning."
    
    def _simulate_llm_prediction(self, prompt: str) -> Dict[str, Any]:
        """
        Simulate LLM prediction (mock implementation)
        
        In a real implementation, this would call the actual LLM model
        with the generated prompt and parse the response.
        
        Args:
            prompt: The prediction prompt
            
        Returns:
            Dict containing the LLM response
        """
        import random
        
        # Generate random but realistic prediction
        numbers = sorted(random.sample(range(1, 41), 6))
        confidence = random.uniform(0.6, 0.9)
        
        # Generate realistic explanation
        explanations = [
            "Based on frequency analysis and recent trends",
            "Considering hot numbers and gap patterns",
            "Balancing statistical patterns with recent draws",
            "Following number distribution principles"
        ]
        
        key_factors = [
            "Historical frequency patterns",
            "Recent draw analysis",
            "Number gap identification",
            "Trend analysis"
        ]
        
        return {
            "numbers": numbers,
            "confidence_score": confidence,
            "explanation": random.choice(explanations),
            "key_factors": random.sample(key_factors, 3)
        }
    
    async def predict(self, request: LocalLlmRequest) -> List[LocalLlmPredictionResult]:
        """
        Generate lottery predictions using the local LLM
        
        Args:
            request: Prediction request with historical data and parameters
            
        Returns:
            List of prediction results
        """
        try:
            # Validate service is available
            service = self._get_service()
            
            # Update last prediction time
            service.last_prediction_at = datetime.now()
            
            # Analyze historical patterns
            logger.info(f"Analyzing {len(request.historical_data)} historical draws")
            patterns = self._analyze_historical_patterns(request.historical_data)
            
            # Generate predictions
            predictions = []
            
            for i in range(request.prediction_count):
                try:
                    # Generate prompt for this prediction
                    prompt = self._generate_prediction_prompt(
                        request.historical_data,
                        request.performance_metrics,
                        patterns
                    )
                    
                    # Get prediction from LLM (simulated)
                    llm_response = self._simulate_llm_prediction(prompt)
                    
                    # Create prediction result
                    prediction = LocalLlmPredictionResult(
                        numbers=llm_response["numbers"],
                        confidence_score=llm_response["confidence_score"],
                        explanation=llm_response["explanation"],
                        key_factors=llm_response["key_factors"],
                        metadata={
                            "prediction_index": i + 1,
                            "total_predictions": request.prediction_count,
                            "historical_draws_used": len(request.historical_data),
                            "performance_metrics_used": len(request.performance_metrics),
                            "target_draw_date": request.target_draw_date.isoformat() if request.target_draw_date else None,
                            "generated_at": datetime.now().isoformat(),
                            "patterns_analyzed": {
                                "hot_numbers": patterns.get("hot_numbers", [])[:5],
                                "cold_numbers": patterns.get("cold_numbers", [])[:5],
                                "trending_up": patterns.get("trending_up", [])
                            }
                        }
                    )
                    
                    predictions.append(prediction)
                    logger.info(f"Generated prediction {i+1}/{request.prediction_count}: {prediction.numbers}")
                    
                except Exception as e:
                    logger.error(f"Error generating prediction {i+1}: {e}")
                    # Continue with other predictions
                    continue
            
            if not predictions:
                raise RuntimeError("Failed to generate any predictions")
            
            logger.info(f"Successfully generated {len(predictions)} predictions")
            return predictions
            
        except Exception as e:
            logger.error(f"Error in prediction process: {e}")
            raise RuntimeError(f"Prediction failed: {str(e)}")
    
    async def validate_request(self, request: LocalLlmRequest) -> Tuple[bool, str]:
        """
        Validate a prediction request
        
        Args:
            request: The prediction request to validate
            
        Returns:
            Tuple[bool, str]: (is_valid, error_message)
        """
        try:
            # Check service availability
            service = self._get_service()
            
            # Validate historical data
            if len(request.historical_data) < 10:
                return False, "Need at least 10 historical draws for reliable prediction"
            
            # Validate prediction count
            if not (1 <= request.prediction_count <= 10):
                return False, "Prediction count must be between 1 and 10"
            
            # Validate historical data quality
            unique_draws = set(draw.draw for draw in request.historical_data)
            if len(unique_draws) != len(request.historical_data):
                return False, "Historical data contains duplicate draws"
            
            return True, "Request is valid"
            
        except Exception as e:
            return False, f"Validation error: {str(e)}"


# Global predictor instance
_predictor_instance: Optional[LocalLlmPredictor] = None


def get_predictor_instance() -> LocalLlmPredictor:
    """Get or create the global predictor instance"""
    global _predictor_instance
    if _predictor_instance is None:
        _predictor_instance = LocalLlmPredictor()
    return _predictor_instance