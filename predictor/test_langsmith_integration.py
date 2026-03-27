"""
Test script for LangSmith integration

Run this to verify LangSmith is working correctly with your setup.
"""

import os
import sys
from datetime import datetime

# Add current directory to path
sys.path.insert(0, os.path.dirname(__file__))

from langsmith_helper import langsmith, trace_prediction, setup_langsmith_env


def test_environment():
    """Test environment configuration"""
    print("\n=== Testing Environment Configuration ===")
    
    env_vars = {
        "LANGCHAIN_TRACING_V2": os.getenv("LANGCHAIN_TRACING_V2"),
        "LANGCHAIN_API_KEY": os.getenv("LANGCHAIN_API_KEY", "")[:20] + "..." if os.getenv("LANGCHAIN_API_KEY") else None,
        "LANGCHAIN_PROJECT": os.getenv("LANGCHAIN_PROJECT"),
        "LANGCHAIN_ENDPOINT": os.getenv("LANGCHAIN_ENDPOINT")
    }
    
    for key, value in env_vars.items():
        status = "✓" if value else "✗"
        print(f"{status} {key}: {value or 'Not set'}")
    
    return all(env_vars.values())


def test_client_connection():
    """Test LangSmith client connection"""
    print("\n=== Testing LangSmith Client Connection ===")
    
    if not langsmith.enabled:
        print("✗ LangSmith is not enabled")
        return False
    
    if not langsmith.client:
        print("✗ LangSmith client not initialized")
        return False
    
    try:
        # Test connection by getting client info
        info = langsmith.client.info()
        print(f"✓ Connected to LangSmith")
        print(f"  Project: {langsmith.project_name}")
        return True
    except Exception as e:
        print(f"✗ Failed to connect: {e}")
        return False


@trace_prediction(name="test_simple_prediction", tags=["test", "integration"])
def test_simple_trace():
    """Test simple function tracing"""
    print("\n=== Testing Simple Trace ===")
    
    # Add metadata
    langsmith.add_metadata({
        "test_type": "simple_trace",
        "timestamp": datetime.utcnow().isoformat()
    })
    
    # Simulate prediction
    numbers = [5, 12, 18, 25, 33, 40]
    
    langsmith.log_prediction(
        prediction_numbers=numbers,
        powerball=8,
        confidence_score=0.75,
        model_name="test-model-v1",
        metadata={
            "method": "test",
            "environment": "development"
        }
    )
    
    print(f"✓ Generated test prediction: {numbers}")
    print(f"  Check LangSmith dashboard: https://smith.langchain.com")
    
    return numbers


@trace_prediction(name="test_error_handling", tags=["test", "error"])
def test_error_trace():
    """Test error logging"""
    print("\n=== Testing Error Trace ===")
    
    try:
        # Simulate an error
        raise ValueError("This is a test error for LangSmith tracing")
    except Exception as e:
        langsmith.log_error(e, {
            "test_type": "error_handling",
            "expected": True
        })
        print(f"✓ Error logged to LangSmith: {e}")
        return None


def test_metadata_and_tags():
    """Test metadata and tags"""
    print("\n=== Testing Metadata and Tags ===")
    
    @trace_prediction(name="test_metadata", tags=["test", "metadata"])
    def sample_function():
        langsmith.add_metadata({
            "custom_field_1": "value1",
            "custom_field_2": 42,
            "nested": {
                "field": "nested_value"
            }
        })
        
        langsmith.add_tags(["additional-tag", "test-run"])
        
        return "success"
    
    result = sample_function()
    print(f"✓ Metadata and tags added to trace")
    return result


def run_all_tests():
    """Run all integration tests"""
    print("=" * 60)
    print("LangSmith Integration Test Suite")
    print("=" * 60)
    
    # Setup environment
    setup_langsmith_env()
    
    # Run tests
    tests = [
        ("Environment Configuration", test_environment),
        ("Client Connection", test_client_connection),
        ("Simple Trace", test_simple_trace),
        ("Error Trace", test_error_trace),
        ("Metadata and Tags", test_metadata_and_tags)
    ]
    
    results = []
    for test_name, test_func in tests:
        try:
            result = test_func()
            results.append((test_name, True, result))
        except Exception as e:
            print(f"✗ Test failed: {e}")
            results.append((test_name, False, str(e)))
    
    # Summary
    print("\n" + "=" * 60)
    print("Test Summary")
    print("=" * 60)
    
    passed = sum(1 for _, success, _ in results if success)
    total = len(results)
    
    for test_name, success, result in results:
        status = "✓ PASS" if success else "✗ FAIL"
        print(f"{status}: {test_name}")
    
    print(f"\nTotal: {passed}/{total} tests passed")
    
    if langsmith.enabled:
        print(f"\n🔗 View traces at: https://smith.langchain.com/o/default/projects/p/{langsmith.project_name}")
    else:
        print("\n⚠ LangSmith is not enabled. Set LANGCHAIN_TRACING_V2=true to enable tracing.")
    
    return passed == total


if __name__ == "__main__":
    success = run_all_tests()
    sys.exit(0 if success else 1)
