# Start predictor service with LangSmith environment variables
$env:LANGCHAIN_TRACING_V2="true"
$env:LANGCHAIN_API_KEY="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"
$env:LANGCHAIN_PROJECT="predict-lotto-nz"
$env:LANGCHAIN_ENDPOINT="https://api.smith.langchain.com"
$env:GROQCLOUD_API_KEY="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"

Write-Host "Starting PredictLottoNZ Prediction Service with LangSmith..." -ForegroundColor Green
python main_with_langsmith.py
