#!/bin/bash

# Script to test circuit breaker and retry behavior

echo "Testing Circuit Breaker and Retry Behavior"
echo "==========================================="
echo ""

SERVICE_URL="http://192.168.49.2:32736"

# Check if running in Kubernetes
if ! kubectl get service booking-service-task5 &>/dev/null; then
    echo "⚠️  Service not found. Make sure deployment is running."
    exit 1
fi

echo "Testing normal requests (should succeed):"
echo "-----------------------------------------"
for i in {1..10}; do
    response=$(curl -s -w "\nHTTP_CODE:%{http_code}" "$SERVICE_URL/ping" 2>/dev/null)
    http_code=$(echo "$response" | grep HTTP_CODE | cut -d':' -f2)
    body=$(echo "$response" | grep -v HTTP_CODE)
    
    if [ "$http_code" == "200" ]; then
        echo "✅ Request $i: Success - $body"
    else
        echo "❌ Request $i: Failed with HTTP $http_code"
    fi
done

echo ""
echo "Circuit Breaker Configuration:"
echo "------------------------------"
kubectl get destinationrule booking-service-task5 -o yaml | grep -A 10 outlierDetection

echo ""
echo "Retry Configuration:"
echo "--------------------"
kubectl get virtualservice booking-service-task5-gateway -o yaml | grep -A 5 retries

echo ""
echo "To simulate failures and test circuit breaker:"
echo "1. Make one pod unhealthy:"
echo "   kubectl exec -it <pod-name> -- kill 1"
echo ""
echo "2. Send multiple requests:"
echo "   for i in {1..20}; do curl http://GATEWAY_URL/ping; done"
echo ""
echo "3. Check if unhealthy pod was ejected:"
echo "   kubectl logs -n istio-system -l app=istio-ingressgateway | grep outlier"

