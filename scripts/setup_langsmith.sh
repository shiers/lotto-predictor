#!/bin/bash

# LangSmith Setup Script
# This script helps set up and verify LangSmith integration

set -e

echo "=========================================="
echo "LangSmith Integration Setup"
echo "=========================================="
echo ""

# Check if .env file exists
if [ ! -f ".env" ]; then
    echo "❌ Error: .env file not found"
    echo "Please create .env file from .env.example"
    exit 1
fi

echo "✓ Found .env file"

# Check if LangSmith variables are set
if ! grep -q "LANGCHAIN_API_KEY" .env; then
    echo "❌ Error: LANGCHAIN_API_KEY not found in .env"
    echo "Please add LangSmith configuration to .env"
    exit 1
fi

echo "✓ LangSmith configuration found in .env"

# Check if predictor directory exists
if [ ! -d "predictor" ]; then
    echo "❌ Error: predictor directory not found"
    exit 1
fi

echo "✓ Found predictor directory"

# Install Python dependencies
echo ""
echo "Installing Python dependencies..."
cd predictor

if [ -f "requirements.txt" ]; then
    pip install -r requirements.txt
    echo "✓ Dependencies installed"
else
    echo "❌ Error: requirements.txt not found"
    exit 1
fi

# Run integration test
echo ""
echo "Running LangSmith integration test..."
python test_langsmith_integration.py

if [ $? -eq 0 ]; then
    echo ""
    echo "=========================================="
    echo "✓ LangSmith Setup Complete!"
    echo "=========================================="
    echo ""
    echo "Next steps:"
    echo "1. Restart your services: docker-compose restart predictor"
    echo "2. Generate predictions to create traces"
    echo "3. View traces at: https://smith.langchain.com"
    echo ""
else
    echo ""
    echo "❌ Integration test failed"
    echo "Please check the error messages above"
    exit 1
fi
