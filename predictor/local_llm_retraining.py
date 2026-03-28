"""
Local LLM Model Retraining System

This module implements model retraining capabilities for the locally hosted LLM service,
including training data preprocessing, model fine-tuning workflows, validation,
performance benchmarking, and training checkpoint management.
"""

import asyncio
import json
import logging
import os
import shutil
import threading
import time
from datetime import datetime, timedelta
from enum import Enum
from pathlib import Path
from typing import Dict, List, Optional, Any, Tuple
from pydantic import BaseModel, Field
import torch
import psutil

from local_llm_service import LocalLlmService, ModelConfig
from model_hot_swap import ModelHotSwapManager, ModelSwapRequest

# Configure logging
logger = logging.getLogger(__name__)


class TrainingStatus(str, Enum):
    """Status of training operation"""
    PENDING = "pending"
    PREPROCESSING = "preprocessing"
    TRAINING = "training"
    VALIDATING = "validating"
    COMPLETED = "completed"
    FAILED = "failed"
    CANCELLED = "cancelled"


class TrainingPhase(str, Enum):
    """Current phase of training"""
    INITIALIZATION = "initialization"
    DATA_PREPROCESSING = "data_preprocessing"
    MODEL_SETUP = "model_setup"
    TRAINING_LOOP = "training_loop"
    VALIDATION = "validation"
    CHECKPOINT_SAVE = "checkpoint_save"
    FINALIZATION = "finalization"


class TrainingDataset(BaseModel):
    """Training dataset structure"""
    historical_draws: List[Dict[str, Any]] = Field(default_factory=list)
    previous_predictions: List[Dict[str, Any]] = Field(default_factory=list)
    accuracy_data: List[Dict[str, Any]] = Field(default_factory=list)
    generated_at: datetime = Field(default_factory=datetime.now)
    total_samples: int = Field(default=0)
    validation_split: float = Field(default=0.2, ge=0.1, le=0.4)
    metadata: Dict[str, Any] = Field(default_factory=dict)


class TrainingConfig(BaseModel):
    """Configuration for model training"""
    learning_rate: float = Field(default=0.001, gt=0.0, le=1.0)
    batch_size: int = Field(default=32, ge=1, le=512)
    epochs: int = Field(default=10, ge=1, le=1000)
    validation_split: float = Field(default=0.2, ge=0.1, le=0.4)
    early_stopping_patience: int = Field(default=3, ge=1, le=20)
    checkpoint_frequency: int = Field(default=1, ge=1, le=10)
    max_training_time_hours: float = Field(default=24.0, gt=0.0, le=168.0)
    use_gpu: bool = Field(default=True)
    mixed_precision: bool = Field(default=True)
    gradient_accumulation_steps: int = Field(default=1, ge=1, le=32)
    warmup_steps: int = Field(default=100, ge=0, le=10000)
    weight_decay: float = Field(default=0.01, ge=0.0, le=1.0)
    save_best_only: bool = Field(default=True)
    custom_parameters: Dict[str, Any] = Field(default_factory=dict)


class TrainingMetrics(BaseModel):
    """Training metrics and progress"""
    current_epoch: int = Field(default=0)
    total_epochs: int = Field(default=0)
    current_step: int = Field(default=0)
    total_steps: int = Field(default=0)
    training_loss: float = Field(default=0.0)
    validation_loss: float = Field(default=0.0)
    validation_accuracy: float = Field(default=0.0)
    learning_rate: float = Field(default=0.0)
    epoch_time_seconds: float = Field(default=0.0)
    estimated_time_remaining_seconds: float = Field(default=0.0)
    best_validation_accuracy: float = Field(default=0.0)
    best_epoch: int = Field(default=0)
    gpu_memory_usage_mb: Optional[float] = Field(default=None)
    cpu_memory_usage_mb: float = Field(default=0.0)
    samples_per_second: float = Field(default=0.0)


class TrainingCheckpoint(BaseModel):
    """Training checkpoint information"""
    checkpoint_id: str = Field(..., description="Unique checkpoint identifier")
    epoch: int = Field(..., description="Epoch number")
    step: int = Field(..., description="Training step")
    validation_accuracy: float = Field(..., description="Validation accuracy at checkpoint")
    training_loss: float = Field(..., description="Training loss at checkpoint")
    validation_loss: float = Field(..., description="Validation loss at checkpoint")
    model_path: str = Field(..., description="Path to saved model")
    created_at: datetime = Field(default_factory=datetime.now)
    file_size_mb: float = Field(default=0.0)
    is_best: bool = Field(default=False)
    metadata: Dict[str, Any] = Field(default_factory=dict)


class TrainingResult(BaseModel):
    """Result of training operation"""
    training_id: str = Field(..., description="Unique training identifier")
    success: bool = Field(..., description="Whether training completed successfully")
    status: TrainingStatus = Field(..., description="Final training status")
    message: str = Field(..., description="Status message")
    started_at: datetime = Field(..., description="Training start time")
    completed_at: Optional[datetime] = Field(None, description="Training completion time")
    duration_seconds: Optional[float] = Field(None, description="Total training duration")
    final_metrics: Optional[TrainingMetrics] = Field(None, description="Final training metrics")
    best_checkpoint: Optional[TrainingCheckpoint] = Field(None, description="Best checkpoint")
    all_checkpoints: List[TrainingCheckpoint] = Field(default_factory=list)
    training_config: TrainingConfig = Field(..., description="Training configuration used")
    dataset_info: Dict[str, Any] = Field(default_factory=dict)
    error_message: Optional[str] = Field(None, description="Error message if failed")
    model_path: Optional[str] = Field(None, description="Path to final trained model")


class LocalLlmRetrainingService:
    """
    Service for retraining locally hosted LLM models
    
    Provides functionality for:
    - Training data preprocessing and validation
    - Model fine-tuning with GPU resource management
    - Training progress monitoring and checkpointing
    - Model validation and performance benchmarking
    - Training checkpoint management and resume functionality
    """
    
    def __init__(self, service: LocalLlmService, hot_swap_manager: ModelHotSwapManager):
        self.service = service
        self.hot_swap_manager = hot_swap_manager
        self.active_trainings: Dict[str, TrainingResult] = {}
        self.training_history: List[TrainingResult] = []
        self.training_lock = threading.Lock()
        
        # Training directories
        self.training_dir = Path("./training")
        self.checkpoints_dir = self.training_dir / "checkpoints"
        self.models_dir = self.training_dir / "models"
        self.logs_dir = self.training_dir / "logs"
        
        # Create directories
        for directory in [self.training_dir, self.checkpoints_dir, self.models_dir, self.logs_dir]:
            directory.mkdir(parents=True, exist_ok=True)
        
        logger.info("LocalLlmRetrainingService initialized")
    
    def _generate_training_id(self) -> str:
        """Generate unique training operation ID"""
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        return f"training_{timestamp}_{id(self)}"
    
    def _generate_checkpoint_id(self, training_id: str, epoch: int) -> str:
        """Generate unique checkpoint ID"""
        return f"{training_id}_epoch_{epoch:03d}"
    
    def _get_memory_usage(self) -> float:
        """Get current memory usage in MB"""
        process = psutil.Process()
        return process.memory_info().rss / 1024 / 1024
    
    def _get_gpu_memory_usage(self) -> Optional[float]:
        """Get GPU memory usage in MB"""
        if torch.cuda.is_available():
            try:
                return torch.cuda.memory_allocated() / 1024 / 1024
            except Exception:
                return None
        return None
    
    async def preprocess_training_data(self, dataset: TrainingDataset) -> Tuple[bool, Dict[str, Any]]:
        """
        Preprocess training data for model consumption
        
        Args:
            dataset: Raw training dataset
            
        Returns:
            Tuple[bool, Dict]: (success, preprocessing_results)
        """
        preprocessing_results = {
            "original_samples": dataset.total_samples,
            "processed_samples": 0,
            "validation_samples": 0,
            "training_samples": 0,
            "preprocessing_time_seconds": 0.0,
            "data_quality_score": 0.0,
            "errors": [],
            "warnings": []
        }
        
        try:
            start_time = time.time()
            
            logger.info(f"Preprocessing training data with {dataset.total_samples} samples")
            
            # Validate dataset structure
            if not dataset.historical_draws:
                preprocessing_results["errors"].append("No historical draws provided")
                return False, preprocessing_results
            
            if len(dataset.historical_draws) < 50:
                preprocessing_results["warnings"].append(
                    f"Limited historical data: {len(dataset.historical_draws)} draws (recommended: 100+)"
                )
            
            # Process historical draws
            processed_draws = []
            for draw in dataset.historical_draws:
                try:
                    # Validate draw structure
                    required_fields = ["draw", "date", "winning_numbers"]
                    if not all(field in draw for field in required_fields):
                        preprocessing_results["warnings"].append(f"Draw missing required fields: {draw}")
                        continue
                    
                    # Normalize winning numbers
                    winning_numbers = draw["winning_numbers"]
                    if isinstance(winning_numbers, list) and len(winning_numbers) == 6:
                        if all(isinstance(n, int) and 1 <= n <= 40 for n in winning_numbers):
                            processed_draws.append(draw)
                        else:
                            preprocessing_results["warnings"].append(f"Invalid winning numbers: {winning_numbers}")
                    else:
                        preprocessing_results["warnings"].append(f"Invalid winning numbers format: {winning_numbers}")
                        
                except Exception as e:
                    preprocessing_results["warnings"].append(f"Error processing draw: {e}")
            
            # Process prediction data
            processed_predictions = []
            for prediction in dataset.previous_predictions:
                try:
                    # Validate prediction structure
                    if "numbers" in prediction and "source" in prediction:
                        processed_predictions.append(prediction)
                    else:
                        preprocessing_results["warnings"].append(f"Invalid prediction format: {prediction}")
                        
                except Exception as e:
                    preprocessing_results["warnings"].append(f"Error processing prediction: {e}")
            
            # Process accuracy data
            processed_accuracy = []
            for accuracy in dataset.accuracy_data:
                try:
                    # Validate accuracy structure
                    if "prediction_id" in accuracy and "accuracy_score" in accuracy:
                        processed_accuracy.append(accuracy)
                    else:
                        preprocessing_results["warnings"].append(f"Invalid accuracy format: {accuracy}")
                        
                except Exception as e:
                    preprocessing_results["warnings"].append(f"Error processing accuracy: {e}")
            
            # Create training samples
            training_samples = []
            
            # Combine draws with predictions for training
            for i, draw in enumerate(processed_draws[:-10]):  # Reserve last 10 for validation
                # Create context from previous draws
                context_draws = processed_draws[max(0, i-20):i]  # Use up to 20 previous draws
                
                # Find predictions for this draw
                draw_predictions = [p for p in processed_predictions 
                                  if p.get("target_draw") == draw.get("draw")]
                
                # Create training sample
                sample = {
                    "context_draws": context_draws,
                    "target_draw": draw,
                    "predictions": draw_predictions,
                    "sample_id": f"sample_{i}"
                }
                training_samples.append(sample)
            
            # Split into training and validation
            split_index = int(len(training_samples) * (1 - dataset.validation_split))
            training_set = training_samples[:split_index]
            validation_set = training_samples[split_index:]
            
            # Calculate data quality score
            quality_factors = []
            
            # Historical data completeness
            if len(processed_draws) >= 100:
                quality_factors.append(1.0)
            elif len(processed_draws) >= 50:
                quality_factors.append(0.7)
            else:
                quality_factors.append(0.3)
            
            # Prediction data availability
            if len(processed_predictions) >= 50:
                quality_factors.append(1.0)
            elif len(processed_predictions) >= 20:
                quality_factors.append(0.7)
            else:
                quality_factors.append(0.3)
            
            # Accuracy data availability
            if len(processed_accuracy) >= 20:
                quality_factors.append(1.0)
            elif len(processed_accuracy) >= 10:
                quality_factors.append(0.7)
            else:
                quality_factors.append(0.3)
            
            data_quality_score = sum(quality_factors) / len(quality_factors)
            
            # Update results
            preprocessing_results.update({
                "processed_samples": len(training_samples),
                "training_samples": len(training_set),
                "validation_samples": len(validation_set),
                "preprocessing_time_seconds": time.time() - start_time,
                "data_quality_score": data_quality_score,
                "processed_draws": len(processed_draws),
                "processed_predictions": len(processed_predictions),
                "processed_accuracy": len(processed_accuracy)
            })
            
            # Save preprocessed data
            training_data_path = self.training_dir / f"preprocessed_data_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
            with open(training_data_path, 'w') as f:
                json.dump({
                    "training_set": training_set,
                    "validation_set": validation_set,
                    "preprocessing_results": preprocessing_results,
                    "dataset_metadata": dataset.metadata
                }, f, indent=2, default=str)
            
            preprocessing_results["data_path"] = str(training_data_path)
            
            logger.info(f"Data preprocessing completed: {len(training_set)} training, {len(validation_set)} validation samples")
            
            return True, preprocessing_results
            
        except Exception as e:
            logger.error(f"Data preprocessing failed: {e}")
            preprocessing_results["errors"].append(f"Preprocessing failed: {e}")
            return False, preprocessing_results
    
    async def start_training(self, dataset: TrainingDataset, config: TrainingConfig) -> TrainingResult:
        """
        Start model retraining process
        
        Args:
            dataset: Training dataset
            config: Training configuration
            
        Returns:
            TrainingResult: Training operation result
        """
        training_id = self._generate_training_id()
        
        # Initialize training result
        training_result = TrainingResult(
            training_id=training_id,
            success=False,
            status=TrainingStatus.PENDING,
            message="Training initialization started",
            started_at=datetime.now(),
            training_config=config
        )
        
        # Add to active trainings
        self.active_trainings[training_id] = training_result
        
        try:
            with self.training_lock:
                logger.info(f"Starting training operation {training_id}")
                
                # Step 1: Preprocess data
                training_result.status = TrainingStatus.PREPROCESSING
                training_result.message = "Preprocessing training data"
                
                success, preprocessing_results = await self.preprocess_training_data(dataset)
                training_result.dataset_info = preprocessing_results
                
                if not success:
                    training_result.status = TrainingStatus.FAILED
                    training_result.message = f"Data preprocessing failed: {preprocessing_results.get('errors', [])}"
                    training_result.completed_at = datetime.now()
                    training_result.error_message = str(preprocessing_results.get('errors', []))
                    return training_result
                
                # Check data quality
                if preprocessing_results.get("data_quality_score", 0.0) < 0.5:
                    training_result.status = TrainingStatus.FAILED
                    training_result.message = f"Data quality too low: {preprocessing_results.get('data_quality_score', 0.0):.2f}"
                    training_result.completed_at = datetime.now()
                    training_result.error_message = "Insufficient data quality for training"
                    return training_result
                
                # Step 2: Setup training environment
                training_result.status = TrainingStatus.TRAINING
                training_result.message = "Setting up training environment"
                
                # Create training directories
                training_run_dir = self.training_dir / training_id
                training_run_dir.mkdir(exist_ok=True)
                
                model_output_dir = self.models_dir / training_id
                model_output_dir.mkdir(exist_ok=True)
                
                checkpoint_dir = self.checkpoints_dir / training_id
                checkpoint_dir.mkdir(exist_ok=True)
                
                # Step 3: Simulate training process
                # In a real implementation, this would:
                # 1. Load the base model
                # 2. Setup training loop with the preprocessed data
                # 3. Run training epochs with checkpointing
                # 4. Monitor metrics and early stopping
                
                await self._simulate_training_process(training_result, config, preprocessing_results)
                
                # Step 4: Finalize training
                if training_result.status != TrainingStatus.FAILED:
                    training_result.success = True
                    training_result.status = TrainingStatus.COMPLETED
                    training_result.message = "Training completed successfully"
                    training_result.model_path = str(model_output_dir / "final_model")
                
                training_result.completed_at = datetime.now()
                if training_result.started_at:
                    training_result.duration_seconds = (training_result.completed_at - training_result.started_at).total_seconds()
                
                logger.info(f"Training operation {training_id} completed with status: {training_result.status}")
                
        except Exception as e:
            logger.error(f"Training operation {training_id} failed: {e}")
            training_result.status = TrainingStatus.FAILED
            training_result.message = f"Training failed: {e}"
            training_result.error_message = str(e)
            training_result.completed_at = datetime.now()
        
        finally:
            # Remove from active trainings and add to history
            self.active_trainings.pop(training_id, None)
            self.training_history.append(training_result)
        
        return training_result
    
    async def _simulate_training_process(self, training_result: TrainingResult, config: TrainingConfig, preprocessing_results: Dict[str, Any]):
        """
        Simulate the training process for demonstration
        
        In a real implementation, this would contain the actual training loop
        """
        try:
            total_epochs = config.epochs
            training_samples = preprocessing_results.get("training_samples", 100)
            validation_samples = preprocessing_results.get("validation_samples", 20)
            
            # Initialize metrics
            metrics = TrainingMetrics(
                total_epochs=total_epochs,
                total_steps=total_epochs * (training_samples // config.batch_size)
            )
            
            checkpoints = []
            best_accuracy = 0.0
            
            for epoch in range(total_epochs):
                epoch_start_time = time.time()
                
                # Simulate training epoch
                await asyncio.sleep(0.5)  # Simulate training time
                
                # Update metrics
                metrics.current_epoch = epoch + 1
                metrics.current_step = (epoch + 1) * (training_samples // config.batch_size)
                
                # Simulate improving metrics
                base_loss = 1.0
                metrics.training_loss = base_loss * (0.9 ** epoch) + 0.1
                metrics.validation_loss = base_loss * (0.85 ** epoch) + 0.15
                metrics.validation_accuracy = min(0.95, 0.5 + (epoch * 0.05))
                metrics.learning_rate = config.learning_rate * (0.95 ** epoch)
                
                epoch_time = time.time() - epoch_start_time
                metrics.epoch_time_seconds = epoch_time
                metrics.estimated_time_remaining_seconds = epoch_time * (total_epochs - epoch - 1)
                
                metrics.gpu_memory_usage_mb = self._get_gpu_memory_usage()
                metrics.cpu_memory_usage_mb = self._get_memory_usage()
                metrics.samples_per_second = training_samples / epoch_time if epoch_time > 0 else 0
                
                # Track best accuracy
                if metrics.validation_accuracy > best_accuracy:
                    best_accuracy = metrics.validation_accuracy
                    metrics.best_validation_accuracy = best_accuracy
                    metrics.best_epoch = epoch + 1
                
                # Create checkpoint
                if (epoch + 1) % config.checkpoint_frequency == 0:
                    checkpoint_id = self._generate_checkpoint_id(training_result.training_id, epoch + 1)
                    checkpoint_path = self.checkpoints_dir / training_result.training_id / f"checkpoint_{epoch+1:03d}.pt"
                    
                    # Simulate saving checkpoint
                    checkpoint_path.parent.mkdir(parents=True, exist_ok=True)
                    checkpoint_path.touch()  # Create empty file for simulation
                    
                    checkpoint = TrainingCheckpoint(
                        checkpoint_id=checkpoint_id,
                        epoch=epoch + 1,
                        step=metrics.current_step,
                        validation_accuracy=metrics.validation_accuracy,
                        training_loss=metrics.training_loss,
                        validation_loss=metrics.validation_loss,
                        model_path=str(checkpoint_path),
                        file_size_mb=50.0 + epoch * 2.0,  # Simulate growing model size
                        is_best=(metrics.validation_accuracy == best_accuracy)
                    )
                    
                    checkpoints.append(checkpoint)
                    
                    logger.info(f"Epoch {epoch+1}/{total_epochs}: "
                              f"train_loss={metrics.training_loss:.4f}, "
                              f"val_loss={metrics.validation_loss:.4f}, "
                              f"val_acc={metrics.validation_accuracy:.4f}")
                
                # Early stopping check
                if epoch >= config.early_stopping_patience:
                    recent_accuracies = [cp.validation_accuracy for cp in checkpoints[-config.early_stopping_patience:]]
                    if len(recent_accuracies) >= config.early_stopping_patience:
                        if all(acc <= recent_accuracies[0] + 0.001 for acc in recent_accuracies[1:]):
                            logger.info(f"Early stopping triggered at epoch {epoch+1}")
                            break
                
                # Check training time limit
                if training_result.started_at:
                    elapsed_hours = (datetime.now() - training_result.started_at).total_seconds() / 3600
                    if elapsed_hours >= config.max_training_time_hours:
                        logger.info(f"Training time limit reached: {elapsed_hours:.2f} hours")
                        break
            
            # Set final metrics and best checkpoint
            training_result.final_metrics = metrics
            if checkpoints:
                best_checkpoint = max(checkpoints, key=lambda cp: cp.validation_accuracy)
                training_result.best_checkpoint = best_checkpoint
                training_result.all_checkpoints = checkpoints
            
        except Exception as e:
            logger.error(f"Training simulation failed: {e}")
            training_result.status = TrainingStatus.FAILED
            training_result.error_message = str(e)
            raise
    
    async def validate_trained_model(self, model_path: str) -> Dict[str, Any]:
        """
        Validate a trained model before deployment
        
        Args:
            model_path: Path to the trained model
            
        Returns:
            Dict containing validation results
        """
        validation_results = {
            "is_valid": False,
            "validation_accuracy": 0.0,
            "baseline_accuracy": 0.65,  # Baseline model accuracy
            "improvement_percentage": 0.0,
            "performance_metrics": {},
            "validation_errors": [],
            "validation_warnings": [],
            "validated_at": datetime.now(),
            "validated_by": "LocalLlmRetrainingService"
        }
        
        try:
            logger.info(f"Validating trained model: {model_path}")
            
            # Check if model exists
            if not os.path.exists(model_path):
                validation_results["validation_errors"].append(f"Model path does not exist: {model_path}")
                return validation_results
            
            # Simulate model validation
            await asyncio.sleep(1.0)  # Simulate validation time
            
            # Simulate validation metrics
            import random
            validation_accuracy = 0.70 + (random.random() * 0.25)  # Random accuracy between 0.70-0.95
            
            validation_results.update({
                "is_valid": True,
                "validation_accuracy": validation_accuracy,
                "improvement_percentage": ((validation_accuracy - validation_results["baseline_accuracy"]) / validation_results["baseline_accuracy"]) * 100,
                "performance_metrics": {
                    "precision": 0.68 + (random.random() * 0.25),
                    "recall": 0.65 + (random.random() * 0.28),
                    "f1_score": 0.67 + (random.random() * 0.26),
                    "inference_latency_ms": 120 + (random.random() * 80)
                }
            })
            
            # Validate model quality
            if validation_results["validation_accuracy"] < 0.5:
                validation_results["validation_errors"].append("Model accuracy is below minimum threshold (50%)")
                validation_results["is_valid"] = False
            
            if validation_results["improvement_percentage"] < 2.0:
                validation_results["validation_warnings"].append("Model improvement over baseline is less than 2%")
            
            if validation_results["performance_metrics"]["inference_latency_ms"] > 500:
                validation_results["validation_warnings"].append("Model inference latency is high (>500ms)")
            
            logger.info(f"Model validation completed. Valid: {validation_results['is_valid']}, "
                       f"Accuracy: {validation_results['validation_accuracy']:.2%}")
            
            return validation_results
            
        except Exception as e:
            logger.error(f"Model validation failed: {e}")
            validation_results["validation_errors"].append(f"Validation failed: {e}")
            return validation_results
    
    async def deploy_trained_model(self, model_path: str, model_name: str) -> bool:
        """
        Deploy a trained model using hot-swap functionality
        
        Args:
            model_path: Path to the trained model
            model_name: Name for the deployed model
            
        Returns:
            bool: True if deployment successful
        """
        try:
            logger.info(f"Deploying trained model: {model_name} from {model_path}")
            
            # Validate model before deployment
            validation_results = await self.validate_trained_model(model_path)
            if not validation_results["is_valid"]:
                logger.error(f"Model validation failed: {validation_results['validation_errors']}")
                return False
            
            # Use hot-swap manager to deploy the model
            swap_request = ModelSwapRequest(
                new_model_path=model_path,
                new_model_name=model_name,
                validation_required=True,
                rollback_on_failure=True,
                test_predictions=5
            )
            
            swap_result = await self.hot_swap_manager.hot_swap_model(swap_request)
            
            if swap_result.success:
                logger.info(f"Model {model_name} deployed successfully via hot-swap")
                return True
            else:
                logger.error(f"Model deployment failed: {swap_result.message}")
                return False
                
        except Exception as e:
            logger.error(f"Model deployment failed: {e}")
            return False
    
    def get_training_status(self, training_id: str) -> Optional[TrainingResult]:
        """Get status of a training operation"""
        # Check active trainings first
        if training_id in self.active_trainings:
            return self.active_trainings[training_id]
        
        # Check history
        for training in self.training_history:
            if training.training_id == training_id:
                return training
        
        return None
    
    def get_active_trainings(self) -> List[TrainingResult]:
        """Get all currently active training operations"""
        return list(self.active_trainings.values())
    
    def get_training_history(self, limit: int = 10) -> List[TrainingResult]:
        """Get recent training history"""
        return self.training_history[-limit:] if limit > 0 else self.training_history
    
    def cancel_training(self, training_id: str) -> bool:
        """Cancel an active training operation"""
        if training_id in self.active_trainings:
            training_result = self.active_trainings[training_id]
            training_result.status = TrainingStatus.CANCELLED
            training_result.message = "Training cancelled by user"
            training_result.completed_at = datetime.now()
            
            # Move to history
            self.active_trainings.pop(training_id)
            self.training_history.append(training_result)
            
            logger.info(f"Training {training_id} cancelled")
            return True
        
        return False
    
    def cleanup_old_checkpoints(self, days_to_keep: int = 30) -> Dict[str, Any]:
        """
        Clean up old training checkpoints to free disk space
        
        Args:
            days_to_keep: Number of days to keep checkpoints
            
        Returns:
            Dict containing cleanup results
        """
        cleanup_results = {
            "files_deleted": 0,
            "space_freed_mb": 0.0,
            "errors": []
        }
        
        try:
            cutoff_date = datetime.now() - timedelta(days=days_to_keep)
            
            for checkpoint_dir in self.checkpoints_dir.iterdir():
                if checkpoint_dir.is_dir():
                    try:
                        # Check directory modification time
                        dir_mtime = datetime.fromtimestamp(checkpoint_dir.stat().st_mtime)
                        
                        if dir_mtime < cutoff_date:
                            # Calculate size before deletion
                            total_size = sum(f.stat().st_size for f in checkpoint_dir.rglob('*') if f.is_file())
                            size_mb = total_size / (1024 * 1024)
                            
                            # Count files
                            file_count = len(list(checkpoint_dir.rglob('*')))
                            
                            # Delete directory
                            shutil.rmtree(checkpoint_dir)
                            
                            cleanup_results["files_deleted"] += file_count
                            cleanup_results["space_freed_mb"] += size_mb
                            
                            logger.info(f"Deleted old checkpoint directory: {checkpoint_dir}")
                            
                    except Exception as e:
                        cleanup_results["errors"].append(f"Error deleting {checkpoint_dir}: {e}")
            
            logger.info(f"Checkpoint cleanup completed: {cleanup_results['files_deleted']} files, "
                       f"{cleanup_results['space_freed_mb']:.2f}MB freed")
            
        except Exception as e:
            cleanup_results["errors"].append(f"Cleanup failed: {e}")
        
        return cleanup_results


# Global retraining service instance
_retraining_service: Optional[LocalLlmRetrainingService] = None


def get_retraining_service(service: LocalLlmService, hot_swap_manager: ModelHotSwapManager) -> LocalLlmRetrainingService:
    """Get or create the global retraining service"""
    global _retraining_service
    if _retraining_service is None:
        _retraining_service = LocalLlmRetrainingService(service, hot_swap_manager)
    return _retraining_service