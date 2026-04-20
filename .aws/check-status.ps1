# Check status of all AWS resources
$region = "ap-southeast-2"

Write-Host "=== ECS Services ===" -ForegroundColor Cyan
aws ecs describe-services --cluster predict-lotto-cluster --services predict-lotto-frontend predict-lotto-backend predict-lotto-predictor --region $region --query "services[*].{Name:serviceName,Desired:desiredCount,Running:runningCount,Pending:pendingCount}" --output table

Write-Host "=== RDS ===" -ForegroundColor Cyan
aws rds describe-db-instances --region $region --query "DBInstances[*].{ID:DBInstanceIdentifier,Status:DBInstanceStatus}" --output table

Write-Host "=== ElastiCache ===" -ForegroundColor Cyan
aws elasticache describe-cache-clusters --region $region --query "CacheClusters[*].{ID:CacheClusterId,Status:CacheClusterStatus}" --output table

Write-Host "=== ALB ===" -ForegroundColor Cyan
aws elbv2 describe-load-balancers --region $region --query "LoadBalancers[*].{Name:LoadBalancerName,State:State.Code}" --output table

Write-Host "=== VPC Endpoints ===" -ForegroundColor Cyan
aws ec2 describe-vpc-endpoints --region $region --query "VpcEndpoints[?State!='deleted'].{ID:VpcEndpointId,Service:ServiceName,State:State}" --output table
