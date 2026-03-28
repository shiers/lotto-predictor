# API Documentation

This document provides comprehensive API documentation for PredictLottoNZ services.

## 🌐 Base URLs

| Service | Local Development | Production |
|---------|------------------|------------|
| Backend API | `http://localhost:5000` | `https://api.your-domain.com` |
| Predictor API | `http://localhost:8000` | `https://predictor.your-domain.com` |
| Frontend | `http://localhost:3000` | `https://your-domain.com` |

## 🔐 Authentication

Currently, the API does not require authentication. Future versions will implement:
- JWT token-based authentication
- API key authentication for external services
- Role-based access control

## 📊 Backend API (.NET Core)

### Health Check

#### GET /api/health
Check the health status of the backend service.

**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0"
}
```

### Lottery Data Management

#### POST /api/lotto/upload
Upload a CSV file containing historical lottery data.

**Request:**
- Content-Type: `multipart/form-data`
- Body: `file` (CSV file)

**Response:**
```json
{
  "recordsAdded": 150,
  "recordsSkipped": 5,
  "message": "Import completed successfully"
}
```

**Error Responses:**
- `400 Bad Request`: Invalid file format or corrupted data
- `413 Payload Too Large`: File size exceeds limit
- `500 Internal Server Error`: Server processing error

**Example:**
```bash
curl -X POST \
  -F "file=@powerball-data.csv" \
  http://localhost:5000/api/lotto/upload
```

#### GET /api/lotto/exists/{drawNumber}
Check if a specific lottery draw exists in the database.

**Parameters:**
- `drawNumber` (path): Integer draw number to check

**Response:**
```json
{
  "exists": true,
  "drawNumber": 1234
}
```

**Example:**
```bash
curl http://localhost:5000/api/lotto/exists/1234
```

#### GET /api/lotto/latest
Retrieve the most recent lottery draw information.

**Response:**
```json
{
  "draw": 1234,
  "date": "2024-01-15T00:00:00Z",
  "winningNumbers": [5, 12, 18, 25, 33, 40],
  "bonusNumber": 7,
  "powerball": 3,
  "division1Winners": 2,
  "division1Prize": 500000.00
}
```

**Error Responses:**
- `404 Not Found`: No draws exist in database

**Example:**
```bash
curl http://localhost:5000/api/lotto/latest
```

### Number Combinations

#### POST /api/combinations/upload
Upload number combinations from various file formats (CSV, TXT, PDF).

**Request:**
- Content-Type: `multipart/form-data`
- Body: `file` (CSV, TXT, or PDF file)

**Response:**
```json
{
  "recordsAdded": 25,
  "recordsSkipped": 2,
  "message": "Combinations uploaded successfully"
}
```

**File Format Examples:**

CSV Format:
```csv
Number1,Number2,Number3,Number4,Number5,Number6
5,12,18,25,33,40
7,14,21,28,35,42
```

TXT Format:
```
5,12,18,25,33,40
7,14,21,28,35,42
```

**Example:**
```bash
curl -X POST \
  -F "file=@combinations.csv" \
  http://localhost:5000/api/combinations/upload
```

#### GET /api/combinations/predictions
Generate and retrieve lottery number predictions.

**Query Parameters:**
- `count` (optional): Number of predictions to generate (1-10, default: 5)
- `provider` (optional): Preferred prediction provider (`frequency`, `ml`, `gpt`, `aws`)

**Response:**
```json
{
  "predictions": [
    {
      "numbers": [5, 12, 18, 25, 33, 40],
      "score": 85.5,
      "source": "frequency",
      "confidence": 0.75
    },
    {
      "numbers": [7, 14, 21, 28, 35, 42],
      "score": 82.3,
      "source": "ml",
      "confidence": 0.68
    }
  ],
  "generatedAt": "2024-01-15T10:30:00Z",
  "totalPredictions": 2
}
```

**Example:**
```bash
curl "http://localhost:5000/api/combinations/predictions?count=5"
```

### Training Data Export

#### GET /api/training-data/export
Export training data for machine learning model development.

**Query Parameters:**
- `format` (optional): Export format (`json`, `csv`, default: `json`)
- `limit` (optional): Maximum number of records (default: 1000)
- `startDate` (optional): Start date filter (ISO 8601 format)
- `endDate` (optional): End date filter (ISO 8601 format)

**Response:**
```json
{
  "draws": [
    {
      "draw": 1234,
      "date": "2024-01-15T00:00:00Z",
      "numbers": [5, 12, 18, 25, 33, 40],
      "bonusNumber": 7,
      "powerball": 3
    }
  ],
  "predictions": [
    {
      "id": 1,
      "numbers": [5, 12, 18, 25, 33, 40],
      "source": "frequency",
      "createdAt": "2024-01-15T10:30:00Z",
      "requestPayload": "...",
      "responsePayload": "..."
    }
  ],
  "combinations": [
    {
      "id": 1,
      "numbers": [5, 12, 18, 25, 33, 40],
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ],
  "exportedAt": "2024-01-15T10:30:00Z",
  "totalRecords": 1500
}
```

**Example:**
```bash
curl "http://localhost:5000/api/training-data/export?format=json&limit=100"
```

## 🤖 Predictor API (Python FastAPI)

### Health Check

#### GET /health
Check the health status of the predictor service.

**Response:**
```json
{
  "status": "healthy",
  "service": "prediction-service",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Root Endpoint

#### GET /
Basic service information.

**Response:**
```json
{
  "message": "PredictLottoNZ Prediction Service is running",
  "version": "1.0.0",
  "docs": "/docs"
}
```

### Prediction Generation

#### POST /predict
Generate lottery number predictions using ML and GPT models.

**Request Body:**
```json
{
  "weekly_numbers": [5.0, 12.0, 18.0, 20.0, 33.0, 40.0]
}
```

**Response:**
```json
{
  "ml_prediction": 25.5,
  "gpt_prediction": 28.3,
  "blended_prediction": 26.9,
  "confidence_score": 0.72,
  "processing_time_ms": 1250
}
```

**Error Responses:**
- `400 Bad Request`: Invalid input format or number range
- `500 Internal Server Error`: Model processing error

**Validation Rules:**
- `weekly_numbers` must contain exactly 6 numbers
- All numbers must be between 1 and 40
- Numbers should be unique (duplicates allowed but not recommended)

**Example:**
```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -d '{"weekly_numbers": [5, 12, 18, 20, 33, 40]}' \
  http://localhost:8000/predict
```

### Model Training

#### POST /train
Train the ML model with historical lottery data.

**Request Body:**
```json
{
  "historical_data": [
    [5, 12, 18, 25, 33, 40],
    [7, 14, 21, 28, 35, 42],
    [1, 8, 15, 22, 29, 36]
  ]
}
```

**Response:**
```json
{
  "status": "success",
  "message": "ML model trained with 3 historical combinations",
  "training_time_ms": 5000,
  "model_accuracy": 0.85
}
```

**Error Responses:**
- `400 Bad Request`: Invalid training data format
- `500 Internal Server Error`: Training process error

**Example:**
```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -d '{"historical_data": [[5,12,18,25,33,40], [7,14,21,28,35,42]]}' \
  http://localhost:8000/train
```

## 📝 Data Models

### LottoDrawDto
```typescript
interface LottoDrawDto {
  draw: number;                    // Draw number
  date: string;                    // ISO 8601 date string
  winningNumbers: number[];        // Array of 6 winning numbers
  bonusNumber: number;             // Bonus number
  powerball: number;               // Powerball number
  division1Winners?: number;       // Optional: Number of division 1 winners
  division1Prize?: number;         // Optional: Division 1 prize amount
}
```

### ImportResult
```typescript
interface ImportResult {
  recordsAdded: number;           // Number of new records added
  recordsSkipped: number;         // Number of duplicate records skipped
  message: string;                // Success/error message
  errors?: string[];              // Optional: List of parsing errors
}
```

### PredictionResult
```typescript
interface PredictionResult {
  numbers: number[];              // Array of 6 predicted numbers
  score: number;                  // Prediction confidence score
  source: string;                 // Prediction source (frequency, ml, gpt, aws)
  confidence?: number;            // Optional: Confidence level (0-1)
  metadata?: any;                 // Optional: Additional prediction metadata
}
```

### PredictRequest
```python
class PredictRequest(BaseModel):
    weekly_numbers: List[float] = Field(
        ..., 
        description="Weekly lottery numbers for prediction analysis",
        min_items=6,
        max_items=6
    )
```

### PredictResponse
```python
class PredictResponse(BaseModel):
    ml_prediction: float = Field(..., description="Machine learning model prediction")
    gpt_prediction: float = Field(..., description="GPT-based prediction")
    blended_prediction: float = Field(..., description="Blended prediction")
    confidence_score: Optional[float] = Field(None, description="Overall confidence")
    processing_time_ms: Optional[int] = Field(None, description="Processing time")
```

## ⚠️ Error Handling

### Standard Error Response Format
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input data",
    "details": "Numbers must be between 1 and 40",
    "timestamp": "2024-01-15T10:30:00Z",
    "path": "/api/combinations/upload"
  }
}
```

### Common Error Codes

| Code | Description | HTTP Status |
|------|-------------|-------------|
| `VALIDATION_ERROR` | Input validation failed | 400 |
| `FILE_FORMAT_ERROR` | Unsupported file format | 400 |
| `DUPLICATE_DRAW` | Draw already exists | 409 |
| `NOT_FOUND` | Resource not found | 404 |
| `SERVICE_UNAVAILABLE` | External service unavailable | 503 |
| `INTERNAL_ERROR` | Server processing error | 500 |

## 🔄 Rate Limiting

### Current Limits
- File uploads: 10 requests per minute per IP
- Prediction requests: 60 requests per minute per IP
- General API: 100 requests per minute per IP

### Rate Limit Headers
```http
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1642248600
```

## 📊 Response Codes

### Success Codes
- `200 OK`: Request successful
- `201 Created`: Resource created successfully
- `202 Accepted`: Request accepted for processing

### Client Error Codes
- `400 Bad Request`: Invalid request format
- `401 Unauthorized`: Authentication required
- `403 Forbidden`: Access denied
- `404 Not Found`: Resource not found
- `409 Conflict`: Resource conflict (duplicate)
- `413 Payload Too Large`: File size exceeded
- `422 Unprocessable Entity`: Validation error
- `429 Too Many Requests`: Rate limit exceeded

### Server Error Codes
- `500 Internal Server Error`: Server processing error
- `502 Bad Gateway`: Upstream service error
- `503 Service Unavailable`: Service temporarily unavailable
- `504 Gateway Timeout`: Upstream service timeout

## 🧪 Testing the API

### Using curl

#### Upload CSV file
```bash
curl -X POST \
  -F "file=@test-data.csv" \
  -H "Accept: application/json" \
  http://localhost:5000/api/lotto/upload
```

#### Get predictions
```bash
curl -X GET \
  "http://localhost:5000/api/combinations/predictions?count=3" \
  -H "Accept: application/json"
```

#### Generate AI predictions
```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -d '{"weekly_numbers": [5, 12, 18, 20, 33, 40]}' \
  http://localhost:8000/predict
```

### Using PowerShell

#### Upload file
```powershell
$uri = "http://localhost:5000/api/lotto/upload"
$filePath = "C:\path\to\test-data.csv"
$form = @{
    file = Get-Item -Path $filePath
}
Invoke-RestMethod -Uri $uri -Method Post -Form $form
```

#### Get predictions
```powershell
$uri = "http://localhost:5000/api/combinations/predictions?count=5"
Invoke-RestMethod -Uri $uri -Method Get
```

### Using JavaScript/Fetch

#### Upload file
```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);

const response = await fetch('/api/lotto/upload', {
  method: 'POST',
  body: formData
});

const result = await response.json();
```

#### Get predictions
```javascript
const response = await fetch('/api/combinations/predictions?count=5');
const predictions = await response.json();
```

## 📋 Interactive Documentation

### Swagger UI (Backend)
Access interactive API documentation at:
- Local: http://localhost:5000/swagger
- Production: https://api.your-domain.com/swagger

### FastAPI Docs (Predictor)
Access interactive API documentation at:
- Local: http://localhost:8000/docs
- Production: https://predictor.your-domain.com/docs

### OpenAPI Specifications
- Backend OpenAPI spec: http://localhost:5000/swagger/v1/swagger.json
- Predictor OpenAPI spec: http://localhost:8000/openapi.json

## 🔧 SDK and Client Libraries

### .NET Client
```csharp
// Example .NET client usage
var client = new HttpClient();
client.BaseAddress = new Uri("http://localhost:5000");

var formData = new MultipartFormDataContent();
formData.Add(new StreamContent(fileStream), "file", "data.csv");

var response = await client.PostAsync("/api/lotto/upload", formData);
var result = await response.Content.ReadAsStringAsync();
```

### JavaScript/TypeScript Client
```typescript
class PredictLottoClient {
  constructor(private baseUrl: string) {}

  async uploadCsv(file: File): Promise<ImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    
    const response = await fetch(`${this.baseUrl}/api/lotto/upload`, {
      method: 'POST',
      body: formData
    });
    
    return response.json();
  }

  async getPredictions(count: number = 5): Promise<PredictionResult[]> {
    const response = await fetch(
      `${this.baseUrl}/api/combinations/predictions?count=${count}`
    );
    return response.json();
  }
}
```

### Python Client
```python
import requests
from typing import List, Dict

class PredictLottoClient:
    def __init__(self, base_url: str):
        self.base_url = base_url
    
    def upload_csv(self, file_path: str) -> Dict:
        with open(file_path, 'rb') as f:
            files = {'file': f}
            response = requests.post(f"{self.base_url}/api/lotto/upload", files=files)
            return response.json()
    
    def get_predictions(self, count: int = 5) -> List[Dict]:
        response = requests.get(f"{self.base_url}/api/combinations/predictions", 
                              params={'count': count})
        return response.json()
    
    def generate_ai_prediction(self, weekly_numbers: List[float]) -> Dict:
        predictor_url = self.base_url.replace(':5000', ':8000')
        response = requests.post(f"{predictor_url}/predict", 
                               json={'weekly_numbers': weekly_numbers})
        return response.json()
```

## 📞 Support

For API support:
1. Check this documentation
2. Try the interactive documentation (Swagger/FastAPI docs)
3. Review example requests and responses
4. Create GitHub issue with API-specific details