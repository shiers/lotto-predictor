# LottoLens Frontend

Vue.js frontend application for the LottoLens lottery analysis system.

## Features

- File upload with progress tracking (CSV, TXT, PDF)
- Real-time prediction generation and display
- Latest lottery draw information
- Responsive design with mobile support
- Toast notifications for user feedback
- Side navigation menu

## Tech Stack

- Vue 3 with TypeScript
- Pinia for state management
- Vue Router for navigation
- Axios for HTTP requests
- Vite for build tooling

## Development

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Type checking
npm run type-check

# Build for production
npm run build

# Preview production build
npm run preview
```

## Environment Variables

Copy `.env.example` to `.env` and configure:

- `VITE_API_BASE_URL`: Backend API base URL
- `VITE_APP_TITLE`: Application title
- `VITE_APP_VERSION`: Application version

## Components

- **FileUpload**: Handles file uploads with progress tracking
- **PredictionsView**: Displays generated lottery predictions
- **SideMenu**: Navigation menu with toast notifications
- **LatestDraw**: Shows the most recent lottery draw information

## API Integration

The frontend communicates with the .NET Core backend API for:

- File uploads (`/api/lotto/upload`, `/api/combinations/upload`)
- Prediction generation (`/api/combinations/predictions`)
- Latest draw retrieval (`/api/lotto/latest`)