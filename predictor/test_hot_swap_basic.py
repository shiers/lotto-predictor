"""
Basic test for model hot-swap functionality

This test verifies that the hot-swap system can be initialized
and basic operations work correctly.
"""

import pytest
import asyncio
import tempfile
import os
from datetime import datetime
from pathlib import Path

from local_llm_service import LocalLlmService, ModelConfig, get_default_config
from model_hot_swap import (
    ModelHotSwapManager,
    ModelSwapRequest,
    SwapStatus,
    get_hot_swap_manager
)


class TestModelHotSwap:
    """Test class for model hot-swap functionality"""
    
    @pytest.fixture
    async def service(self):
        """Create a test service instance"""
        config = get_default_config()
        
        # Create temporary model path
        with tempfile.TemporaryDirectory() as temp_dir:
            config.model_path = temp_dir
            service = LocalLlmService(config)
            
            # Load initial model
            await service.load_model()
            
            yield service
            
            # Cleanup
            await service.unload_model()
    
    @pytest.fixture
    def temp_model_path(self):
        """Create temporary model path for testing"""
        with tempfile.TemporaryDirectory() as temp_dir:
            # Create a dummy model file
            model_file = Path(temp_dir) / "model.bin"
            model_file.write_text("dummy model content")
            yield str(model_file)
    
    @pytest.mark.asyncio
    async def test_hot_swap_manager_initialization(self, service):
        """Test that hot swap manager can be initialized"""
        manager = ModelHotSwapManager(service)
        
        assert manager.service == service
        assert len(manager.swap_history) == 0
        assert len(manager.model_versions) == 0
        assert len(manager.active_swaps) == 0
    
    @pytest.mark.asyncio
    async def test_model_validation(self, service, temp_model_path):
        """Test model validation functionality"""
        manager = ModelHotSwapManager(service)
        
        # Test valid model path
        is_compatible, results = await manager.validate_model_compatibility(temp_model_path)
        
        assert isinstance(is_compatible, bool)
        assert isinstance(results, dict)
        assert "path_exists" in results
        assert "path_accessible" in results
        assert "compatibility_score" in results
        
        # Should be compatible since we created a valid file
        assert is_compatible is True
        assert results["path_exists"] is True
        assert results["path_accessible"] is True
    
    @pytest.mark.asyncio
    async def test_invalid_model_validation(self, service):
        """Test validation with invalid model path"""
        manager = ModelHotSwapManager(service)
        
        # Test non-existent path
        is_compatible, results = await manager.validate_model_compatibility("/nonexistent/path")
        
        assert is_compatible is False
        assert results["path_exists"] is False
        assert len(results["errors"]) > 0
    
    @pytest.mark.asyncio
    async def test_hot_swap_request_creation(self, temp_model_path):
        """Test creating hot swap requests"""
        request = ModelSwapRequest(
            new_model_path=temp_model_path,
            new_model_name="test_model",
            validation_required=True,
            rollback_on_failure=True,
            test_predictions=3
        )
        
        assert request.new_model_path == temp_model_path
        assert request.new_model_name == "test_model"
        assert request.validation_required is True
        assert request.rollback_on_failure is True
        assert request.test_predictions == 3
    
    @pytest.mark.asyncio
    async def test_hot_swap_with_validation_failure(self, service):
        """Test hot swap with validation failure"""
        manager = ModelHotSwapManager(service)
        
        request = ModelSwapRequest(
            new_model_path="/nonexistent/path",
            new_model_name="invalid_model",
            validation_required=True,
            rollback_on_failure=True
        )
        
        result = await manager.hot_swap_model(request)
        
        assert result.success is False
        assert result.status == SwapStatus.FAILED
        assert "validation failed" in result.message.lower()
        assert result.swap_id is not None
        assert result.started_at is not None
        assert result.completed_at is not None
    
    @pytest.mark.asyncio
    async def test_swap_status_tracking(self, service, temp_model_path):
        """Test swap status tracking"""
        manager = ModelHotSwapManager(service)
        
        request = ModelSwapRequest(
            new_model_path=temp_model_path,
            new_model_name="test_model"
        )
        
        # Start swap (will likely fail due to mock implementation, but that's ok)
        result = await manager.hot_swap_model(request)
        
        # Check that swap is in history
        assert len(manager.swap_history) == 1
        assert manager.swap_history[0].swap_id == result.swap_id
        
        # Test getting swap status
        status = manager.get_swap_status(result.swap_id)
        assert status is not None
        assert status.swap_id == result.swap_id
        
        # Test non-existent swap
        non_existent = manager.get_swap_status("nonexistent_id")
        assert non_existent is None
    
    @pytest.mark.asyncio
    async def test_model_version_tracking(self, service):
        """Test model version tracking"""
        manager = ModelHotSwapManager(service)
        
        # Initially no versions
        versions = manager.get_model_versions()
        assert len(versions) == 0
        
        active_version = manager.get_active_version()
        assert active_version is None
    
    def test_global_manager_instance(self):
        """Test global manager instance"""
        config = get_default_config()
        service1 = LocalLlmService(config)
        service2 = LocalLlmService(config)
        
        manager1 = get_hot_swap_manager(service1)
        manager2 = get_hot_swap_manager(service1)  # Same service
        
        # Should return the same instance for same service
        assert manager1 is manager2
    
    @pytest.mark.asyncio
    async def test_swap_history_limit(self, service, temp_model_path):
        """Test swap history with limit"""
        manager = ModelHotSwapManager(service)
        
        # Add multiple swaps to history (simulate)
        for i in range(5):
            request = ModelSwapRequest(
                new_model_path=temp_model_path,
                new_model_name=f"test_model_{i}"
            )
            await manager.hot_swap_model(request)
        
        # Test history with limit
        history_all = manager.get_swap_history()
        history_limited = manager.get_swap_history(limit=3)
        
        assert len(history_all) == 5
        assert len(history_limited) == 3
        
        # Should return most recent
        assert history_limited == history_all[-3:]


if __name__ == "__main__":
    pytest.main([__file__, "-v"])