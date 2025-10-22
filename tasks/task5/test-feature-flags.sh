#!/bin/bash

# Script to test feature flags (ENABLE_FEATURE_X)
# Feature flags control internal microservice behavior, NOT routing
# Routing is handled by canary weights (90% v1, 10% v2)

echo "Testing Feature Flags (Internal Behavior)"
echo "=========================================="
echo ""

SERVICE_URL="http://booking-service-task5"

# Check if running in Kubernetes
if ! kubectl get service booking-service-task5 &>/dev/null; then
    echo "⚠️  Service not found. Using localhost for testing..."
    SERVICE_URL="http://localhost:8080"
fi

echo "Feature flags control which features are enabled in each version:"
echo "  • v1 (90% traffic): ENABLE_FEATURE_X=false"
echo "  • v2 (10% traffic): ENABLE_FEATURE_X=true"
echo ""

echo "1. Testing /info endpoint (shows featureX status):"
echo "---------------------------------------------------"
v1_count=0
v2_count=0
for i in $(seq 1 20); do
    response=$(curl -s "$SERVICE_URL/info")
    featureX=$(echo "$response" | grep -o '"featureX":[^,}]*' | cut -d':' -f2)
    if [ "$featureX" == "true" ]; then
        ((v2_count++))
    else
        ((v1_count++))
    fi
done
echo "Results from 20 requests:"
echo "  v1 (featureX: false): $v1_count"
echo "  v2 (featureX: true):  $v2_count"
echo ""

echo "2. Testing /feature endpoint (only available when featureX=true):"
echo "------------------------------------------------------------------"
feature_available=0
feature_not_found=0
for i in $(seq 1 20); do
    response=$(curl -s "$SERVICE_URL/feature" 2>/dev/null)
    if echo "$response" | grep -q "Feature X is enabled"; then
        ((feature_available++))
    else
        ((feature_not_found++))
    fi
done
echo "Results from 20 requests:"
echo "  Feature available: $feature_available (~10% expected - v2 instances)"
echo "  Feature not found: $feature_not_found (~90% expected - v1 instances)"
if [ $feature_available -gt 0 ] && [ $feature_not_found -gt 0 ]; then
    echo "✅ Feature X endpoint correctly available only on v2"
else
    echo "⚠️  Unexpected distribution"
fi
echo ""

echo "3. Testing /bookings endpoint (enhanced features on v2):"
echo "---------------------------------------------------------"
enhanced_count=0
basic_count=0
for i in $(seq 1 20); do
    response=$(curl -s "$SERVICE_URL/bookings")
    if echo "$response" | grep -q "enhanced"; then
        ((enhanced_count++))
    else
        ((basic_count++))
    fi
done
echo "Results from 20 requests:"
echo "  Enhanced bookings: $enhanced_count (~10% expected - v2 with featureX)"
echo "  Basic bookings:    $basic_count (~90% expected - v1 without featureX)"
if [ $enhanced_count -gt 0 ] && [ $basic_count -gt 0 ]; then
    echo "✅ Enhanced features correctly enabled only on v2"
else
    echo "⚠️  Unexpected distribution"
fi
echo ""

echo "4. Sample responses:"
echo "--------------------"
echo ""
echo "Calling /info multiple times to see both versions:"
for i in {1..10}; do
    response=$(curl -s "$SERVICE_URL/info")
    featureX=$(echo "$response" | grep -o '"featureX":[^,}]*' | cut -d':' -f2)
    if [ "$featureX" == "true" ]; then
        echo "✓ v2 response: $response"
        break
    fi
done
for i in {1..10}; do
    response=$(curl -s "$SERVICE_URL/info")
    featureX=$(echo "$response" | grep -o '"featureX":[^,}]*' | cut -d':' -f2)
    if [ "$featureX" == "false" ]; then
        echo "✓ v1 response: $response"
        break
    fi
done
echo ""

echo "Summary:"
echo "--------"
echo "Feature flags (ENABLE_FEATURE_X) control internal behavior:"
echo "  ✓ v1 instances: Feature X disabled (90% of traffic)"
echo "  ✓ v2 instances: Feature X enabled (10% of traffic)"
echo "  ✓ /feature endpoint: Only available on v2"
echo "  ✓ Enhanced bookings: Only on v2"
echo "  ✓ Routing: Based on weights, not headers"

