# Deployment Guide

This guide covers deployment options for PredictLottoNZ across different environments.

## 🐳 Docker Deployment (Recommended)

### Local Production Deployment

1. **Prepare environment**
   ```bash
   git clone <repository-url>
   cd predict-lotto-nz
   cp .env.example .env
   ```

2. **Configure environment variables**
   ```env
   # Required
   POSTGRES_PASSWORD=your_secure_password
   
   # Optional AI services
   OPENAI_API_KEY=your_openai_key
   AWS_ACCESS_KEY_ID=your_aws_key
   AWS_SECRET_ACCESS_KEY=your_aws_secret
   ```

3. **Deploy services**
   ```bash
   docker-compose up -d
   ```

4. **Verify deployment**
   ```bash
   ./scripts/test-docker.ps1 -HealthCheck
   ```

### Development Deployment

```bash
# Start with hot reload
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# View logs
docker-compose logs -f
```

## ☁️ Cloud Deployment

### AWS Deployment

#### Option 1: EC2 with Docker Compose

1. **Launch EC2 instance**
   - Ubuntu 20.04 LTS
   - t3.medium or larger
   - Security groups: 22, 80, 443, 3000, 5000, 8000

2. **Install Docker**
   ```bash
   sudo apt update
   sudo apt install -y docker.io docker-compose
   sudo usermod -aG docker $USER
   ```

3. **Deploy application**
   ```bash
   git clone <repository-url>
   cd predict-lotto-nz
   cp .env.example .env
   # Configure environment variables
   docker-compose up -d
   ```

4. **Set up reverse proxy (optional)**
   ```nginx
   # /etc/nginx/sites-available/predict-lotto-nz
   server {
       listen 80;
       server_name your-domain.com;
       
       location / {
           proxy_pass http://localhost:3000;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
       }
       
       location /api/ {
           proxy_pass http://localhost:5000;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
       }
   }
   ```

#### Option 2: ECS with Fargate

1. **Create ECS cluster**
   ```bash
   aws ecs create-cluster --cluster-name predict-lotto-cluster
   ```

2. **Create task definitions**
   ```json
   {
     "family": "predict-lotto-backend",
     "networkMode": "awsvpc",
     "requiresCompatibilities": ["FARGATE"],
     "cpu": "512",
     "memory": "1024",
     "containerDefinitions": [
       {
         "name": "backend",
         "image": "your-registry/predict-lotto-backend:latest",
         "portMappings": [
           {
             "containerPort": 80,
             "protocol": "tcp"
           }
         ],
         "environment": [
           {
             "name": "ConnectionStrings__DefaultConnection",
             "value": "your-connection-string"
           }
         ]
       }
     ]
   }
   ```

3. **Create services**
   ```bash
   aws ecs create-service \
     --cluster predict-lotto-cluster \
     --service-name predict-lotto-backend \
     --task-definition predict-lotto-backend \
     --desired-count 1 \
     --launch-type FARGATE
   ```

### Azure Deployment

#### Container Instances

1. **Create resource group**
   ```bash
   az group create --name predict-lotto-rg --location eastus
   ```

2. **Deploy containers**
   ```bash
   # Backend
   az container create \
     --resource-group predict-lotto-rg \
     --name predict-lotto-backend \
     --image your-registry/predict-lotto-backend:latest \
     --ports 80 \
     --environment-variables \
       ConnectionStrings__DefaultConnection="your-connection-string"
   
   # Frontend
   az container create \
     --resource-group predict-lotto-rg \
     --name predict-lotto-frontend \
     --image your-registry/predict-lotto-frontend:latest \
     --ports 80
   
   # Predictor
   az container create \
     --resource-group predict-lotto-rg \
     --name predict-lotto-predictor \
     --image your-registry/predict-lotto-predictor:latest \
     --ports 8000
   ```

### Google Cloud Platform

#### Cloud Run

1. **Build and push images**
   ```bash
   # Configure Docker for GCP
   gcloud auth configure-docker
   
   # Build and push
   docker build -t gcr.io/your-project/predict-lotto-backend ./backend
   docker push gcr.io/your-project/predict-lotto-backend
   ```

2. **Deploy services**
   ```bash
   # Backend
   gcloud run deploy predict-lotto-backend \
     --image gcr.io/your-project/predict-lotto-backend \
     --platform managed \
     --region us-central1 \
     --allow-unauthenticated
   
   # Frontend
   gcloud run deploy predict-lotto-frontend \
     --image gcr.io/your-project/predict-lotto-frontend \
     --platform managed \
     --region us-central1 \
     --allow-unauthenticated
   ```

## 🔧 Production Configuration

### Environment Variables

#### Security
```env
# Use strong passwords
POSTGRES_PASSWORD=complex_secure_password_123!

# Use production database
POSTGRES_DB=predict_lotto_nz_prod

# Set production environment
ASPNETCORE_ENVIRONMENT=Production
NODE_ENV=production
```

#### Performance
```env
# Database connection pooling
ConnectionStrings__DefaultConnection="Host=postgres;Port=5434;Database=predict_lotto_nz_prod;Username=postgres;Password=your_password;Pooling=true;MinPoolSize=5;MaxPoolSize=20"

# Logging levels
LOG_LEVEL=WARNING
ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Warning
```

### SSL/TLS Configuration

#### Using Let's Encrypt with Nginx

1. **Install Certbot**
   ```bash
   sudo apt install certbot python3-certbot-nginx
   ```

2. **Obtain certificate**
   ```bash
   sudo certbot --nginx -d your-domain.com
   ```

3. **Auto-renewal**
   ```bash
   sudo crontab -e
   # Add: 0 12 * * * /usr/bin/certbot renew --quiet
   ```

### Database Configuration

#### PostgreSQL Production Settings

```sql
-- postgresql.conf
shared_buffers = 256MB
effective_cache_size = 1GB
maintenance_work_mem = 64MB
checkpoint_completion_target = 0.9
wal_buffers = 16MB
default_statistics_target = 100
random_page_cost = 1.1
effective_io_concurrency = 200
```

#### Backup Strategy

```bash
#!/bin/bash
# backup-database.sh
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups"
DB_NAME="predict_lotto_nz_prod"

# Create backup
docker-compose exec postgres pg_dump -U postgres $DB_NAME > $BACKUP_DIR/backup_$DATE.sql

# Keep only last 7 days
find $BACKUP_DIR -name "backup_*.sql" -mtime +7 -delete
```

## 📊 Monitoring and Logging

### Health Monitoring

#### Docker Health Checks
```yaml
# docker-compose.yml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:80/api/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 30s
```

#### External Monitoring
```bash
# Simple monitoring script
#!/bin/bash
SERVICES=("http://localhost:3000/health" "http://localhost:5000/api/health" "http://localhost:8000/health")

for service in "${SERVICES[@]}"; do
  if curl -f $service > /dev/null 2>&1; then
    echo "✅ $service is healthy"
  else
    echo "❌ $service is down"
    # Send alert (email, Slack, etc.)
  fi
done
```

### Centralized Logging

#### Using ELK Stack
```yaml
# docker-compose.logging.yml
version: '3.8'
services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:7.14.0
    environment:
      - discovery.type=single-node
    ports:
      - "9200:9200"
  
  logstash:
    image: docker.elastic.co/logstash/logstash:7.14.0
    volumes:
      - ./logstash.conf:/usr/share/logstash/pipeline/logstash.conf
  
  kibana:
    image: docker.elastic.co/kibana/kibana:7.14.0
    ports:
      - "5601:5601"
    depends_on:
      - elasticsearch
```

## 🔒 Security Considerations

### Container Security

1. **Use non-root users**
   ```dockerfile
   # In Dockerfile
   RUN adduser --disabled-password --gecos '' appuser
   USER appuser
   ```

2. **Scan for vulnerabilities**
   ```bash
   # Scan images
   docker scan predict-lotto-backend:latest
   docker scan predict-lotto-frontend:latest
   docker scan predict-lotto-predictor:latest
   ```

3. **Use secrets management**
   ```bash
   # Docker secrets
   echo "your_secret_password" | docker secret create postgres_password -
   ```

### Network Security

1. **Firewall configuration**
   ```bash
   # UFW rules
   sudo ufw allow 22/tcp    # SSH
   sudo ufw allow 80/tcp    # HTTP
   sudo ufw allow 443/tcp   # HTTPS
   sudo ufw deny 5434/tcp   # Block direct database access
   sudo ufw enable
   ```

2. **Docker network isolation**
   ```yaml
   # docker-compose.yml
   networks:
     frontend:
       driver: bridge
     backend:
       driver: bridge
       internal: true  # No external access
   ```

## 🚀 Performance Optimization

### Database Optimization

1. **Indexing**
   ```sql
   -- Add indexes for common queries
   CREATE INDEX idx_lotto_draws_date ON "LottoDraws" ("Date");
   CREATE INDEX idx_predictions_created_at ON "Predictions" ("CreatedAt");
   CREATE INDEX idx_combinations_created_at ON "NumberCombinations" ("CreatedAt");
   ```

2. **Connection pooling**
   ```csharp
   // In appsettings.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=postgres;Database=predict_lotto_nz;Username=postgres;Password=password;Pooling=true;MinPoolSize=5;MaxPoolSize=20;ConnectionLifetime=300"
     }
   }
   ```

### Application Optimization

1. **Caching**
   ```csharp
   // Add Redis caching
   services.AddStackExchangeRedisCache(options =>
   {
       options.Configuration = "localhost:6379";
   });
   ```

2. **CDN for static assets**
   ```nginx
   # Nginx configuration
   location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
       expires 1y;
       add_header Cache-Control "public, immutable";
   }
   ```

## 📋 Deployment Checklist

### Pre-deployment
- [ ] Environment variables configured
- [ ] SSL certificates obtained
- [ ] Database backups configured
- [ ] Monitoring set up
- [ ] Security hardening applied
- [ ] Performance testing completed

### Deployment
- [ ] Services deployed successfully
- [ ] Health checks passing
- [ ] Database migrations applied
- [ ] Static assets served correctly
- [ ] API endpoints responding

### Post-deployment
- [ ] Monitoring alerts configured
- [ ] Backup verification
- [ ] Performance metrics baseline
- [ ] Security scan completed
- [ ] Documentation updated

## 🔄 Rollback Procedures

### Quick Rollback
```bash
# Stop current deployment
docker-compose down

# Restore previous version
git checkout previous-tag
docker-compose up -d

# Verify rollback
./scripts/test-docker.ps1 -HealthCheck
```

### Database Rollback
```bash
# Restore database backup
docker-compose exec postgres psql -U postgres -d predict_lotto_nz < backup_previous.sql

# Run down migrations if needed
docker-compose exec backend dotnet ef database update PreviousMigration
```

## 📞 Support

For deployment issues:
1. Check service logs: `docker-compose logs [service]`
2. Verify health checks: `./scripts/test-docker.ps1 -HealthCheck`
3. Review environment variables: `docker-compose config`
4. Create GitHub issue with deployment details