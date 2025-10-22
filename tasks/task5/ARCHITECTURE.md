# Task 5 Architecture: Canary Release with Feature Flags

## 📐 Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                         User Request                         │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│              Istio VirtualService (Traffic Split)            │
│                  Weight-based Routing ONLY                   │
└────────────┬─────────────────────────────────┬──────────────┘
             │                                 │
        90% traffic                       10% traffic
             │                                 │
             ▼                                 ▼
┌────────────────────────┐      ┌────────────────────────┐
│    v1 Deployment       │      │    v2 Deployment       │
│ (booking-service)      │      │ (booking-service)      │
├────────────────────────┤      ├────────────────────────┤
│ Image: latest          │      │ Image: latest          │
│ ENABLE_FEATURE_X=false │      │ ENABLE_FEATURE_X=true  │
│                        │      │                        │
│ ❌ No /feature endpoint │      │ ✅ /feature endpoint    │
│ ❌ Basic bookings       │      │ ✅ Enhanced bookings    │
│ ⚙️  Stable version      │      │ 🚀 Canary version       │
└────────────────────────┘      └────────────────────────┘

Note: Both deployments use the SAME Docker image (booking-service:latest)
      The difference is ONLY the ENABLE_FEATURE_X environment variable
```

## 🎯 Key Principles

### 1. **Routing: Weight-Based Only**
- Istio VirtualService routes traffic based on weights
- 90% → v1 (stable)
- 10% → v2 (canary)
- **No header-based routing**
- **No user selection of version**

### 2. **Feature Flags: Internal Behavior**
- Environment variable: `ENABLE_FEATURE_X`
- Controls microservice functionality
- **Does NOT affect routing**
- Determined at deployment time

## 📋 Component Responsibilities

| Component | Responsibility | Controls |
|-----------|---------------|----------|
| **Istio VirtualService** | Route traffic | Weight distribution (90/10) |
| **Istio DestinationRule** | Define subsets | v1 and v2 labels |
| **Kubernetes Service** | Service discovery | Load balancing (version-agnostic) |
| **Deployment v1** | Run stable version | ENABLE_FEATURE_X=false, image: latest |
| **Deployment v2** | Run canary version | ENABLE_FEATURE_X=true, image: latest |
| **Feature Flag** | Enable/disable features | Internal behavior only |

## 🔀 Traffic Flow

### Standard Request Flow
```
1. User → curl http://booking-service-task5/bookings
2. Istio intercepts request
3. Istio routes based on weight:
   - 90% chance → v1 pod (ENABLE_FEATURE_X=false)
   - 10% chance → v2 pod (ENABLE_FEATURE_X=true)
4. Selected pod processes request
5. Response varies based on pod's feature flag
```

### Example: Multiple Requests
```bash
# Request 1-9: Likely go to v1
curl http://booking-service-task5/bookings
# Response: Standard bookings (no "enhanced" field)

# Request 10: Likely goes to v2
curl http://booking-service-task5/bookings
# Response: Enhanced bookings (with "enhanced": true)
```

## 🚀 Feature Flag Behavior

### v1 Pods (ENABLE_FEATURE_X=false)

| Endpoint | Behavior |
|----------|----------|
| `/info` | `{"featureX": false, ...}` |
| `/health` | `{"status": "UP"}` |
| `/ready` | `{"status": "READY"}` |
| `/ping` | `"pong from v1"` |
| `/bookings` | Standard bookings |
| `/feature` | ❌ 404 Not Found |

### v2 Pods (ENABLE_FEATURE_X=true)

| Endpoint | Behavior |
|----------|----------|
| `/info` | `{"featureX": true, ...}` |
| `/health` | `{"status": "UP"}` |
| `/ready` | `{"status": "READY"}` |
| `/ping` | `"pong from v2 (with Feature X)"` |
| `/bookings` | Enhanced bookings with extra fields |
| `/feature` | ✅ `{"message": "Feature X is enabled!", ...}` |

## 🔧 Configuration Files

### `booking-service-traffic.yml`
```yaml
VirtualService:
  - Weight-based routing ONLY
  - 90% to subset v1
  - 10% to subset v2
  
DestinationRule:
  - Defines subset v1 (version: v1 label)
  - Defines subset v2 (version: v2 label)
```

### `booking-service-deployment.yml` (v1)
```yaml
Image: booking-service:latest
Labels: version=v1
Environment:
  ENABLE_FEATURE_X: "false"
```

### `booking-service-deployment-v2.yml` (v2)
```yaml
Image: booking-service:latest  # Same image as v1
Labels: version=v2
Environment:
  ENABLE_FEATURE_X: "true"     # Only difference
```

**Key Point:** Both deployments use the **same Docker image**. The behavior difference comes purely from the environment variable.

## 📊 Comparison: Feature Flags vs Routing

| Aspect | This Implementation | ❌ Incorrect Approach |
|--------|---------------------|----------------------|
| **Routing** | Weight-based (Istio) | Header-based |
| **Feature Control** | Environment variable | HTTP headers |
| **User Selection** | No (automatic) | Yes (via headers) |
| **Canary Distribution** | Automatic 90/10 | Manual header passing |
| **Production Ready** | ✅ Yes | ❌ No |

## 🎯 Why This Architecture?

### ✅ Advantages

1. **Clean Separation of Concerns**
   - Routing = Istio's job
   - Features = Application's job

2. **Real Canary Testing**
   - Random 10% of users test v2
   - Realistic production traffic
   - Unbiased metrics

3. **Simple User Experience**
   - No special headers needed
   - Transparent to end users
   - No configuration required

4. **Easy Rollout Management**
   - Adjust weights in one place
   - Gradual rollout: 10% → 30% → 50% → 100%
   - Quick rollback if needed

### ❌ Why NOT Header-Based Routing?

1. **Not True Canary**
   - Only tech-savvy users test v2
   - Biased metrics
   - Doesn't represent real users

2. **Complex User Experience**
   - Users need to know headers
   - Applications need to pass headers
   - Error-prone

3. **Feature Flags ≠ Routing**
   - Feature flags should control behavior
   - Routing should be transparent
   - Mixing them causes confusion

## 🔄 Rollout Strategy

### Phase 1: Initial Canary (10%)
```yaml
weight: 90  # v1
weight: 10  # v2
```
- Monitor v2 metrics
- Check error rates, latency
- Verify functionality

### Phase 2: Increase Canary (30%)
```yaml
weight: 70  # v1
weight: 30  # v2
```
- More users get v2
- More data for analysis

### Phase 3: Majority Canary (70%)
```yaml
weight: 30  # v1
weight: 70  # v2
```
- v2 handles most traffic
- v1 ready for rollback

### Phase 4: Full Rollout (100%)
```yaml
weight: 0   # v1
weight: 100 # v2
```
- All traffic to v2
- Can remove v1 deployment

### Phase 5: Promote v2 to v1
- Remove v1 deployment
- Rename v2 → v1
- Disable ENABLE_FEATURE_X (now standard)
- Ready for next canary (v3)

## 📈 Monitoring

### Key Metrics to Track

**Per Version:**
- Request count
- Error rate (4xx, 5xx)
- Response time (p50, p95, p99)
- Resource usage (CPU, memory)

**Compare v1 vs v2:**
```bash
# Using Istio/Kiali dashboard
- Error rate: v1 vs v2
- Latency: v1 vs v2
- Success rate: v1 vs v2
```

**Feature Flag Specific:**
- `/feature` endpoint usage (v2 only)
- Enhanced bookings usage (v2 only)

## 🧪 Testing

### Automated Tests
```bash
# Test canary distribution
./test-canary.sh
# Expects: ~90% v1, ~10% v2

# Test feature flag behavior
./test-feature-flags.sh
# Expects: /feature only on v2, enhanced bookings only on v2
```

### Manual Tests
```bash
# Make 100 requests, observe distribution
for i in {1..100}; do
  curl -s http://booking-service-task5/info | grep featureX
done | sort | uniq -c

# Expected output:
#  ~90 "featureX":false
#  ~10 "featureX":true
```

## 🎓 Learning Points

1. **Feature flags control behavior, not routing**
2. **Canary releases should be automatic and transparent**
3. **Istio handles routing, application handles features**
4. **Weight-based distribution gives realistic testing**
5. **Separation of concerns makes systems maintainable**

## 📚 References

- [Istio Traffic Management](https://istio.io/latest/docs/concepts/traffic-management/)
- [Canary Deployments Best Practices](https://istio.io/latest/docs/tasks/traffic-management/traffic-shifting/)
- [Feature Flags vs Feature Toggles](https://martinfowler.com/articles/feature-toggles.html)

