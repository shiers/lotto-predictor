@echo off
REM Start predictor service with LangSmith environment variables
REM Set these environment variables in your shell or .env file before running:
REM   LANGCHAIN_API_KEY, GROQCLOUD_API_KEY

set LANGCHAIN_TRACING_V2=true
set LANGCHAIN_PROJECT=predict-lotto-nz
set LANGCHAIN_ENDPOINT=https://api.smith.langchain.com

if "%LANGCHAIN_API_KEY%"=="" (
    echo ERROR: LANGCHAIN_API_KEY environment variable is not set
    exit /b 1
)
if "%GROQCLOUD_API_KEY%"=="" (
    echo ERROR: GROQCLOUD_API_KEY environment variable is not set
    exit /b 1
)

echo Starting PredictLottoNZ Prediction Service with LangSmith...
python main_with_langsmith.py
