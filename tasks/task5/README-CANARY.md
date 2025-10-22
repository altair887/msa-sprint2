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
# 1. Build Docker images
cd booking-service
docker build -t booking-service:v1 -t booking-service:latest .
docker build -t booking-service:v2 .
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

The default configuration routes:
- **90%** of traffic to v1 (stable)
- **10%** of traffic to v2 (canary)

This happens automatically with no user intervention.

### Feature Flags (Header-based)

You can force routing to v2 using HTTP headers:

#### Option 1: Using `x-version` header

```bash
curl -H "x-version: v2" http://booking-service-task5/version
```

#### Option 2: Using `x-feature` header

```bash
curl -H "x-feature: beta" http://booking-service-task5/bookings
```

### Priority Order

Istio processes routing rules in order:

1. **x-version: v2** header → routes to v2
2. **x-feature: beta** header → routes to v2  
3. **Default** → 90% v1, 10% v2 (canary)

## 📊 Traffic Configuration Explained

### VirtualService

```yaml
http:
  # Priority 1: Explicit version header
  - match:
    - headers:
        x-version:
          exact: v2
    route:
    - destination:
        subset: v2
  
  # Priority 2: Beta feature flag
  - match:
    - headers:
        x-feature:
          exact: beta
    route:
    - destination:
        subset: v2
  
  # Priority 3: Default canary split
  - route:
    - destination:
        subset: v1
      weight: 90
    - destination:
        subset: v2
      weight: 10
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

### Test Feature Flags

```bash
./test-feature-flags.sh
```

This tests:
- Default routing (canary)
- `x-version: v2` header routing
- `x-feature: beta` header routing
- Feature X availability on v2

### Manual Testing

#### Check info endpoint

```bash
# Random version (canary split)
curl http://booking-service-task5/info

# Force v2 (with Feature X)
curl -H "x-version: v2" http://booking-service-task5/info
```

#### Access Feature X (only on v2)

```bash
curl -H "x-version: v2" http://booking-service-task5/feature
```

#### Check enhanced bookings

```bash
# v1 - basic bookings
curl http://booking-service-task5/bookings

# v2 - enhanced bookings
curl -H "x-feature: beta" http://booking-service-task5/bookings
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
| Image tag | latest/v1 | v2 |
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

### Feature Flags
- Enable features for specific users (beta testers)
- A/B testing with different user groups
- Dark launches (deploy but don't expose)

### Example: Beta User Access

```bash
# Beta users get header from auth service
curl -H "x-feature: beta" \
     -H "Authorization: Bearer <token>" \
     http://booking-service-task5/bookings
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

