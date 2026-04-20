# Shutdown script - scales all services to 0 and stops RDS
# Run this to stop all recurring costs

$region = "ap-southeast-2"
$cluster = "predict-lotto-cluster"

Write-Host "Scaling ECS services to 0..."
aws ecs update-service --cluster $cluster --service predict-lotto-frontend --desired-count 0 --region $region | Out-Null
aws ecs update-service --cluster $cluster --service predict-lotto-backend --desired-count 0 --region $region | Out-Null
aws ecs update-service --cluster $cluster --service predict-lotto-predictor --desired-count 0 --region $region | Out-Null
Write-Host "ECS services scaled to 0"

Write-Host "Stopping RDS instance..."
aws rds stop-db-instance --db-instance-identifier predict-lotto-db --region $region | Out-Null
Write-Host "RDS stopping (takes a few minutes)"

Write-Host "Stopping ElastiCache cluster..."
aws elasticache describe-cache-clusters --region $region --query "CacheClusters[*].CacheClusterId" --output text | ForEach-Object {
    aws elasticache delete-cache-cluster --cache-cluster-id $_ --region $region | Out-Null
}
Write-Host "ElastiCache stopping"

Write-Host ""
Write-Host "NOTE: To eliminate VPC endpoint costs (~$0.01/hr each), delete these endpoints manually:"
Write-Host "  - vpce-0412a1c642cfd66db (secretsmanager)"
Write-Host "  - vpce-07c4d1e8f7865b1bb (ecr.api)"
Write-Host "  - vpce-06bc13260609b5a9c (ecr.dkr)"
Write-Host "  - vpce-0cbb06daf1d8c0266 (s3 gateway - free)"
Write-Host "  - predict-lotto-logs endpoint"
Write-Host ""
Write-Host "The ALB also incurs hourly charges. To stop it:"
Write-Host "  aws elbv2 describe-load-balancers --region $region --query 'LoadBalancers[*].LoadBalancerArn' --output text"
Write-Host "  Then: aws elbv2 delete-load-balancer --load-balancer-arn <arn> --region $region"
Write-Host ""
Write-Host "Shutdown complete. Run startup.ps1 to bring everything back."
