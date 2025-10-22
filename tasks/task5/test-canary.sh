#!/bin/bash

# Script to test canary release (90% v1, 10% v2)
# Expected: ~90% requests go to v1, ~10% go to v2

echo "Testing Canary Release (90/10 split)"
echo "======================================"
echo ""

SERVICE_URL="http://booking-service-task5"
REQUESTS=100

# Check if running in Kubernetes
if ! kubectl get service booking-service-task5 &>/dev/null; then
    echo "⚠️  Service not found. Using localhost for testing..."
    SERVICE_URL="http://localhost:8080"
fi

# Counter for versions
v1_count=0
v2_count=0
error_count=0

echo "Sending $REQUESTS requests to test traffic distribution..."
echo ""

for i in $(seq 1 $REQUESTS); do
    # Make request and check if featureX is enabled
    response=$(curl -s "$SERVICE_URL/info" 2>/dev/null)
    
    if [ $? -ne 0 ]; then
        ((error_count++))
        continue
    fi
    
    featureX=$(echo "$response" | grep -o '"featureX":[^,}]*' | cut -d':' -f2)
    
    if [ "$featureX" == "false" ]; then
        ((v1_count++))
        echo -n "1"
    elif [ "$featureX" == "true" ]; then
        ((v2_count++))
        echo -n "2"
    else
        ((error_count++))
        echo -n "?"
    fi
    
    # New line every 50 requests
    if [ $((i % 50)) -eq 0 ]; then
        echo ""
    fi
done

echo ""
echo ""
echo "Results:"
echo "--------"
echo "Total requests: $REQUESTS"
echo "v1 responses:   $v1_count ($(awk "BEGIN {printf \"%.1f\", ($v1_count/$REQUESTS)*100}")%)"
echo "v2 responses:   $v2_count ($(awk "BEGIN {printf \"%.1f\", ($v2_count/$REQUESTS)*100}")%)"
echo "Errors:         $error_count"
echo ""

# Check if distribution is close to expected
v1_percent=$(awk "BEGIN {printf \"%.0f\", ($v1_count/$REQUESTS)*100}")
v2_percent=$(awk "BEGIN {printf \"%.0f\", ($v2_count/$REQUESTS)*100}")

if [ $v1_percent -ge 80 ] && [ $v1_percent -le 100 ] && [ $v2_percent -ge 0 ] && [ $v2_percent -le 20 ]; then
    echo "✅ Canary release working correctly!"
    echo "   Expected: ~90% v1, ~10% v2"
    echo "   Actual:   $v1_percent% v1, $v2_percent% v2"
else
    echo "⚠️  Traffic distribution unexpected"
    echo "   Expected: ~90% v1, ~10% v2"
    echo "   Actual:   $v1_percent% v1, $v2_percent% v2"
fi

echo ""
echo "Sample response from v1:"
curl -s "$SERVICE_URL/info" | grep -q '"featureX":false' && curl -s "$SERVICE_URL/info"

echo ""
echo "Sample response from v2 (with Feature X):"
for i in {1..10}; do
    response=$(curl -s "$SERVICE_URL/info")
    if echo "$response" | grep -q '"featureX":true'; then
        echo "$response"
        break
    fi
done

