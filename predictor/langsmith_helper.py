"""
LangSmith Integration Helper

Provides utilities for tracing, logging, and monitoring lottery predictions
with LangSmith observability platform.
"""

import os
from typing import Optional, Dict, Any, List
from datetime import datetime
from functools import wraps

try:
    from langsmith import Client, traceable
    from langsmith.run_helpers import get_current_run_tree
    LANGSMITH_AVAILABLE = True
except ImportError:
    LANGSMITH_AVAILABLE = False
    print("Warning: LangSmith not installed. Install with: pip install langsmith")


class LangSmithHelper:
    """Helper class for LangSmith integration"""
    
    def __init__(self):
        self.enabled = self._check_enabled()
        self.client = None
        
        if self.enabled:
            try:
                self.client = Client(
                    api_key=os.getenv("LANGCHAIN_API_KEY"),
                    api_url=os.getenv("LANGCHAIN_ENDPOINT", "https://api.smith.langchain.com")
                )
                self.project_name = os.getenv("LANGCHAIN_PROJECT", "predict-lotto-nz")
                print(f"✓ LangSmith initialized for project: {self.project_name}")
            except Exception as e:
                print(f"Warning: Failed to initialize LangSmith: {e}")
                self.enabled = False
    
    def _check_enabled(self) -> bool:
        """Check if LangSmith is enabled and configured"""
        if not LANGSMITH_AVAILABLE:
            return False
        
        tracing_enabled = os.getenv("LANGCHAIN_TRACING_V2", "false").lower() == "true"
        api_key = os.getenv("LANGCHAIN_API_KEY")
        
        return tracing_enabled and bool(api_key)
    
    def add_metadata(self, metadata: Dict[str, Any]) -> None:
        """Add metadata to current trace"""
        if not self.enabled:
            return
        
        try:
            run = get_current_run_tree()
            if run:
                run.add_metadata(metadata)
        except Exception as e:
            print(f"Warning: Failed to add metadata: {e}")
    
    def add_tags(self, tags: List[str]) -> None:
        """Add tags to current trace"""
        if not self.enabled:
            return
        
        try:
            run = get_current_run_tree()
            if run:
                run.add_tags(tags)
        except Exception as e:
            print(f"Warning: Failed to add tags: {e}")
    
    def create_feedback(
        self,
        run_id: str,
        key: str,
        score: float,
        comment: Optional[str] = None
    ) -> None:
        """Create feedback for a prediction run"""
        if not self.enabled or not self.client:
            return
        
        try:
            self.client.create_feedback(
                run_id=run_id,
                key=key,
                score=score,
                comment=comment
            )
            print(f"✓ Feedback recorded: {key}={score}")
        except Exception as e:
            print(f"Warning: Failed to create feedback: {e}")
    
    def log_prediction(
        self,
        prediction_numbers: List[int],
        powerball: Optional[int] = None,
        confidence_score: Optional[float] = None,
        model_name: Optional[str] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> None:
        """Log prediction details to current trace"""
        if not self.enabled:
            return
        
        prediction_metadata = {
            "prediction_numbers": prediction_numbers,
            "powerball": powerball,
            "confidence_score": confidence_score,
            "model_name": model_name,
            "timestamp": datetime.utcnow().isoformat()
        }
        
        if metadata:
            prediction_metadata.update(metadata)
        
        self.add_metadata(prediction_metadata)
    
    def log_error(
        self,
        error: Exception,
        context: Optional[Dict[str, Any]] = None
    ) -> None:
        """Log error details to current trace"""
        if not self.enabled:
            return
        
        error_metadata = {
            "error_type": type(error).__name__,
            "error_message": str(error),
            "timestamp": datetime.utcnow().isoformat()
        }
        
        if context:
            error_metadata.update(context)
        
        self.add_metadata(error_metadata)
        self.add_tags(["error"])


# Global instance
langsmith = LangSmithHelper()


def trace_prediction(name: Optional[str] = None, tags: Optional[List[str]] = None):
    """
    Decorator for tracing prediction functions
    
    Usage:
        @trace_prediction(name="generate_lottery_numbers", tags=["production"])
        def predict(count: int):
            return [1, 2, 3, 4, 5, 6]
    """
    def decorator(func):
        if not langsmith.enabled:
            # If LangSmith not available, return original function
            return func
        
        @traceable(name=name or func.__name__)
        @wraps(func)
        def wrapper(*args, **kwargs):
            # Add tags if provided
            if tags:
                langsmith.add_tags(tags)
            
            # Add function metadata
            langsmith.add_metadata({
                "function": func.__name__,
                "args_count": len(args),
                "kwargs_keys": list(kwargs.keys())
            })
            
            try:
                result = func(*args, **kwargs)
                return result
            except Exception as e:
                langsmith.log_error(e, {"function": func.__name__})
                raise
        
        return wrapper
    return decorator


def setup_langsmith_env():
    """
    Setup LangSmith environment variables from .env file
    Call this at application startup
    """
    # Load from .env if not already set
    if not os.getenv("LANGCHAIN_TRACING_V2"):
        try:
            from dotenv import load_dotenv
            # Try parent directory first (for docker-compose setup)
            parent_env = os.path.join(os.path.dirname(__file__), "..", ".env")
            if os.path.exists(parent_env):
                load_dotenv(parent_env)
            else:
                load_dotenv()
        except ImportError:
            print("Warning: python-dotenv not installed")
    
    # Verify configuration
    if os.getenv("LANGCHAIN_TRACING_V2", "").lower() == "true":
        api_key = os.getenv("LANGCHAIN_API_KEY")
        project = os.getenv("LANGCHAIN_PROJECT", "predict-lotto-nz")
        
        if api_key:
            print(f"✓ LangSmith tracing enabled for project: {project}")
        else:
            print("⚠ LangSmith tracing enabled but LANGCHAIN_API_KEY not set")
    else:
        print("ℹ LangSmith tracing disabled")


# Example usage
if __name__ == "__main__":
    # Setup environment
    setup_langsmith_env()
    
    # Example traced function
    @trace_prediction(name="example_prediction", tags=["test"])
    def generate_numbers(count: int = 6):
        """Generate random lottery numbers"""
        import random
        numbers = sorted(random.sample(range(1, 41), count))
        
        # Log prediction details
        langsmith.log_prediction(
            prediction_numbers=numbers,
            confidence_score=0.75,
            model_name="random-v1",
            metadata={"method": "random_sampling"}
        )
        
        return numbers
    
    # Test the function
    result = generate_numbers(6)
    print(f"Generated numbers: {result}")
    
    # Example feedback
    # langsmith.create_feedback(
    #     run_id="<run_id_from_trace>",
    #     key="accuracy",
    #     score=0.5,
    #     comment="3 out of 6 numbers matched"
    # )
