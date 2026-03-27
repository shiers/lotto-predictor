@echo off
REM Start predictor service with LangSmith environment variables
set LANGCHAIN_TRACING_V2=true
set LANGCHAIN_API_KEY=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
set LANGCHAIN_PROJECT=predict-lotto-nz
set LANGCHAIN_ENDPOINT=https://api.smith.langchain.com
set GROQCLOUD_API_KEY=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

echo Starting PredictLottoNZ Prediction Service with LangSmith...
python main_with_langsmith.py
