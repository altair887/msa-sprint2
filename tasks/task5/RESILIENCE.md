# Resilience Patterns: Retry and Circuit Breaker

## 📋 Overview

This document explains the **Retry** and **Circuit Breaker** patterns implemented in the booking-service using Istio.

## 🔄 Retry Pattern

### Configuration (VirtualService)

```yaml
retries:
  attempts: 3
  perTryTimeout: 2s
  retryOn: 5xx,reset,connect-failure,refused-stream
```

### Parameters Explained

| Parameter | Value | Description |
|-----------|-------|-------------|
| **attempts** | 3 | Maximum retry attempts (original + 3 retries = 4 total attempts) |
| **perTryTimeout** | 2s | Timeout for each individual attempt |
| **retryOn** | 5xx,reset,connect-failure,refused-stream | Conditions that trigger a retry |

### Retry Conditions

- **5xx**: HTTP 500, 502, 503, 504 errors
- **reset**: TCP connection reset by peer
- **connect-failure**: Failed to establish connection
- **refused-stream**: HTTP/2 stream refused

### Example Flow

```
User Request
    ↓
Attempt 1 → Pod fails with 503 → Retry
    ↓
Attempt 2 → Pod fails with 503 → Retry
    ↓
Attempt 3 → Pod succeeds with 200 → Return to user
    ↓
Total time: ~4 seconds (2 failed attempts × 2s timeout)
```

### Benefits

✅ Handles transient failures (temporary network issues)
✅ Improves user experience (automatic recovery)
✅ Works with canary releases (retries can hit different versions)

### Risks

⚠️ Increased latency on failures (up to 6s with 3 retries)
⚠️ Amplified load on failing service
⚠️ Non-idempotent operations may execute multiple times

## ⚡ Circuit Breaker Pattern

### Configuration (DestinationRule)

```yaml
trafficPolicy:
  connectionPool:
    tcp:
      maxConnections: 100
    http:
      http1MaxPendingRequests: 50
      http2MaxRequests: 100
      maxRequestsPerConnection: 2
  outlierDetection:
    consecutive5xxErrors: 5
    interval: 30s
    baseEjectionTime: 30s
    maxEjectionPercent: 50
    minHealthPercent: 50
```

### Connection Pool

Prevents overwhelming the service with too many concurrent connections.

| Setting | Value | Limit |
|---------|-------|-------|
| **maxConnections** | 100 | Max TCP connections to all instances |
| **http1MaxPendingRequests** | 50 | Max queued HTTP/1.1 requests |
| **http2MaxRequests** | 100 | Max concurrent HTTP/2 streams |
| **maxRequestsPerConnection** | 2 | Requests per connection before closing |

**What happens when limits are exceeded?**
→ New requests are queued (up to http1MaxPendingRequests)
→ If queue is full → Request fails immediately with 503

### Outlier Detection (Circuit Breaker)

Automatically removes unhealthy instances from the load balancing pool.

| Setting | Value | Description |
|---------|-------|-------------|
| **consecutive5xxErrors** | 5 | Eject after 5 consecutive 5xx errors |
| **interval** | 30s | Time between ejection analysis |
| **baseEjectionTime** | 30s | Minimum time to keep instance ejected |
| **maxEjectionPercent** | 50 | Max percentage of instances that can be ejected |
| **minHealthPercent** | 50 | Minimum percentage that must remain healthy |

### Circuit Breaker States

```
┌─────────────┐
│   CLOSED    │  ← Normal operation, all pods in pool
│ All healthy │
└──────┬──────┘
       │ 5 consecutive errors detected
       ↓
┌─────────────┐
│    OPEN     │  ← Failing pod ejected from pool
│  Pod ejected│  ← Traffic only goes to healthy pods
└──────┬──────┘
       │ After 30 seconds (baseEjectionTime)
       ↓
┌─────────────┐
│ HALF-OPEN   │  ← Pod added back to pool (testing)
│ Testing pod │
└──────┬──────┘
       │
       ├─→ If healthy → CLOSED (keep in pool)
       └─→ If fails → OPEN (eject again)
```

### Example: Pod Failure Scenario

```
Timeline:

00:00 - All pods healthy
00:05 - v2 pod starts returning 503 errors
00:10 - 5 consecutive errors detected
        → Circuit OPENS: v2 pod ejected
        
00:10-00:40 - All traffic goes to v1 pods only
              (v2 pod is in timeout)
              
00:40 - Circuit moves to HALF-OPEN
        → v2 pod re-added to pool
        
00:41 - Test request to v2:
        ├─ Success → Circuit CLOSED (v2 healthy again)
        └─ Failure → Circuit OPEN (eject for another 30s)
```

## 🔗 How Retry and Circuit Breaker Work Together

### Scenario: Gradual Service Degradation

```
1. Pod v2-1 starts experiencing issues
   ↓
2. Request hits v2-1 → 503 error
   → Retry #1 → might hit v2-1 again → 503
   → Retry #2 → might hit v1 → SUCCESS ✅
   
3. After 5 consecutive failures to v2-1:
   → Circuit breaker ejects v2-1
   
4. Future requests:
   → Only routed to healthy pods (v1, v2-2)
   → Fewer retries needed
   → Better latency for users
   
5. After 30 seconds:
   → v2-1 gets a second chance
   → If healthy → back in pool
   → If still failing → ejected again
```

### Benefits of Combining Both

✅ **Retry** handles transient failures (temporary issues)
✅ **Circuit Breaker** handles persistent failures (unhealthy pods)
✅ **Together** they provide comprehensive resilience

## 📊 Monitoring

### Check Circuit Breaker Status

```bash
# View DestinationRule configuration
kubectl get destinationrule booking-service-task5 -o yaml

# Check Istio logs for ejection events
kubectl logs -n istio-system -l app=istio-ingressgateway | grep outlier

# Check pod statistics
kubectl exec -n istio-system <istio-proxy-pod> -- \
  curl localhost:15000/clusters | grep booking-service-task5
```

### Metrics to Monitor

- **Request success rate**: Should improve with retries
- **Latency p99**: May increase with retries
- **Ejection count**: How often pods are ejected
- **Active connections**: Should stay below maxConnections
- **Pending requests**: Should stay below http1MaxPendingRequests

## ⚙️ Tuning Guidelines

### Retry Configuration

| Scenario | Recommended Settings |
|----------|---------------------|
| **High latency tolerance** | attempts: 5, perTryTimeout: 5s |
| **Low latency requirement** | attempts: 2, perTryTimeout: 1s |
| **Critical operations** | attempts: 1 (no retry), manual retry |
| **Idempotent GET requests** | attempts: 3-5, perTryTimeout: 2-3s |

### Circuit Breaker Configuration

| Scenario | Recommended Settings |
|----------|---------------------|
| **High traffic** | maxConnections: 500, consecutive5xxErrors: 10 |
| **Low traffic** | maxConnections: 50, consecutive5xxErrors: 3 |
| **Aggressive protection** | baseEjectionTime: 60s, maxEjectionPercent: 30 |
| **Lenient recovery** | baseEjectionTime: 15s, maxEjectionPercent: 70 |

## 🧪 Testing

### Test Retry Behavior

```bash
# Simulate transient failure (pod will recover)
kubectl exec -it <pod-name> -c booking-service -- sh -c "sleep 5"

# Send request during sleep (should retry and succeed)
curl http://<gateway-url>/ping
```

### Test Circuit Breaker

```bash
# Make pod unhealthy (kill main process)
kubectl exec -it <pod-name> -c booking-service -- kill 1

# Send multiple requests (should trigger circuit breaker)
for i in {1..20}; do curl http://<gateway-url>/ping; echo ""; done

# Check if pod was ejected
kubectl logs -n istio-system -l app=istio-ingressgateway | grep -i eject
```

## 🎯 Best Practices

### Retry

1. ✅ **Only retry safe operations** (GET, idempotent POST)
2. ✅ **Set reasonable timeouts** (avoid cascading delays)
3. ✅ **Limit retry attempts** (3-5 max)
4. ✅ **Log retry events** for debugging
5. ❌ **Don't retry** payment processing, user creation

### Circuit Breaker

1. ✅ **Monitor ejection rates** (frequent ejections = problem)
2. ✅ **Set maxEjectionPercent** (prevent total outage)
3. ✅ **Tune for your traffic** (adjust limits based on load)
4. ✅ **Combine with health checks** (pod readiness/liveness)
5. ✅ **Test failure scenarios** (chaos engineering)

## 📚 References

- [Istio Traffic Management](https://istio.io/latest/docs/concepts/traffic-management/)
- [Circuit Breaker Pattern](https://martinfowler.com/bliki/CircuitBreaker.html)
- [Retry Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/retry)

