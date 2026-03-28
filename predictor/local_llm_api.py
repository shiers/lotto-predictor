"""
Local LLM API Endpoints

This module provides FastAPI endpoints for the local LLM service,
including prediction requests and model management operations.
"""

import logging
from typing import Dict, Any, Optional, List
from fastapi import APIRouter, HTTPException, BackgroundTasks
from pydantic import BaseModel, Field

from local_llm_service import (
    LocalLlmService, 
    ModelConfig, 
    HealthStatus,
    get_service_instance,
    initialize_service,
    get_default_config
)
from local_llm_predictor import (
    LocalLlmRequest,
    LocalLlmPredictionResult,
    get_predictor_instance
)
from model_hot_swap import (
    ModelSwapRequest,
    ModelSwapResult,
    ModelVersion,
    get_hot_swap_manager
)
from local_llm_retraining import (
    LocalLlmRetrainingService,
    TrainingDataset,
    TrainingConfig,
    TrainingResult,
    TrainingStatus,
    get_retraining_service
)

# Configure logging
logger = logging.getLogger(__name__)

# Create API router
router = APIRouter(prefix="/local-llm", tags=["Local LLM"])


class ModelLoadRequest(BaseModel):
    """Request to load a model"""
    model_path: Optional[str] = Field(None, description="Path to model, uses config default if not provided")


class ModelLoadResponse(BaseModel):
    """Response for model loading"""
    success: bool = Field(..., description="Whether the operation was successful")
    message: str = Field(..., description="Status message")
    model_name: Optional[str] = Field(None, description="Name of the loaded model")


class ModelCompatibilityRequest(BaseModel):
    """Request to validate model compatibility"""
    model_path: str = Field(..., description="Path to model to validate")


class ModelCompatibilityResponse(BaseModel):
    """Response for model compatibility validation"""
    is_compatible: bool = Field(..., description="Whether the model is compatible")
    message: str = Field(..., description="Validation message")


class ServiceInitRequest(BaseModel):
    """Request to initialize the service"""
    config: Optional[ModelConfig] = Field(None, description="Service configuration, uses defaults if not provided")


class ServiceInitResponse(BaseModel):
    """Response for service initialization"""
    success: bool = Field(..., description="Whether initialization was successful")
    message: str = Field(..., description="Status message")
    config: ModelConfig = Field(..., description="Applied configuration")


class PredictionResponse(BaseModel):
    """Response for prediction requests"""
    success: bool = Field(..., description="Whether prediction was successful")
    predictions: List[LocalLlmPredictionResult] = Field(..., description="Generated predictions")
    message: str = Field(..., description="Status message")


class ValidationResponse(BaseModel):
    """Response for request validation"""
    is_valid: bool = Field(..., description="Whether the request is valid")
    message: str = Field(..., description="Validation message")


class SwapStatusResponse(BaseModel):
    """Response for swap status queries"""
    found: bool = Field(..., description="Whether the swap was found")
    swap_result: Optional[ModelSwapResult] = Field(None, description="Swap result if found")


class TrainingRequest(BaseModel):
    """Request to start model retraining"""
    dataset: TrainingDataset = Field(..., description="Training dataset")
    config: Optional[TrainingConfig] = Field(None, description="Training configuration, uses defaults if not provided")


class TrainingResponse(BaseModel):
    """Response for training requests"""
    success: bool = Field(..., description="Whether training was started successfully")
    training_id: str = Field(..., description="Unique training identifier")
    message: str = Field(..., description="Status message")


class TrainingStatusResponse(BaseModel):
    """Response for training status queries"""
    found: bool = Field(..., description="Whether the training was found")
    training_result: Optional[TrainingResult] = Field(None, description="Training result if found")


class ModelValidationRequest(BaseModel):
    """Request to validate a trained model"""
    model_path: str = Field(..., description="Path to the trained model")


class ModelValidationResponse(BaseModel):
    """Response for model validation"""
    validation_results: Dict[str, Any] = Field(..., description="Validation results")


class ModelDeploymentRequest(BaseModel):
    """Request to deploy a trained model"""
    model_path: str = Field(..., description="Path to the trained model")
    model_name: str = Field(..., description="Name for the deployed model")


class ModelDeploymentResponse(BaseModel):
    """Response for model deployment"""
    success: bool = Field(..., description="Whether deployment was successful")
    message: str = Field(..., description="Status message")


@router.post("/init", response_model=ServiceInitResponse)
async def initialize_llm_service(request: ServiceInitRequest):
    """
    Initialize the local LLM service
    
    Args:
        request: Service initialization request
        
    Returns:
        ServiceInitResponse: Initialization result
    """
    try:
        # Use provided config or default
        config = request.config or get_default_config()
        
        # Initialize service
        service = initialize_service(config)
        
        logger.info(f"Local LLM service initialized with config: {config.model_name}")
        
        return ServiceInitResponse(
            success=True,
            message="Local LLM service initialized successfully",
            config=config
        )
        
    except Exception as e:
        logger.error(f"Failed to initialize service: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Failed to initialize service: {str(e)}"
        )


@router.post("/load-model", response_model=ModelLoadResponse)
async def load_model(request: ModelLoadRequest, background_tasks: BackgroundTasks):
    """
    Load a model into the LLM service
    
    Args:
        request: Model loading request
        background_tasks: FastAPI background tasks
        
    Returns:
        ModelLoadResponse: Loading result
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized. Call /init first."
            )
        
        # Validate model if path provided
        if request.model_path:
            is_compatible, validation_message = service.validate_model_compatibility(request.model_path)
            if not is_compatible:
                return ModelLoadResponse(
                    success=False,
                    message=f"Model validation failed: {validation_message}"
                )
        
        # Load model
        success = await service.load_model(request.model_path)
        
        if success:
            metadata = service.get_model_metadata()
            model_name = metadata.model_name if metadata else "unknown"
            
            return ModelLoadResponse(
                success=True,
                message="Model loaded successfully",
                model_name=model_name
            )
        else:
            return ModelLoadResponse(
                success=False,
                message="Failed to load model"
            )
            
    except Exception as e:
        logger.error(f"Error loading model: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error loading model: {str(e)}"
        )


@router.post("/unload-model", response_model=ModelLoadResponse)
async def unload_model():
    """
    Unload the current model
    
    Returns:
        ModelLoadResponse: Unloading result
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        success = await service.unload_model()
        
        return ModelLoadResponse(
            success=success,
            message="Model unloaded successfully" if success else "Failed to unload model"
        )
        
    except Exception as e:
        logger.error(f"Error unloading model: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error unloading model: {str(e)}"
        )


@router.get("/health", response_model=HealthStatus)
async def get_health():
    """
    Get health status of the LLM service
    
    Returns:
        HealthStatus: Current health status
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=503,
                detail="Service not initialized"
            )
        
        health_status = await service.get_health_status()
        return health_status
        
    except Exception as e:
        logger.error(f"Error getting health status: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error getting health status: {str(e)}"
        )


@router.get("/model-info")
async def get_model_info() -> Dict[str, Any]:
    """
    Get detailed information about the current model
    
    Returns:
        Dict containing model information
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        model_info = await service.get_model_info()
        return model_info
        
    except Exception as e:
        logger.error(f"Error getting model info: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error getting model info: {str(e)}"
        )


@router.post("/validate-model", response_model=ModelCompatibilityResponse)
async def validate_model(request: ModelCompatibilityRequest):
    """
    Validate model compatibility
    
    Args:
        request: Model compatibility validation request
        
    Returns:
        ModelCompatibilityResponse: Validation result
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        is_compatible, message = service.validate_model_compatibility(request.model_path)
        
        return ModelCompatibilityResponse(
            is_compatible=is_compatible,
            message=message
        )
        
    except Exception as e:
        logger.error(f"Error validating model: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error validating model: {str(e)}"
        )


@router.post("/predict", response_model=PredictionResponse)
async def predict_numbers(request: LocalLlmRequest):
    """
    Generate lottery number predictions using the local LLM
    
    Args:
        request: Prediction request with historical data and parameters
        
    Returns:
        PredictionResponse: Generated predictions
    """
    try:
        predictor = get_predictor_instance()
        
        # Validate request
        is_valid, validation_message = await predictor.validate_request(request)
        if not is_valid:
            return PredictionResponse(
                success=False,
                predictions=[],
                message=f"Invalid request: {validation_message}"
            )
        
        # Generate predictions
        predictions = await predictor.predict(request)
        
        return PredictionResponse(
            success=True,
            predictions=predictions,
            message=f"Successfully generated {len(predictions)} predictions"
        )
        
    except Exception as e:
        logger.error(f"Error generating predictions: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error generating predictions: {str(e)}"
        )


@router.post("/validate-request", response_model=ValidationResponse)
async def validate_prediction_request(request: LocalLlmRequest):
    """
    Validate a prediction request without generating predictions
    
    Args:
        request: Prediction request to validate
        
    Returns:
        ValidationResponse: Validation result
    """
    try:
        predictor = get_predictor_instance()
        
        is_valid, message = await predictor.validate_request(request)
        
        return ValidationResponse(
            is_valid=is_valid,
            message=message
        )
        
    except Exception as e:
        logger.error(f"Error validating request: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error validating request: {str(e)}"
        )


@router.post("/hot-swap", response_model=ModelSwapResult)
async def hot_swap_model(request: ModelSwapRequest, background_tasks: BackgroundTasks):
    """
    Perform hot model swap with zero downtime
    
    Args:
        request: Model swap request
        background_tasks: FastAPI background tasks
        
    Returns:
        ModelSwapResult: Result of the swap operation
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        
        # Perform the swap
        swap_result = await hot_swap_manager.hot_swap_model(request)
        
        return swap_result
        
    except Exception as e:
        logger.error(f"Error performing hot swap: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error performing hot swap: {str(e)}"
        )


@router.get("/swap-status/{swap_id}", response_model=SwapStatusResponse)
async def get_swap_status(swap_id: str):
    """
    Get status of a model swap operation
    
    Args:
        swap_id: Unique identifier of the swap operation
        
    Returns:
        SwapStatusResponse: Swap status information
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        swap_result = hot_swap_manager.get_swap_status(swap_id)
        
        return SwapStatusResponse(
            found=swap_result is not None,
            swap_result=swap_result
        )
        
    except Exception as e:
        logger.error(f"Error getting swap status: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error getting swap status: {str(e)}"
        )


@router.get("/model-versions", response_model=List[ModelVersion])
async def get_model_versions():
    """
    Get all model versions
    
    Returns:
        List[ModelVersion]: All model versions
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        versions = hot_swap_manager.get_model_versions()
        
        return versions
        
    except Exception as e:
        logger.error(f"Error getting model versions: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error getting model versions: {str(e)}"
        )


@router.get("/active-version", response_model=Optional[ModelVersion])
async def get_active_version():
    """
    Get currently active model version
    
    Returns:
        Optional[ModelVersion]: Active model version if any
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        active_version = hot_swap_manager.get_active_version()
        
        return active_version
        
    except Exception as e:
        logger.error(f"Error getting active version: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error getting active version: {str(e)}"
        )


@router.get("/swap-history")
async def get_swap_history(limit: int = 10) -> List[ModelSwapResult]:
    """
    Get recent model swap history
    
    Args:
        limit: Maximum number of results to return
        
    Returns:
        List[ModelSwapResult]: Recent swap operations
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        history = hot_swap_manager.get_swap_history(limit)
        
        return history
        
    except Exception as e:
        logger.error(f"Error getting swap history: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error getting swap history: {str(e)}"
        )


@router.get("/status")
async def get_service_status() -> Dict[str, Any]:
    """
    Get basic service status
    
    Returns:
        Dict containing service status
    """
    try:
        service = get_service_instance()
        
        if not service:
            return {
                "initialized": False,
                "message": "Service not initialized"
            }
        
        health = await service.get_health_status()
        
        # Get hot swap manager info
        try:
            hot_swap_manager = get_hot_swap_manager(service)
            active_version = hot_swap_manager.get_active_version()
            active_swaps = len(hot_swap_manager.active_swaps)
        except:
            active_version = None
            active_swaps = 0
        
        return {
            "initialized": True,
            "status": health.status,
            "model_loaded": health.model_loaded,
            "model_name": health.model_name,
            "uptime_seconds": health.uptime_seconds,
            "active_version": active_version.version_id if active_version else None,
            "active_swaps": active_swaps
        }
        
    except Exception as e:
        logger.error(f"Error getting service status: {e}")
        return {
            "initialized": False,
            "error": str(e)
        }


# Model Retraining Endpoints

@router.post("/retrain", response_model=TrainingResponse)
async def start_model_retraining(request: TrainingRequest, background_tasks: BackgroundTasks):
    """
    Start model retraining process
    
    Args:
        request: Training request with dataset and configuration
        background_tasks: FastAPI background tasks
        
    Returns:
        TrainingResponse: Training operation result
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        retraining_service = get_retraining_service(service, hot_swap_manager)
        
        # Use provided config or default
        config = request.config or TrainingConfig()
        
        # Start training in background
        async def run_training():
            try:
                training_result = await retraining_service.start_training(request.dataset, config)
                logger.info(f"Training {training_result.training_id} completed with status: {training_result.status}")
            except Exception as e:
                logger.error(f"Background training failed: {e}")
        
        background_tasks.add_task(run_training)
        
        # Generate training ID for immediate response
        training_id = retraining_service._generate_training_id()
        
        return TrainingResponse(
            success=True,
            training_id=training_id,
            message="Model retraining started successfully"
        )
        
    except Exception as e:
        logger.error(f"Error starting retraining: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error starting retraining: {str(e)}"
        )


@router.get("/training-status/{training_id}", response_model=TrainingStatusResponse)
async def get_training_status(training_id: str):
    """
    Get status of a training operation
    
    Args:
        training_id: Unique identifier of the training operation
        
    Returns:
        TrainingStatusResponse: Training status information
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        retraining_service = get_retraining_service(service, hot_swap_manager)
        
        training_result = retraining_service.get_training_status(training_id)
        
        return TrainingStatusResponse(
            found=training_result is not None,
            training_result=training_result
        )
        
    except Exception as e:
        logger.error(f"Error getting training status: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error getting training status: {str(e)}"
        )


@router.get("/active-trainings", response_model=List[TrainingResult])
async def get_active_trainings():
    """
    Get all currently active training operations
    
    Returns:
        List[TrainingResult]: Active training operations
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        retraining_service = get_retraining_service(service, hot_swap_manager)
        
        active_trainings = retraining_service.get_active_trainings()
        return active_trainings
        
    except Exception as e:
        logger.error(f"Error getting active trainings: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error getting active trainings: {str(e)}"
        )


@router.get("/training-history")
async def get_training_history(limit: int = 10) -> List[TrainingResult]:
    """
    Get recent training history
    
    Args:
        limit: Maximum number of results to return
        
    Returns:
        List[TrainingResult]: Recent training operations
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        retraining_service = get_retraining_service(service, hot_swap_manager)
        
        history = retraining_service.get_training_history(limit)
        return history
        
    except Exception as e:
        logger.error(f"Error getting training history: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error getting training history: {str(e)}"
        )


@router.post("/cancel-training/{training_id}")
async def cancel_training(training_id: str) -> Dict[str, Any]:
    """
    Cancel an active training operation
    
    Args:
        training_id: Unique identifier of the training operation
        
    Returns:
        Dict containing cancellation result
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        retraining_service = get_retraining_service(service, hot_swap_manager)
        
        success = retraining_service.cancel_training(training_id)
        
        return {
            "success": success,
            "message": f"Training {training_id} cancelled" if success else f"Training {training_id} not found or already completed"
        }
        
    except Exception as e:
        logger.error(f"Error cancelling training: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error cancelling training: {str(e)}"
        )


@router.post("/validate-trained-model", response_model=ModelValidationResponse)
async def validate_trained_model(request: ModelValidationRequest):
    """
    Validate a trained model before deployment
    
    Args:
        request: Model validation request
        
    Returns:
        ModelValidationResponse: Validation results
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        retraining_service = get_retraining_service(service, hot_swap_manager)
        
        validation_results = await retraining_service.validate_trained_model(request.model_path)
        
        return ModelValidationResponse(
            validation_results=validation_results
        )
        
    except Exception as e:
        logger.error(f"Error validating trained model: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error validating trained model: {str(e)}"
        )


@router.post("/deploy-trained-model", response_model=ModelDeploymentResponse)
async def deploy_trained_model(request: ModelDeploymentRequest, background_tasks: BackgroundTasks):
    """
    Deploy a trained model using hot-swap functionality
    
    Args:
        request: Model deployment request
        background_tasks: FastAPI background tasks
        
    Returns:
        ModelDeploymentResponse: Deployment result
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        retraining_service = get_retraining_service(service, hot_swap_manager)
        
        # Deploy model in background
        async def run_deployment():
            try:
                success = await retraining_service.deploy_trained_model(request.model_path, request.model_name)
                logger.info(f"Model deployment {'succeeded' if success else 'failed'}: {request.model_name}")
            except Exception as e:
                logger.error(f"Background deployment failed: {e}")
        
        background_tasks.add_task(run_deployment)
        
        return ModelDeploymentResponse(
            success=True,
            message=f"Model deployment started for {request.model_name}"
        )
        
    except Exception as e:
        logger.error(f"Error deploying trained model: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error deploying trained model: {str(e)}"
        )


@router.post("/cleanup-checkpoints")
async def cleanup_old_checkpoints(days_to_keep: int = 30) -> Dict[str, Any]:
    """
    Clean up old training checkpoints to free disk space
    
    Args:
        days_to_keep: Number of days to keep checkpoints (default: 30)
        
    Returns:
        Dict containing cleanup results
    """
    try:
        service = get_service_instance()
        if not service:
            raise HTTPException(
                status_code=400,
                detail="Service not initialized"
            )
        
        hot_swap_manager = get_hot_swap_manager(service)
        retraining_service = get_retraining_service(service, hot_swap_manager)
        
        cleanup_results = retraining_service.cleanup_old_checkpoints(days_to_keep)
        return cleanup_results
        
    except Exception as e:
        logger.error(f"Error cleaning up checkpoints: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Error cleaning up checkpoints: {str(e)}"
        )