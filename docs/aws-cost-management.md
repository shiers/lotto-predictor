# AWS Cost Management

## Shutting Down (Zero Cost Mode)

Run the shutdown script from the repo root:

```powershell
.\.aws\shutdown.ps1
```

This will:

- Scale all 3 ECS services to 0 tasks
- Stop the RDS instance
- Stop ElastiCache

### Additional savings — delete these manually in the AWS Console

**VPC Endpoints** (biggest cost — ~$21/month combined):
Go to [VPC → Endpoints](https://ap-southeast-2.console.aws.amazon.com/vpc/home?region=ap-southeast-2#Endpoints:) and delete:

- `vpce-0412a1c642cfd66db` — secretsmanager
- `vpce-07c4d1e8f7865b1bb` — ecr.api
- `vpce-06bc13260609b5a9c` — ecr.dkr
- The CloudWatch logs endpoint

Keep the S3 gateway endpoint (`vpce-0cbb06daf1d8c0266`) — it's free.

**ALB** (~$6/month):

```powershell
aws elbv2 describe-load-balancers --region ap-southeast-2 --query "LoadBalancers[*].{Name:LoadBalancerName,Arn:LoadBalancerArn}" --output table
aws elbv2 delete-load-balancer --load-balancer-arn <arn> --region ap-southeast-2
```

## Starting Back Up

If you only scaled down ECS/RDS (kept VPC endpoints and ALB):

```powershell
.\.aws\startup.ps1
```

If you also deleted VPC endpoints and/or the ALB, recreate them first:

### Recreate VPC Endpoints

Go to [VPC → Endpoints → Create endpoint](https://ap-southeast-2.console.aws.amazon.com/vpc/home?region=ap-southeast-2#CreateVpcEndpoint:) for each:

| Name                         | Service                                     | Type      |
| ---------------------------- | ------------------------------------------- | --------- |
| predict-lotto-secretsmanager | com.amazonaws.ap-southeast-2.secretsmanager | Interface |
| predict-lotto-ecr-api        | com.amazonaws.ap-southeast-2.ecr.api        | Interface |
| predict-lotto-ecr-dkr        | com.amazonaws.ap-southeast-2.ecr.dkr        | Interface |
| predict-lotto-logs           | com.amazonaws.ap-southeast-2.logs           | Interface |
| predict-lotto-s3             | com.amazonaws.ap-southeast-2.s3             | Gateway   |

For all Interface endpoints use:

- VPC: `vpc-6037d707`
- Subnets: all 3 (`subnet-a29f35c5`, `subnet-01379248`, `subnet-85a6a0dc`)
- Security group: `sg-061d9290300e7f647` (predict-lotto-ecs-sg)
- Private DNS: enabled

For the S3 Gateway endpoint use:

- VPC: `vpc-6037d707`
- Route table: `rtb-7bb4231c`

### Recreate ALB

The easiest way is to trigger a fresh GitHub Actions deployment after the VPC endpoints are up — the pipeline will handle ECS service updates. Alternatively recreate the ALB manually in EC2 → Load Balancers.

## Key Resource IDs

| Resource           | ID                                                                                                           |
| ------------------ | ------------------------------------------------------------------------------------------------------------ |
| VPC                | vpc-6037d707                                                                                                 |
| Subnets            | subnet-a29f35c5, subnet-01379248, subnet-85a6a0dc                                                            |
| Route table        | rtb-7bb4231c                                                                                                 |
| ECS security group | sg-061d9290300e7f647 (predict-lotto-ecs-sg)                                                                  |
| ALB security group | sg-039487fb281213b16 (predict-lotto-alb-sg)                                                                  |
| RDS security group | sg-39560b5f (default)                                                                                        |
| RDS instance       | predict-lotto-db                                                                                             |
| ECS cluster        | predict-lotto-cluster                                                                                        |
| ECR registry       | 897458219603.dkr.ecr.ap-southeast-2.amazonaws.com                                                            |
| ALB ARN            | arn:aws:elasticloadbalancing:ap-southeast-2:897458219603:loadbalancer/app/predict-lotto-alb/3c2df83ba25e3851 |
| ALB DNS            | Check Route 53 hosted zone for the alias record pointing to the ALB                                          |

## Recreating ElastiCache

ElastiCache cannot be stopped — it must be deleted and recreated. To recreate:

```powershell
aws elasticache create-replication-group `
  --replication-group-id predict-lotto-redis `
  --replication-group-description "predict-lotto redis cache" `
  --num-cache-clusters 1 `
  --cache-node-type cache.t3.micro `
  --engine redis `
  --engine-version 7.0 `
  --security-group-ids sg-39560b5f `
  --region ap-southeast-2
```

Wait for status to become `available` before starting ECS services. The connection string in the backend task definition uses:
`clustercfg.predict-lotto-redis.hvymco.apse2.cache.amazonaws.com:6379,ssl=true`

Note: after recreation the endpoint will change — update the `REDIS_CONNECTION_STRING` environment variable in `.aws/task-definition-backend.json` and redeploy.

## Restoring RDS from Snapshot

The RDS instance was deleted with a final snapshot: `predict-lotto-db-final-snapshot`

To restore when spinning back up:

```powershell
aws rds restore-db-instance-from-db-snapshot `
  --db-instance-identifier predict-lotto-db `
  --db-snapshot-identifier predict-lotto-db-final-snapshot `
  --db-instance-class db.t3.micro `
  --no-multi-az `
  --publicly-accessible `
  --vpc-security-group-ids sg-39560b5f `
  --region ap-southeast-2
```

Wait for status `available` before starting ECS services. The endpoint will be the same hostname if you use the same instance identifier.
