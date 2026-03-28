# Start predictor service with LangSmith environment variables
# Set these environment variables in your shell or .env file before running:
#   LANGCHAIN_API_KEY, GROQCLOUD_API_KEY

$env:LANGCHAIN_TRACING_V2="true"
$env:LANGCHAIN_PROJECT="predict-lotto-nz"
$env:LANGCHAIN_ENDPOINT="https://api.smith.langchain.com"

if (-not $env:LANGCHAIN_API_KEY) {
    Write-Error "ERROR: LANGCHAIN_API_KEY environment variable is not set"
    exit 1
}
if (-not $env:GROQCLOUD_API_KEY) {
    Write-Error "ERROR: GROQCLOUD_API_KEY environment variable is not set"
    exit 1
}

Write-Host "Starting PredictLottoNZ Prediction Service with LangSmith..." -ForegroundColor Green
python main_with_langsmith.py
