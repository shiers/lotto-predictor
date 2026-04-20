# ALB Recreation Guide

Use this guide to recreate the Application Load Balancer after deleting it to save costs.

## Current ALB Configuration (for reference)

- Name: `predict-lotto-alb`
- DNS: `predict-lotto-alb-1372341717.ap-southeast-2.elb.amazonaws.com`
- Scheme: internet-facing
- Security group: `sg-039487fb281213b16` (predict-lotto-alb-sg)
- Subnets: `subnet-01379248`, `subnet-85a6a0dc`, `subnet-a29f35c5`
- SSL Certificate ARN: `arn:aws:acm:ap-southeast-2:897458219603:certificate/a18623ec-7afe-46df-a3dd-1561709c7a08`

## Step 1 — Create Target Groups

Go to [EC2 → Target Groups → Create target group](https://ap-southeast-2.console.aws.amazon.com/ec2/home?region=ap-southeast-2#CreateTargetGroup:)

### Frontend target group

- Target type: IP
- Name: `predict-lotto-frontend-tg`
- Protocol: HTTP, Port: 80
- VPC: `vpc-6037d707`
- Health check path: `/`
- Click "Next" then "Create target group" (no targets to register — ECS registers them automatically)

### Backend target group

- Target type: IP
- Name: `predict-lotto-backend-tg`
- Protocol: HTTP, Port: 80
- VPC: `vpc-6037d707`
- Health check path: `/api/health`
- Click "Next" then "Create target group"

## Step 2 — Create the ALB

Go to [EC2 → Load Balancers → Create load balancer](https://ap-southeast-2.console.aws.amazon.com/ec2/home?region=ap-southeast-2#CreateALB:) → choose "Application Load Balancer"

- Name: `predict-lotto-alb`
- Scheme: Internet-facing
- IP address type: IPv4
- VPC: `vpc-6037d707`
- Subnets: select all 3 AZs (`subnet-01379248`, `subnet-85a6a0dc`, `subnet-a29f35c5`)
- Security group: remove default, add `sg-039487fb281213b16` (predict-lotto-alb-sg)
- Listener: HTTP:80 → forward to `predict-lotto-frontend-tg` (we'll update this after)
- Click "Create load balancer"

## Step 3 — Add Listeners

After the ALB is created, go to its Listeners tab and add:

### Listener 1 — HTTP:80 redirect

- Edit the default HTTP:80 listener
- Action: Redirect to HTTPS:443

### Listener 2 — HTTPS:443 → frontend

- Protocol: HTTPS, Port: 443
- Certificate: select `a18623ec-7afe-46df-a3dd-1561709c7a08` (lottonz.dryad.ca)
- SSL policy: `ELBSecurityPolicy-TLS13-1-2-Res-PQ-2025-09`
- Default action: Forward to `predict-lotto-frontend-tg`
- Add rule for `/api/*` → Forward to `predict-lotto-backend-tg`

### Listener 3 — HTTPS:8443 → backend (for api.lottonz.dryad.ca)

- Protocol: HTTPS, Port: 8443
- Certificate: same as above
- Default action: Forward to `predict-lotto-backend-tg`

### Listener 4 — HTTP:8080 → backend (optional, for direct access)

- Protocol: HTTP, Port: 8080
- Default action: Forward to `predict-lotto-backend-tg`

## Step 4 — Update Route 53

Go to [Route 53 → Hosted zones → lottonz.dryad.ca](https://console.aws.amazon.com/route53/v2/hostedzones)

Update the A record aliases to point to the new ALB DNS name:

- `lottonz.dryad.ca` → Alias to new ALB
- `api.lottonz.dryad.ca` → Alias to new ALB

The new ALB will have a different DNS name — get it with:

```powershell
aws elbv2 describe-load-balancers --names predict-lotto-alb --region ap-southeast-2 --query "LoadBalancers[*].DNSName" --output text
```

## Step 5 — Update ECS Services

After the ALB and target groups are recreated, update each ECS service to use the new target groups:

Go to ECS → predict-lotto-cluster → each service → Update service → Load balancing → update the target group ARNs.

Or trigger a fresh GitHub Actions deployment by pushing to master — it will redeploy the services.

## Step 6 — Verify

Once tasks are running and registered as healthy in the target groups, test:

- https://lottonz.dryad.ca — frontend
- https://api.lottonz.dryad.ca — backend API
