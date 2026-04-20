# Startup script - restores all services
# Run this to bring everything back up

$region = "ap-southeast-2"
$cluster = "predict-lotto-cluster"

Write-Host "Starting RDS instance..."
aws rds start-db-instance --db-instance-identifier predict-lotto-db --region $region | Out-Null
Write-Host "RDS starting (takes 5-10 minutes, wait before starting ECS)"

Write-Host "Waiting 5 minutes for RDS to start..."
Start-Sleep -Seconds 300

Write-Host "Scaling ECS services back up..."
aws ecs update-service --cluster $cluster --service predict-lotto-frontend --desired-count 1 --network-configuration "awsvpcConfiguration={subnets=[subnet-a29f35c5,subnet-01379248,subnet-85a6a0dc],securityGroups=[sg-061d9290300e7f647],assignPublicIp=ENABLED}" --region $region | Out-Null
aws ecs update-service --cluster $cluster --service predict-lotto-backend --desired-count 1 --network-configuration "awsvpcConfiguration={subnets=[subnet-a29f35c5,subnet-01379248,subnet-85a6a0dc],securityGroups=[sg-061d9290300e7f647],assignPublicIp=ENABLED}" --region $region | Out-Null
aws ecs update-service --cluster $cluster --service predict-lotto-predictor --desired-count 1 --network-configuration "awsvpcConfiguration={subnets=[subnet-a29f35c5,subnet-01379248,subnet-85a6a0dc],securityGroups=[sg-061d9290300e7f647],assignPublicIp=ENABLED}" --region $region | Out-Null
Write-Host "ECS services scaling up"

Write-Host ""
Write-Host "NOTE: If you deleted VPC endpoints, recreate them before ECS tasks start:"
Write-Host "  - com.amazonaws.ap-southeast-2.secretsmanager (Interface)"
Write-Host "  - com.amazonaws.ap-southeast-2.ecr.api (Interface)"
Write-Host "  - com.amazonaws.ap-southeast-2.ecr.dkr (Interface)"
Write-Host "  - com.amazonaws.ap-southeast-2.logs (Interface)"
Write-Host "  - com.amazonaws.ap-southeast-2.s3 (Gateway)"
Write-Host "  All with security group: sg-061d9290300e7f647, subnets: all 3"
Write-Host ""
Write-Host "Startup initiated. Check ECS cluster in a few minutes."
