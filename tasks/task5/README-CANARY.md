VT

Some debug commands:
kubectl get pods -n istio-system -o wide

# Watch the pods in real-time
kubectl get pods -n istio-system --watch

# Check the status of all resources in istio-system
kubectl get all -n istio-system

# Describe a specific Istio pod
kubectl describe pod <pod-name> -n istio-system

kubectl get pods → queries default namespace only
kubectl get pods -n istio-system → queries istio-system namespace only
kubectl get pods -A (or --all-namespaces) → queries all namespaces

kubectl describe pod booking-service-task5-67d5f7cb47-znnb5


cd booking-service
docker build -t booking-service:latest .

# Deploy both versions (differ only by env var)
kubectl apply -f booking-service-deployment.yml      # ENABLE_FEATURE_X=false
kubectl apply -f booking-service-deployment-v2.yml   # ENABLE_FEATURE_X=true

# Step 2: Create the Service (networking)
kubectl apply -f booking-service-service.yml

# Step 3: Configure Istio traffic rules
kubectl apply -f booking-service-traffic.yml

minikube image load booking-service:latest

kubectl rollout restart deployment booking-service-task5
kubectl rollout restart deployment booking-service-task5-v2

kubectl delete pod -l app=booking-service-task5

minikube service istio-ingressgateway -n istio-system

kubectl scale deployment booking-service-task5 --replicas=0

$ curl -H "x-feature-flag:v2" http://127.0.0.1:49504/ping

# Canary Release and Feature Flags Setup

This directory contains a complete setup for **canary releases** and **feature flags** using Istio service mesh.

## 📋 Overview

### What's Included

- **booking-service-deployment.yml** - v1 deployment (stable version)
- **booking-service-deployment-v2.yml** - v2 deployment (canary version with Feature X)
- **booking-service-service.yml** - Kubernetes Service (version-agnostic)
- **booking-service-traffic.yml** - Istio traffic management (canary + feature flags)

### Features

✅ **Canary Release**: 90% traffic to v1, 10% to v2  
✅ **Feature Flags**: Header-based routing to specific versions  
✅ **Load Balancing**: Round-robin across pod replicas  
✅ **Health Checks**: Liveness and readiness probes configured

## 🚀 Deployment

### Prerequisites

- Kubernetes cluster (minikube or similar)
- Istio installed and configured
- kubectl configured to access the cluster

### Quick Start

```bash
# Deploy everything
./deploy-canary.sh

# Test canary distribution
./test-canary.sh

# Test feature flags
./test-feature-flags.sh
```

### Manual Deployment

```bash
# 1. Build Docker image
cd booking-service
docker build -t booking-service:latest .
cd ..

# 2. Deploy v1
kubectl apply -f booking-service-deployment.yml

# 3. Deploy Service
kubectl apply -f booking-service-service.yml

# 4. Deploy v2
kubectl apply -f booking-service-deployment-v2.yml

# 5. Apply Istio traffic rules
kubectl apply -f booking-service-traffic.yml
```

## 🔀 Traffic Management

### Canary Release (Weight-based)

The configuration routes traffic based on **weights only**:
- **90%** of traffic to v1 (stable, ENABLE_FEATURE_X=false)
- **10%** of traffic to v2 (canary, ENABLE_FEATURE_X=true)

This happens automatically with no user intervention.

### Feature Flags (Internal Behavior)

Feature flags control **microservice behavior**, NOT routing:

- **v1 instances** have `ENABLE_FEATURE_X=false`:
  - Basic functionality
  - No `/feature` endpoint
  - Standard bookings response

- **v2 instances** have `ENABLE_FEATURE_X=true`:
  - Enhanced functionality  
  - `/feature` endpoint available
  - Enhanced bookings with additional fields

**Important**: Routing is purely weight-based. Feature flags only affect what happens inside each version.

## 📊 Traffic Configuration Explained

### VirtualService

```yaml
http:
  # Canary release: Weight-based traffic split
  - route:
    - destination:
        host: booking-service-task5
        subset: v1
      weight: 90  # 90% to v1 (ENABLE_FEATURE_X=false)
    - destination:
        host: booking-service-task5
        subset: v2
      weight: 10  # 10% to v2 (ENABLE_FEATURE_X=true)
```

### DestinationRule

```yaml
subsets:
  - name: v1
    labels:
      version: v1
  - name: v2
    labels:
      version: v2
```

## 🔍 Testing

### Test Canary Distribution

```bash
./test-canary.sh
```

Expected output:
```
Results:
--------
Total requests: 100
v1 responses:   91 (91.0%)
v2 responses:   9 (9.0%)
✅ Canary release working correctly!
```

### Test Feature Flags (Internal Behavior)

```bash
./test-feature-flags.sh
```

This tests:
- Feature X availability based on ENABLE_FEATURE_X flag
- `/feature` endpoint only accessible on v2 instances
- Enhanced bookings only on v2 instances
- Proper distribution (~90% v1, ~10% v2)

### Manual Testing

#### Check info endpoint

```bash
# Random version based on canary split (90% v1, 10% v2)
curl http://booking-service-task5/info

# Response examples:
# v1: {"featureX":false,"service":"booking-service-task5"}
# v2: {"featureX":true,"service":"booking-service-task5"}
```

#### Access Feature X endpoint

```bash
# Only available on v2 instances (~10% of requests)
curl http://booking-service-task5/feature

# v1 response: 404 Not Found
# v2 response: {"message":"Feature X is enabled!","feature":"beta-feature"}
```

#### Check enhanced bookings

```bash
# Call multiple times to see both versions
curl http://booking-service-task5/bookings

# v1 response (90%): Standard bookings without "enhanced" field
# v2 response (10%): Bookings with "enhanced":true and additional message
```

## 📈 Gradual Rollout Strategy

### Phase 1: Initial Canary (Current)
- 90% v1, 10% v2
- Monitor metrics, errors, latency

### Phase 2: Increase Canary
Edit `booking-service-traffic.yml`:
```yaml
weight: 70  # v1
weight: 30  # v2
```

### Phase 3: Majority Canary
```yaml
weight: 25  # v1
weight: 75  # v2
```

### Phase 4: Full Rollout
```yaml
weight: 0   # v1
weight: 100 # v2
```

### Phase 5: Cleanup
```bash
# Remove v1 deployment
kubectl delete deployment booking-service-task5

# Rename v2 to v1
kubectl patch deployment booking-service-task5-v2 \
  -p '{"metadata":{"name":"booking-service-task5"}}'
```

## 🛠️ Configuration Details

### Version Differences

| Feature | v1 | v2 |
|---------|----|----|
| ENABLE_FEATURE_X | false | true |
| /feature endpoint | ❌ | ✅ |
| Enhanced bookings | ❌ | ✅ |
| Docker image | booking-service:latest | booking-service:latest |
| Replicas | 1 | 1 |

### Environment Variables

**v1:**
```yaml
env:
  - name: PORT
    value: "8080"
  - name: ENABLE_FEATURE_X
    value: "false"
```

**v2:**
```yaml
env:
  - name: PORT
    value: "8080"
  - name: ENABLE_FEATURE_X
    value: "true"
```

## 🔧 Troubleshooting

### Check pod status

```bash
kubectl get pods -l app=booking-service-task5
```

### Check Istio configuration

```bash
kubectl get virtualservice booking-service-task5 -o yaml
kubectl get destinationrule booking-service-task5 -o yaml
```

### View logs

```bash
# v1 logs
kubectl logs -l version=v1 -f

# v2 logs
kubectl logs -l version=v2 -f
```

### Check traffic distribution

```bash
# Install istioctl
istioctl dashboard kiali
```

### Common Issues

**Issue**: All traffic goes to v1
- **Solution**: Ensure v2 pods are running and ready
- Check: `kubectl get pods -l version=v2`

**Issue**: Feature flags don't work
- **Solution**: Verify Istio sidecar injection
- Check: `kubectl get pod <pod-name> -o jsonpath='{.spec.containers[*].name}'`

**Issue**: Service not accessible
- **Solution**: Check service endpoints
- Check: `kubectl get endpoints booking-service-task5`

## 📚 Use Cases

### Canary Releases
- Test new code with 10% of production traffic
- Monitor metrics before full rollout
- Quick rollback if issues detected
- Gradual traffic shifting (10% → 30% → 50% → 100%)

### Feature Flags (ENABLE_FEATURE_X)
- Control internal microservice behavior
- Enable experimental features in v2
- Test new functionality with subset of traffic
- Independent of routing logic

### Example: Production Canary Flow

```bash
# Users make normal requests (no special headers needed)
curl http://booking-service-task5/bookings

# 90% get v1 response (stable, no enhanced features)
# 10% get v2 response (canary, with enhanced features)

# Monitor v2 metrics:
# - Error rates
# - Response times
# - Resource usage

# If v2 is stable, gradually increase weight in traffic.yml
```

## 🎯 Best Practices

1. **Start Small**: Begin with 5-10% canary traffic
2. **Monitor Closely**: Watch error rates, latency, resource usage
3. **Automate**: Use metrics to automatically adjust traffic
4. **Have Rollback Plan**: Keep v1 running until v2 is proven
5. **Use Feature Flags**: Test features before enabling for all users
6. **Document Changes**: Track what's different in v2

## 📖 References

- [Istio Traffic Management](https://istio.io/latest/docs/concepts/traffic-management/)
- [Canary Deployments](https://istio.io/latest/docs/tasks/traffic-management/traffic-shifting/)
- [Header-based Routing](https://istio.io/latest/docs/tasks/traffic-management/request-routing/)

