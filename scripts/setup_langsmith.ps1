# LangSmith Setup Script for Windows PowerShell
# This script helps set up and verify LangSmith integration

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "LangSmith Integration Setup" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Check if .env file exists
if (-not (Test-Path ".env")) {
    Write-Host "❌ Error: .env file not found" -ForegroundColor Red
    Write-Host "Please create .env file from .env.example"
    exit 1
}

Write-Host "✓ Found .env file" -ForegroundColor Green

# Check if LangSmith variables are set
$envContent = Get-Content ".env" -Raw
if ($envContent -notmatch "LANGCHAIN_API_KEY") {
    Write-Host "❌ Error: LANGCHAIN_API_KEY not found in .env" -ForegroundColor Red
    Write-Host "Please add LangSmith configuration to .env"
    exit 1
}

Write-Host "✓ LangSmith configuration found in .env" -ForegroundColor Green

# Check if predictor directory exists
if (-not (Test-Path "predictor")) {
    Write-Host "❌ Error: predictor directory not found" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Found predictor directory" -ForegroundColor Green

# Install Python dependencies
Write-Host ""
Write-Host "Installing Python dependencies..." -ForegroundColor Yellow
Set-Location predictor

if (Test-Path "requirements.txt") {
    pip install -r requirements.txt
    Write-Host "✓ Dependencies installed" -ForegroundColor Green
} else {
    Write-Host "❌ Error: requirements.txt not found" -ForegroundColor Red
    exit 1
}

# Run integration test
Write-Host ""
Write-Host "Running LangSmith integration test..." -ForegroundColor Yellow
python test_langsmith_integration.py

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "✓ LangSmith Setup Complete!" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Restart your services: docker-compose restart predictor"
    Write-Host "2. Generate predictions to create traces"
    Write-Host "3. View traces at: https://smith.langchain.com"
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "❌ Integration test failed" -ForegroundColor Red
    Write-Host "Please check the error messages above"
    exit 1
}
