#!/bin/bash

# Script to test feature flags using HTTP headers
# Tests routing based on x-version and x-feature headers

echo "Testing Feature Flags (Header-based Routing)"
echo "============================================="
echo ""

SERVICE_URL="http://booking-service-task5"

# Check if running in Kubernetes
if ! kubectl get service booking-service-task5 &>/dev/null; then
    echo "⚠️  Service not found. Using localhost for testing..."
    SERVICE_URL="http://localhost:8080"
fi

echo "1. Testing default routing (no headers - canary split):"
echo "--------------------------------------------------------"
response=$(curl -s "$SERVICE_URL/info")
echo "$response"
featureX=$(echo "$response" | grep -o '"featureX":[^,}]*' | cut -d':' -f2)
if [ "$featureX" == "true" ]; then
    echo "→ Routed to: v2 (featureX: true)"
else
    echo "→ Routed to: v1 (featureX: false)"
fi
echo ""

echo "2. Testing x-version: v2 header (should route to v2 with featureX):"
echo "--------------------------------------------------------------------"
response=$(curl -s -H "x-version: v2" "$SERVICE_URL/info")
echo "$response"
featureX=$(echo "$response" | grep -o '"featureX":[^,}]*' | cut -d':' -f2)
if [ "$featureX" == "true" ]; then
    echo "✅ Correctly routed to v2 (featureX enabled)"
else
    echo "❌ Expected v2 with featureX, got featureX: $featureX"
fi
echo ""

echo "3. Testing x-feature: beta header (should route to v2 with featureX):"
echo "----------------------------------------------------------------------"
response=$(curl -s -H "x-feature: beta" "$SERVICE_URL/info")
echo "$response"
featureX=$(echo "$response" | grep -o '"featureX":[^,}]*' | cut -d':' -f2)
if [ "$featureX" == "true" ]; then
    echo "✅ Correctly routed to v2 (featureX enabled)"
else
    echo "❌ Expected v2 with featureX, got featureX: $featureX"
fi
echo ""

echo "4. Testing /feature endpoint with x-version: v2:"
echo "-------------------------------------------------"
response=$(curl -s -H "x-version: v2" "$SERVICE_URL/feature")
if echo "$response" | grep -q "Feature X is enabled"; then
    echo "✅ Feature X is accessible on v2"
    echo "$response"
else
    echo "❌ Feature X not found (response: $response)"
fi
echo ""

echo "5. Testing /bookings with feature flag (x-feature: beta):"
echo "----------------------------------------------------------"
response=$(curl -s -H "x-feature: beta" "$SERVICE_URL/bookings")
echo "$response"
if echo "$response" | grep -q "enhanced"; then
    echo "✅ Enhanced features enabled"
else
    echo "⚠️  Enhanced features not detected"
fi
echo ""

echo "6. Multiple requests with x-version: v2 (should always have featureX):"
echo "-----------------------------------------------------------------------"
v2_count=0
total=10
for i in $(seq 1 $total); do
    response=$(curl -s -H "x-version: v2" "$SERVICE_URL/info")
    featureX=$(echo "$response" | grep -o '"featureX":[^,}]*' | cut -d':' -f2)
    if [ "$featureX" == "true" ]; then
        ((v2_count++))
        echo -n "2"
    else
        echo -n "1"
    fi
done
echo ""
echo "$v2_count/$total requests routed to v2 (with featureX)"
if [ $v2_count -eq $total ]; then
    echo "✅ All requests correctly routed to v2 with header"
else
    echo "❌ Some requests incorrectly routed"
fi
echo ""

echo "Summary:"
echo "--------"
echo "Feature flags allow you to:"
echo "  • Route specific users to v2 using x-version: v2 header"
echo "  • Enable beta features using x-feature: beta header"
echo "  • Test new features without affecting all users"
echo "  • Gradually roll out features to select users"

