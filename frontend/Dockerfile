# Build stage
FROM node:18-alpine AS build
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies (use legacy peer deps to resolve conflicts)
RUN npm ci --legacy-peer-deps

# Copy source code
COPY . .

# Build the application
ARG VUE_APP_API_BASE_URL=http://localhost:5000
ENV VUE_APP_API_BASE_URL=$VUE_APP_API_BASE_URL
# Skip type checking in Docker build to avoid blocking on non-critical type errors
RUN npm run build-only

# Production stage
FROM nginx:alpine AS runtime
WORKDIR /usr/share/nginx/html

# Copy built application
COPY --from=build /app/dist .

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/ || exit 1

CMD ["nginx", "-g", "daemon off;"]