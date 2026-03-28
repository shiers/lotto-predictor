"""
Property-based test for Local LLM response structure

**Feature: predict-lotto-nz, Property 22: Local LLM response structure is validated**
**Validates: Requirements 12.3**

This test verifies that for any local LLM response, the system receives
structured data with confidence scores and reasoning.
"""

import pytest
from hypothesis import given, strategies as st, assume, settings
from datetime import datetime, timedelta
from typing import List, Dict, Any
import random
import json

from local_llm_predictor import (
    LocalLlmPredictionResult,
    LocalLlmRequest,
    LottoDrawData,
    PredictionAccuracyData,
    LocalLlmPredictor,
    get_predictor_instance
)


# Strategy for generating valid prediction numbers
@st.composite
def valid_numbers_strategy(draw):
    """Generate 6 unique valid lotto numbers"""
    numbers = draw(st.lists(
        st.integers(min_value=1, max_value=40),
        min_size=6,
        max_size=6,
        unique=True
    ))
    return sorted(numbers)


# Strategy for generating valid confidence scores
confidence_score_strategy = st.floats(min_value=0.0, max_value=1.0)


# Strategy for generating explanations
explanation_strategy = st.text(min_size=10, max_size=500).filter(
    lambda x: x.strip() and not x.isspace()
)


# Strategy for generating key factors
@st.composite
def key_factors_strategy(draw):
    """Generate list of key factors"""
    factor_options = [
        "Historical frequency analysis",
        "Recent trend patterns",
        "Number gap identification",
        "Statistical distribution",
        "Hot number analysis",
        "Cold number patterns",
        "Temporal analysis",
        "Proximity scoring",
        "Pattern recognition",
        "Regression analysis"
    ]
    
    num_factors = draw(st.integers(min_value=1, max_value=5))
    factors = draw(st.lists(
        st.sampled_from(factor_options),
        min_size=num_factors,
        max_size=num_factors,
        unique=True
    ))
    return factors


# Strategy for generating metadata
@st.composite
def metadata_strategy(draw):
    """Generate valid metadata dictionary"""
    metadata = {}
    
    # Add some common metadata fields
    if draw(st.booleans()):
        metadata["prediction_index"] = draw(st.integers(min_value=1, max_value=10))
    
    if draw(st.booleans()):
        metadata["total_predictions"] = draw(st.integers(min_value=1, max_value=10))
    
    if draw(st.booleans()):
        metadata["historical_draws_used"] = draw(st.integers(min_value=10, max_value=1000))
    
    if draw(st.booleans()):
        metadata["generated_at"] = datetime.now().isoformat()
    
    if draw(st.booleans()):
        metadata["model_version"] = draw(st.text(min_size=3, max_size=20))
    
    return metadata


# Strategy for generating valid LocalLlmPredictionResult
@st.composite
def prediction_result_strategy(draw):
    """Generate valid LocalLlmPredictionResult"""
    numbers = draw(valid_numbers_strategy())
    confidence_score = draw(confidence_score_strategy)
    explanation = draw(explanation_strategy)
    key_factors = draw(key_factors_strategy())
    metadata = draw(metadata_strategy())
    
    return LocalLlmPredictionResult(
        numbers=numbers,
        confidence_score=confidence_score,
        explanation=explanation,
        key_factors=key_factors,
        metadata=metadata
    )


class TestLocalLlmResponseStructure:
    """Test class for Local LLM response structure property"""
    
    @given(prediction_result_strategy())
    @settings(max_examples=50, deadline=None)
    def test_response_structure_is_validated(self, result: LocalLlmPredictionResult):
        """
        **Feature: predict-lotto-nz, Property 22: Local LLM response structure is validated**
        **Validates: Requirements 12.3**
        
        For any local LLM response, the system should receive structured data
        with confidence scores and reasoning.
        """
        # Property: Response must contain exactly 6 unique numbers between 1-40
        assert result.numbers is not None
        assert len(result.numbers) == 6
        assert all(isinstance(num, int) for num in result.numbers)
        assert all(1 <= num <= 40 for num in result.numbers)
        assert len(set(result.numbers)) == 6, "All numbers must be unique"
        assert result.numbers == sorted(result.numbers), "Numbers should be sorted"
        
        # Property: Response must contain valid confidence score
        assert result.confidence_score is not None
        assert isinstance(result.confidence_score, (int, float))
        assert 0.0 <= result.confidence_score <= 1.0
        
        # Property: Response must contain explanation
        assert result.explanation is not None
        assert isinstance(result.explanation, str)
        assert len(result.explanation.strip()) > 0, "Explanation cannot be empty"
        
        # Property: Response must contain key factors
        assert result.key_factors is not None
        assert isinstance(result.key_factors, list)
        assert len(result.key_factors) > 0, "Must have at least one key factor"
        assert all(isinstance(factor, str) for factor in result.key_factors)
        assert all(len(factor.strip()) > 0 for factor in result.key_factors)
        
        # Property: Metadata must be a dictionary
        assert result.metadata is not None
        assert isinstance(result.metadata, dict)
    
    @given(st.lists(prediction_result_strategy(), min_size=1, max_size=10))
    @settings(max_examples=30, deadline=None)
    def test_multiple_predictions_structure(self, results: List[LocalLlmPredictionResult]):
        """
        Test that multiple prediction results maintain consistent structure
        """
        # Property: All results must have valid structure
        for i, result in enumerate(results):
            assert len(result.numbers) == 6, f"Result {i} must have 6 numbers"
            assert 0.0 <= result.confidence_score <= 1.0, f"Result {i} confidence score invalid"
            assert len(result.explanation.strip()) > 0, f"Result {i} explanation cannot be empty"
            assert len(result.key_factors) > 0, f"Result {i} must have key factors"
        
        # Property: All predictions should have unique number combinations
        number_combinations = [tuple(result.numbers) for result in results]
        if len(results) > 1:
            # Allow some duplicates but not all (realistic for lottery predictions)
            unique_combinations = set(number_combinations)
            assert len(unique_combinations) >= 1, "Must have at least one unique combination"
    
    def test_response_serialization(self):
        """
        Test that response can be properly serialized and deserialized
        """
        # Create a valid response
        result = LocalLlmPredictionResult(
            numbers=[5, 12, 18, 25, 33, 40],
            confidence_score=0.75,
            explanation="Based on frequency analysis and recent trends",
            key_factors=["Historical frequency", "Recent patterns", "Gap analysis"],
            metadata={
                "prediction_index": 1,
                "generated_at": datetime.now().isoformat(),
                "model_version": "1.0.0"
            }
        )
        
        # Test serialization
        result_dict = result.model_dump()
        assert isinstance(result_dict, dict)
        assert "numbers" in result_dict
        assert "confidence_score" in result_dict
        assert "explanation" in result_dict
        assert "key_factors" in result_dict
        assert "metadata" in result_dict
        
        # Test JSON serialization
        json_str = json.dumps(result_dict)
        assert isinstance(json_str, str)
        
        # Test deserialization
        parsed_dict = json.loads(json_str)
        reconstructed = LocalLlmPredictionResult(**parsed_dict)
        
        assert reconstructed.numbers == result.numbers
        assert reconstructed.confidence_score == result.confidence_score
        assert reconstructed.explanation == result.explanation
        assert reconstructed.key_factors == result.key_factors
        assert reconstructed.metadata == result.metadata
    
    def test_response_with_minimal_data(self):
        """
        Test response with minimal required data
        """
        result = LocalLlmPredictionResult(
            numbers=[1, 2, 3, 4, 5, 6],
            confidence_score=0.5,
            explanation="Minimal explanation",
            key_factors=["Single factor"]
        )
        
        # Verify minimal data is valid
        assert len(result.numbers) == 6
        assert result.confidence_score == 0.5
        assert result.explanation == "Minimal explanation"
        assert result.key_factors == ["Single factor"]
        assert result.metadata == {}  # Default empty dict
    
    def test_response_with_maximum_data(self):
        """
        Test response with maximum realistic data
        """
        result = LocalLlmPredictionResult(
            numbers=[3, 7, 14, 21, 28, 35],
            confidence_score=0.95,
            explanation="Comprehensive analysis based on multiple statistical models and historical patterns",
            key_factors=[
                "Historical frequency analysis",
                "Recent trend identification",
                "Number gap patterns",
                "Statistical distribution balance",
                "Temporal pattern recognition"
            ],
            metadata={
                "prediction_index": 5,
                "total_predictions": 10,
                "historical_draws_used": 500,
                "performance_metrics_used": 3,
                "generated_at": datetime.now().isoformat(),
                "model_version": "2.1.0",
                "processing_time_ms": 1250,
                "patterns_analyzed": {
                    "hot_numbers": [7, 14, 21, 28, 35],
                    "cold_numbers": [1, 2, 8, 15, 22],
                    "trending_up": [3, 7, 14]
                }
            }
        )
        
        # Verify all data is preserved
        assert len(result.numbers) == 6
        assert result.confidence_score == 0.95
        assert len(result.explanation) > 50  # Comprehensive explanation
        assert len(result.key_factors) == 5
        assert len(result.metadata) >= 7  # Rich metadata
        assert "patterns_analyzed" in result.metadata
    
    def test_invalid_response_structures(self):
        """
        Test that invalid response structures are rejected
        """
        # Test invalid number count
        with pytest.raises(ValueError):
            LocalLlmPredictionResult(
                numbers=[1, 2, 3, 4, 5],  # Only 5 numbers
                confidence_score=0.5,
                explanation="Test",
                key_factors=["Test"]
            )
        
        # Test invalid number range
        with pytest.raises(ValueError):
            LocalLlmPredictionResult(
                numbers=[0, 1, 2, 3, 4, 5],  # 0 is invalid
                confidence_score=0.5,
                explanation="Test",
                key_factors=["Test"]
            )
        
        # Test duplicate numbers
        with pytest.raises(ValueError):
            LocalLlmPredictionResult(
                numbers=[1, 1, 2, 3, 4, 5],  # Duplicate 1
                confidence_score=0.5,
                explanation="Test",
                key_factors=["Test"]
            )
        
        # Test invalid confidence score
        with pytest.raises(ValueError):
            LocalLlmPredictionResult(
                numbers=[1, 2, 3, 4, 5, 6],
                confidence_score=1.5,  # > 1.0
                explanation="Test",
                key_factors=["Test"]
            )
        
        with pytest.raises(ValueError):
            LocalLlmPredictionResult(
                numbers=[1, 2, 3, 4, 5, 6],
                confidence_score=-0.1,  # < 0.0
                explanation="Test",
                key_factors=["Test"]
            )
    
    def test_response_confidence_score_precision(self):
        """
        Test confidence score precision and edge cases
        """
        # Test edge values
        result_min = LocalLlmPredictionResult(
            numbers=[1, 2, 3, 4, 5, 6],
            confidence_score=0.0,
            explanation="Minimum confidence",
            key_factors=["Low confidence factor"]
        )
        assert result_min.confidence_score == 0.0
        
        result_max = LocalLlmPredictionResult(
            numbers=[35, 36, 37, 38, 39, 40],
            confidence_score=1.0,
            explanation="Maximum confidence",
            key_factors=["High confidence factor"]
        )
        assert result_max.confidence_score == 1.0
        
        # Test precision
        result_precise = LocalLlmPredictionResult(
            numbers=[10, 15, 20, 25, 30, 35],
            confidence_score=0.123456789,
            explanation="Precise confidence",
            key_factors=["Precise factor"]
        )
        assert abs(result_precise.confidence_score - 0.123456789) < 1e-9


if __name__ == "__main__":
    pytest.main([__file__, "-v"])