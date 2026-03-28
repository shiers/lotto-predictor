# PredictLottoNZ Project Structure

## Overview

This is a monorepo containing three main services that work together to provide lottery prediction functionality.

## Directory Structure

```
predict-lotto-nz/
├── backend/                    # .NET Core Web API Service
│   ├── Controllers/           # API Controllers
│   ├── Services/             # Business Logic Services
│   ├── Models/               # Data Models and DTOs
│   ├── Data/                 # Entity Framework DbContext
│   ├── Migrations/           # EF Core Migrations
│   ├── Dockerfile            # Production Docker image
│   ├── Dockerfile.dev        # Development Docker image
│   └── appsettings.json      # Configuration
│
├── frontend/                   # Vue.js Single Page Application
│   ├── src/
│   │   ├── components/       # Vue Components
│   │   ├── views/           # Page Views
│   │   ├── services/        # API Service Layer
│   │   ├── store/           # Vuex/Pinia State Management
│   │   └── assets/          # Static Assets
│   ├── public/              # Public Assets
│   ├── Dockerfile           # Production Docker image
│   ├── Dockerfile.dev       # Development Docker image
│   └── package.json         # Dependencies
│
├── predictor/                  # Python FastAPI Prediction Service
│   ├── app/
│   │   ├── models/          # ML Models
│   │   ├── services/        # Prediction Services
│   │   ├── routers/         # FastAPI Routers
│   │   └── schemas/         # Pydantic Models
│   ├── data/                # Training Data
│   ├── models/              # Trained Model Files
│   ├── Dockerfile           # Production Docker image
│   ├── Dockerfile.dev       # Development Docker image
│   └── requirements.txt     # Python Dependencies
│
├── scripts/                    # Deployment and Utility Scripts
│   ├── init-db.sql          # Database Initialization
│   ├── dev-data.sql         # Development Test Data
│   └── deploy.sh            # Deployment Script
│
├── .kiro/                      # Kiro Specification Files
│   └── specs/
│       └── predict-lotto-nz/
│           ├── requirements.md
│           ├── design.md
│           └── tasks.md
│
├── docker-compose.yml          # Production Docker Compose
├── docker-compose.dev.yml      # Development Docker Compose
├── .env                        # Environment Variables
├── .env.example               # Environment Template
├── .gitignore                 # Git Ignore Rules
├── Makefile                   # Development Commands
├── README.md                  # Project Documentation
└── PROJECT_STRUCTURE.md       # This File
```

## Service Communication

### Frontend → Backend
- HTTP REST API calls
- JSON request/response format
- File uploads via multipart/form-data

### Backend → Database
- Entity Framework Core ORM
- PostgreSQL database
- Async/await patterns

### Backend → Predictor
- HTTP REST API calls
- JSON request/response format
- Fallback chain: AWS LLM → FastAPI → Frequency

### Predictor → External Services
- OpenAI GPT API integration
- AWS LLM services (future)
- ML model inference

## Development Workflow

1. **Local Development**: Use docker-compose.dev.yml for hot-reload development
2. **Testing**: Each service has its own test suite
3. **Integration**: Full stack testing with docker-compose.yml
4. **Deployment**: Production deployment with environment-specific configurations

## Key Technologies

- **Backend**: .NET 6, Entity Framework Core, PostgreSQL
- **Frontend**: Vue.js 3, TypeScript, Axios, Vuex/Pinia
- **Predictor**: Python 3.9+, FastAPI, scikit-learn, OpenAI API
- **Infrastructure**: Docker, Docker Compose, PostgreSQL
- **Testing**: xUnit (.NET), Jest (Vue.js), pytest (Python)

## Getting Started

1. Copy `.env.example` to `.env` and configure
2. Run `make dev-setup` to initialize development environment
3. Run `make up` to start all services
4. Access frontend at http://localhost:3000

See README.md for detailed setup instructions.