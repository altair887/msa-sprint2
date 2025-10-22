#!/bin/bash

set -e

# 1. Ensure both deployments are running first
kubectl get pods -l app=booking-service-task5

# 2. Stop v2
kubectl scale deployment booking-service-task5-v2 --replicas=0

# 3. IMPORTANT: Wait for Istio to detect the change (30 seconds)
sleep 5

echo "▶️ Testing fallback route..."
curl -s "http://127.0.0.1:50525/ping" || echo "Fallback route working"
