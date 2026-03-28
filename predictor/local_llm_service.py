"""
Local LLM Service for PredictLottoNZ

This module provides a locally hosted LLM service with model loading,
GPU/CPU resource management, and health monitoring capabilities.
"""

import asyncio
import logging
import os
import json
import time
import threading
from typing import Dict, List, Optional, Any, Tuple
from datetime import datetime
from pathlib import Path
import psutil
import torch
from pydantic import BaseModel, Field
from fastapi import HTTPException

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class ModelConfig(BaseModel):
    """Configuration for LLM model"""
    model_path: str = Field(..., description="Path to the model files")
    model_name: str = Field(..., description="Name of the model")
    max_memory_gb: float = Field(default=8.0, description="Maximum memory allocation in GB")
    use_gpu: bool = Field(default=True, description="Whether to use GPU if available")
    max_tokens: int = Field(default=512, description="Maximum tokens for generation")
    temperature: float = Field(default=0.7, description="Temperature for generation")
    device: Optional[str] = Field(default=None, description="Specific device to use")


class ModelMetadata(BaseModel):
    """Metadata for loaded model"""
    model_name: str
    model_path: str
    loaded_at: datetime
    version: str
    memory_usage_mb: float
    device: str
    is_active: bool


class HealthStatus(BaseModel):
    """Health status of the LLM service"""
    status: str = Field(..., description="Service status: healthy, degraded, unhealthy")
    model_loaded: bool = Field(..., description="Whether a model is currently loaded")
    model_name: Optional[str] = Field(None, description="Name of the loaded model")
    memory_usage_mb: float = Field(..., description="Current memory usage in MB")
    gpu_available: bool = Field(..., description="Whether GPU is available")
    gpu_memory_mb: Optional[float] = Field(None, description="GPU memory usage in MB")
    uptime_seconds: float = Field(..., description="Service uptime in seconds")
    last_prediction_at: Optional[datetime] = Field(None, description="Last prediction timestamp")


class LocalLlmService:
    """
    Local LLM Service with model management and resource monitoring
    
    Provides functionality for:
    - Model loading and initialization
    - GPU/CPU resource management
    - Health monitoring
    - Model hot-swapping
    """
    
    def __init__(self, config: ModelConfig):
        self.config = config
        self.model = None
        self.tokenizer = None
        self.model_metadata: Optional[ModelMetadata] = None
        self.start_time = time.time()
        self.last_prediction_at: Optional[datetime] = None
        self.is_loading = False
        self.load_lock = threading.Lock()
        
        # Initialize device
        self._initialize_device()
        
        # Start health monitoring
        self._start_health_monitoring()
        
        logger.info(f"LocalLlmService initialized with config: {config}")
    
    def _initialize_device(self):
        """Initialize the compute device (GPU/CPU)"""
        if self.config.use_gpu and torch.cuda.is_available():
            if self.config.device:
                self.device = self.config.device
            else:
                self.device = "cuda:0"
            logger.info(f"Using GPU device: {self.device}")
        else:
            self.device = "cpu"
            logger.info("Using CPU device")
    
    def _start_health_monitoring(self):
        """Start background health monitoring"""
        def monitor():
            while True:
                try:
                    self._update_health_metrics()
                    time.sleep(30)  # Update every 30 seconds
                except Exception as e:
                    logger.error(f"Health monitoring error: {e}")
                    time.sleep(60)  # Wait longer on error
        
        monitor_thread = threading.Thread(target=monitor, daemon=True)
        monitor_thread.start()
        logger.info("Health monitoring started")
    
    def _update_health_metrics(self):
        """Update internal health metrics"""
        # This would update internal metrics for monitoring
        # Implementation depends on specific monitoring requirements
        pass
    
    def _get_memory_usage(self) -> float:
        """Get current memory usage in MB"""
        process = psutil.Process()
        return process.memory_info().rss / 1024 / 1024
    
    def _get_gpu_memory_usage(self) -> Optional[float]:
        """Get GPU memory usage in MB"""
        if torch.cuda.is_available() and self.device.startswith("cuda"):
            try:
                gpu_memory = torch.cuda.memory_allocated(self.device) / 1024 / 1024
                return gpu_memory
            except Exception as e:
                logger.warning(f"Could not get GPU memory usage: {e}")
        return None
    
    async def load_model(self, model_path: Optional[str] = None) -> bool:
        """
        Load the LLM model
        
        Args:
            model_path: Optional path to model, uses config path if not provided
            
        Returns:
            bool: True if model loaded successfully
        """
        with self.load_lock:
            if self.is_loading:
                logger.warning("Model loading already in progress")
                return False
            
            self.is_loading = True
        
        try:
            target_path = model_path or self.config.model_path
            logger.info(f"Loading model from: {target_path}")
            
            # Validate model path exists
            if not os.path.exists(target_path):
                raise FileNotFoundError(f"Model path does not exist: {target_path}")
            
            # For this implementation, we'll simulate model loading
            # In a real implementation, this would load actual model files
            # using libraries like transformers, torch, etc.
            
            # Simulate loading time
            await asyncio.sleep(2)
            
            # Create mock model and tokenizer
            self.model = {"type": "mock_llm", "path": target_path}
            self.tokenizer = {"type": "mock_tokenizer", "vocab_size": 50000}
            
            # Create metadata
            self.model_metadata = ModelMetadata(
                model_name=self.config.model_name,
                model_path=target_path,
                loaded_at=datetime.now(),
                version="1.0.0",
                memory_usage_mb=self._get_memory_usage(),
                device=self.device,
                is_active=True
            )
            
            logger.info(f"Model loaded successfully: {self.config.model_name}")
            return True
            
        except Exception as e:
            logger.error(f"Failed to load model: {e}")
            return False
        finally:
            self.is_loading = False
    
    async def unload_model(self) -> bool:
        """
        Unload the current model to free resources
        
        Returns:
            bool: True if model unloaded successfully
        """
        try:
            if self.model is None:
                logger.warning("No model loaded to unload")
                return True
            
            logger.info("Unloading model...")
            
            # Clear model and tokenizer
            self.model = None
            self.tokenizer = None
            
            # Clear GPU cache if using GPU
            if torch.cuda.is_available() and self.device.startswith("cuda"):
                torch.cuda.empty_cache()
            
            # Update metadata
            if self.model_metadata:
                self.model_metadata.is_active = False
            
            logger.info("Model unloaded successfully")
            return True
            
        except Exception as e:
            logger.error(f"Failed to unload model: {e}")
            return False
    
    def is_model_loaded(self) -> bool:
        """Check if a model is currently loaded"""
        return self.model is not None and not self.is_loading
    
    def get_model_metadata(self) -> Optional[ModelMetadata]:
        """Get metadata for the currently loaded model"""
        return self.model_metadata
    
    async def get_health_status(self) -> HealthStatus:
        """
        Get comprehensive health status of the service
        
        Returns:
            HealthStatus: Current health status
        """
        try:
            # Determine overall status
            if not self.is_model_loaded():
                status = "degraded" if self.model_metadata else "unhealthy"
            else:
                status = "healthy"
            
            # Get memory usage
            memory_usage = self._get_memory_usage()
            gpu_memory = self._get_gpu_memory_usage()
            
            # Calculate uptime
            uptime = time.time() - self.start_time
            
            return HealthStatus(
                status=status,
                model_loaded=self.is_model_loaded(),
                model_name=self.model_metadata.model_name if self.model_metadata else None,
                memory_usage_mb=memory_usage,
                gpu_available=torch.cuda.is_available(),
                gpu_memory_mb=gpu_memory,
                uptime_seconds=uptime,
                last_prediction_at=self.last_prediction_at
            )
            
        except Exception as e:
            logger.error(f"Error getting health status: {e}")
            return HealthStatus(
                status="unhealthy",
                model_loaded=False,
                memory_usage_mb=0.0,
                gpu_available=False,
                uptime_seconds=time.time() - self.start_time
            )
    
    def validate_model_compatibility(self, model_path: str) -> Tuple[bool, str]:
        """
        Validate model compatibility before loading
        
        Args:
            model_path: Path to the model to validate
            
        Returns:
            Tuple[bool, str]: (is_compatible, error_message)
        """
        try:
            # Check if path exists
            if not os.path.exists(model_path):
                return False, f"Model path does not exist: {model_path}"
            
            # Check if it's a directory or file
            path_obj = Path(model_path)
            if not (path_obj.is_dir() or path_obj.is_file()):
                return False, f"Invalid model path: {model_path}"
            
            # For this mock implementation, we'll accept any existing path
            # In a real implementation, this would check model format,
            # architecture compatibility, etc.
            
            return True, "Model is compatible"
            
        except Exception as e:
            return False, f"Error validating model: {e}"
    
    async def get_model_info(self) -> Dict[str, Any]:
        """
        Get detailed information about the current model
        
        Returns:
            Dict containing model information
        """
        if not self.is_model_loaded():
            return {"error": "No model loaded"}
        
        return {
            "model_loaded": True,
            "metadata": self.model_metadata.dict() if self.model_metadata else None,
            "device": self.device,
            "memory_usage_mb": self._get_memory_usage(),
            "gpu_memory_mb": self._get_gpu_memory_usage(),
            "config": self.config.dict()
        }


# Global service instance
_service_instance: Optional[LocalLlmService] = None


def get_service_instance() -> Optional[LocalLlmService]:
    """Get the global service instance"""
    return _service_instance


def initialize_service(config: ModelConfig) -> LocalLlmService:
    """
    Initialize the global service instance
    
    Args:
        config: Model configuration
        
    Returns:
        LocalLlmService: Initialized service instance
    """
    global _service_instance
    _service_instance = LocalLlmService(config)
    return _service_instance


def get_default_config() -> ModelConfig:
    """
    Get default configuration for the LLM service
    
    Returns:
        ModelConfig: Default configuration
    """
    return ModelConfig(
        model_path=os.getenv("LOCAL_LLM_MODEL_PATH", "/models/default"),
        model_name=os.getenv("LOCAL_LLM_MODEL_NAME", "default-llm"),
        max_memory_gb=float(os.getenv("LOCAL_LLM_MAX_MEMORY_GB", "8.0")),
        use_gpu=os.getenv("LOCAL_LLM_USE_GPU", "true").lower() == "true",
        max_tokens=int(os.getenv("LOCAL_LLM_MAX_TOKENS", "512")),
        temperature=float(os.getenv("LOCAL_LLM_TEMPERATURE", "0.7"))
    )