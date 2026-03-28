"""
FastAPI service with LangSmith integration example

This is an enhanced version of main.py showing how to integrate LangSmith tracing.
You can either replace main.py with this, or use it as a reference.
"""

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
from typing import List, Optional
import logging
import asyncio
import os

# Import LangSmith helper
from langsmith_helper import langsmith, trace_prediction, setup_langsmith_env

# Setup LangSmith environment at startup
setup_langsmith_env()

from blended_predictor import BlendedPredictor

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize the blended predictor
predictor = BlendedPredictor()

app = FastAPI(
    title="PredictLottoNZ Prediction Service",
    description="FastAPI service for generating lottery number predictions using ML and GPT models with LangSmith observability",
    version="1.0.0"
)

# Configure CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, specify actual origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Pydantic models for request and response
class PredictRequest(BaseModel):
    weekly_numbers: List[float] = Field(
        ..., 
        description="Weekly lottery numbers for prediction analysis",
        json_schema_extra={"example": [5.0, 12.0, 18.0, 20.0, 33.0, 40.0]}
    )

class PredictResponse(BaseModel):
    ml_prediction: float = Field(
        ...,
        description="Machine learning model prediction",
        json_schema_extra={"example": 25.5}
    )
    gpt_prediction: float = Field(
        ...,
        description="GPT-based prediction",
        json_schema_extra={"example": 28.3}
    )
    blended_prediction: float = Field(
        ...,
        description="Blended prediction combining ML and GPT results",
        json_schema_extra={"example": 26.9}
    )
    langsmith_trace_url: Optional[str] = Field(
        None,
        description="LangSmith trace URL for debugging (if enabled)"
    )

@app.on_event("startup")
async def startup_event():
    """Log startup information"""
    logger.info("Starting PredictLottoNZ Prediction Service")
    if langsmith.enabled:
        logger.info(f"✓ LangSmith tracing enabled for project: {langsmith.project_name}")
    else:
        logger.info("ℹ LangSmith tracing disabled")

@app.get("/")
async def root():
    """Health check endpoint"""
    return {
        "message": "PredictLottoNZ Prediction Service is running",
        "langsmith_enabled": langsmith.enabled,
        "project": langsmith.project_name if langsmith.enabled else None
    }

@app.get("/health")
async def health_check():
    """Health check endpoint for monitoring"""
    return {
        "status": "healthy",
        "service": "prediction-service",
        "langsmith_enabled": langsmith.enabled
    }

@app.post("/train")
@trace_prediction(name="train_ml_model", tags=["training"])
async def train_model(historical_data: List[List[float]]):
    """
    Train the ML model with historical lottery data
    
    Args:
        historical_data: List of historical number combinations
        
    Returns:
        Training status
    """
    try:
        logger.info(f"Received training request with {len(historical_data)} historical records")
        
        # Add metadata to LangSmith trace
        langsmith.add_metadata({
            "operation": "training",
            "data_points": len(historical_data),
            "model_type": "ml"
        })
        
        # Validate historical data
        for i, combination in enumerate(historical_data):
            if len(combination) != 6:
                raise HTTPException(
                    status_code=400,
                    detail=f"Historical combination {i} must contain exactly 6 numbers"
                )
            
            for num in combination:
                if not (1 <= num <= 40):
                    raise HTTPException(
                        status_code=400,
                        detail=f"All numbers in combination {i} must be between 1 and 40"
                    )
        
        # Train the ML model
        predictor.train_ml_model(historical_data)
        
        # Add success metadata
        langsmith.add_metadata({
            "training_status": "success",
            "combinations_trained": len(historical_data)
        })
        langsmith.add_tags(["success"])
        
        return {
            "status": "success",
            "message": f"ML model trained with {len(historical_data)} historical combinations"
        }
        
    except HTTPException as e:
        langsmith.log_error(e, {"operation": "training", "status_code": e.status_code})
        raise
    except Exception as e:
        logger.error(f"Error training model: {str(e)}")
        langsmith.log_error(e, {"operation": "training"})
        raise HTTPException(status_code=500, detail="Internal server error during training")

@app.post("/predict", response_model=PredictResponse)
@trace_prediction(name="generate_lottery_prediction", tags=["prediction", "production"])
async def predict(request: PredictRequest):
    """
    Generate lottery number predictions using ML and GPT models
    
    Args:
        request: PredictRequest containing weekly numbers for analysis
        
    Returns:
        PredictResponse with ML, GPT, and blended predictions
    """
    try:
        logger.info(f"Received prediction request with weekly_numbers: {request.weekly_numbers}")
        
        # Add request metadata to LangSmith trace
        langsmith.add_metadata({
            "operation": "prediction",
            "input_numbers": request.weekly_numbers,
            "number_count": len(request.weekly_numbers)
        })
        
        # Validate input
        if len(request.weekly_numbers) != 6:
            raise HTTPException(
                status_code=400, 
                detail="weekly_numbers must contain exactly 6 numbers"
            )
        
        # Validate number range (1-40 for NZ Lotto)
        for num in request.weekly_numbers:
            if not (1 <= num <= 40):
                raise HTTPException(
                    status_code=400,
                    detail="All numbers must be between 1 and 40"
                )
        
        # Generate predictions using ML and GPT models
        ml_pred, gpt_pred, blended_pred = await predictor.predict(request.weekly_numbers)
        
        # Log prediction results to LangSmith
        langsmith.log_prediction(
            prediction_numbers=[ml_pred, gpt_pred, blended_pred],
            confidence_score=None,  # Add if available
            model_name="blended",
            metadata={
                "ml_prediction": ml_pred,
                "gpt_prediction": gpt_pred,
                "blended_prediction": blended_pred,
                "input_numbers": request.weekly_numbers
            }
        )
        langsmith.add_tags(["success"])
        
        response = PredictResponse(
            ml_prediction=ml_pred,
            gpt_prediction=gpt_pred,
            blended_prediction=blended_pred,
            langsmith_trace_url=f"https://smith.langchain.com/o/default/projects/p/{langsmith.project_name}" if langsmith.enabled else None
        )
        
        logger.info(f"Generated predictions: ML={ml_pred}, GPT={gpt_pred}, Blended={blended_pred}")
        return response
        
    except HTTPException as e:
        langsmith.log_error(e, {
            "operation": "prediction",
            "status_code": e.status_code,
            "input_numbers": request.weekly_numbers
        })
        raise
    except Exception as e:
        logger.error(f"Error generating predictions: {str(e)}")
        langsmith.log_error(e, {
            "operation": "prediction",
            "input_numbers": request.weekly_numbers
        })
        raise HTTPException(status_code=500, detail="Internal server error during prediction")

@app.post("/feedback")
async def submit_feedback(
    run_id: str,
    accuracy_score: float,
    matches: int,
    comment: str = None
):
    """
    Submit feedback for a prediction run
    
    Args:
        run_id: LangSmith run ID from the prediction trace
        accuracy_score: Score between 0 and 1
        matches: Number of matching numbers
        comment: Optional comment
        
    Returns:
        Feedback submission status
    """
    if not langsmith.enabled:
        return {
            "status": "disabled",
            "message": "LangSmith is not enabled"
        }
    
    try:
        langsmith.create_feedback(
            run_id=run_id,
            key="accuracy",
            score=accuracy_score,
            comment=comment or f"Matched {matches}/6 numbers"
        )
        
        return {
            "status": "success",
            "message": f"Feedback recorded for run {run_id}"
        }
    except Exception as e:
        logger.error(f"Error submitting feedback: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to submit feedback")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8001)
