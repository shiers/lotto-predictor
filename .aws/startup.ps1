# Startup script - restores all services
# Run this to bring everything back up

$region = "ap-southeast-2"
$cluster = "predict-lotto-cluster"

Write-Host "Step 1: Restore RDS from snapshot..."
Write-Host "Run this command and wait 10-15 minutes before continuing:"
Write-Host ""
Write-Host "aws rds restore-db-instance-from-db-snapshot --db-instance-identifier predict-lotto-db --db-snapshot-identifier predict-lotto-db-final-snapshot --db-instance-class db.t3.micro --no-multi-az --vpc-security-group-ids sg-39560b5f --region $region"
Write-Host ""
Write-Host "Check RDS status with:"
Write-Host "aws rds describe-db-instances --region $region --query 'DBInstances[*].{ID:DBInstanceIdentifier,Status:DBInstanceStatus}' --output table"
Write-Host ""
Read-Host "Press Enter once RDS status is 'available' to continue"

Write-Host ""
Write-Host "Step 2: Recreate ElastiCache..."
aws elasticache create-replication-group `
  --replication-group-id predict-lotto-redis `
  --replication-group-description "predict-lotto redis cache" `
  --num-cache-clusters 1 `
  --cache-node-type cache.t3.micro `
  --engine redis `
  --security-group-ids sg-39560b5f `
  --region $region | Out-Null
Write-Host "ElastiCache creating (takes 5-10 minutes)"
Write-Host ""
Write-Host "Check status with:"
Write-Host "aws elasticache describe-replication-groups --region $region --query 'ReplicationGroups[*].{ID:ReplicationGroupId,Status:Status}' --output table"
Write-Host ""
Read-Host "Press Enter once ElastiCache status is 'available' to continue"

Write-Host ""
Write-Host "Step 3: Recreate ALB..."
Write-Host "Follow the steps in docs/aws-alb-recreation.md to recreate the ALB and target groups."
Write-Host "This must be done manually in the AWS Console."
Write-Host ""
Read-Host "Press Enter once ALB is created and Route 53 is updated to continue"

Write-Host ""
Write-Host "Step 4: Scale ECS services back up..."
aws ecs update-service --cluster $cluster --service predict-lotto-frontend --desired-count 1 --network-configuration "awsvpcConfiguration={subnets=[subnet-a29f35c5,subnet-01379248,subnet-85a6a0dc],securityGroups=[sg-061d9290300e7f647],assignPublicIp=ENABLED}" --region $region | Out-Null
aws ecs update-service --cluster $cluster --service predict-lotto-backend --desired-count 1 --network-configuration "awsvpcConfiguration={subnets=[subnet-a29f35c5,subnet-01379248,subnet-85a6a0dc],securityGroups=[sg-061d9290300e7f647],assignPublicIp=ENABLED}" --region $region | Out-Null
aws ecs update-service --cluster $cluster --service predict-lotto-predictor --desired-count 1 --network-configuration "awsvpcConfiguration={subnets=[subnet-a29f35c5,subnet-01379248,subnet-85a6a0dc],securityGroups=[sg-061d9290300e7f647],assignPublicIp=ENABLED}" --region $region | Out-Null
Write-Host "ECS services scaling up"

Write-Host ""
Write-Host "NOTE: Do NOT create VPC endpoints - they are not needed and cost ~$29/month."
Write-Host "The network ACL and security groups are already configured correctly."
Write-Host ""
Write-Host "Monitor ECS tasks at:"
Write-Host "https://ap-southeast-2.console.aws.amazon.com/ecs/v2/clusters/predict-lotto-cluster/tasks"
Write-Host ""
Write-Host "Startup complete. Check https://lottonz.dryad.ca in a few minutes."
