#!/bin/bash

# Script to test header-based routing and canary routing

echo "Testing Header-Based Routing and Canary Release"
echo "================================================"
echo ""

# Get the Istio gateway URL
echo "Getting Istio Ingress Gateway URL..."
GATEWAY_URL=$(minikube service istio-ingressgateway -n istio-system --url 2>/dev/null | sed -n '2p')

if [ -z "$GATEWAY_URL" ]; then
    echo "❌ Failed to get Istio gateway URL"
    echo "Make sure minikube is running and Istio is installed"
    exit 1
fi

echo "✅ Gateway URL: $GATEWAY_URL"
echo ""

# Test 1: Normal canary routing (without header)
echo "Test 1: Normal Canary Routing (without header)"
echo "-----------------------------------------------"
echo "Expected: ~90% v1, ~10% v2"
echo ""

v1_count=0
v2_count=0

for i in {1..10}; do
    response=$(curl -s "$GATEWAY_URL/ping")
    echo "Request $i: $response"
    
    if echo "$response" | grep -q "v1"; then
        ((v1_count++))
    elif echo "$response" | grep -q "v2"; then
        ((v2_count++))
    fi
done

echo ""
echo "Results: v1=$v1_count, v2=$v2_count"
echo ""

# Test 2: Header-based routing (force v2)
echo "Test 2: Header-Based Routing (X-Feature-Enabled: true)"
echo "-------------------------------------------------------"
echo "Expected: 100% v2"
echo ""

v2_header_count=0

for i in {1..10}; do
    response=$(curl -s -H "X-Feature-Enabled: true" "$GATEWAY_URL/ping")
    echo "Request $i: $response"
    
    if echo "$response" | grep -q "v2"; then
        ((v2_header_count++))
    fi
done

echo ""
echo "Results: v2=$v2_header_count/10"

if [ $v2_header_count -eq 10 ]; then
    echo "✅ Header routing working correctly (100% v2)"
else
    echo "❌ Header routing failed (expected 10/10 v2, got $v2_header_count/10)"
fi
echo ""

# Test 3: Feature endpoint (v2 only)
echo "Test 3: Feature Endpoint (v2 only)"
echo "-----------------------------------"
echo "Accessing /feature with header (should work):"
feature_response=$(curl -s -H "X-Feature-Enabled: true" "$GATEWAY_URL/feature")
echo "$feature_response"

if echo "$feature_response" | grep -q "Feature X is enabled"; then
    echo "✅ Feature endpoint accessible on v2"
else
    echo "❌ Feature endpoint not accessible"
fi
echo ""

# Test 4: Info endpoint
echo "Test 4: Info Endpoint"
echo "---------------------"
echo "Without header (random):"
curl -s "$GATEWAY_URL/info" | head -1
echo ""

echo "With header (v2):"
curl -s -H "X-Feature-Enabled: true" "$GATEWAY_URL/info" | head -1
echo ""

# Summary
echo "========================================="
echo "Summary:"
echo "========================================="
echo "Gateway URL: $GATEWAY_URL"
echo ""
echo "Canary Routing (no header):"
echo "  v1: $v1_count/10 (~$(( v1_count * 10 ))%)"
echo "  v2: $v2_count/10 (~$(( v2_count * 10 ))%)"
echo ""
echo "Header Routing (X-Feature-Enabled: true):"
echo "  v2: $v2_header_count/10 ($(( v2_header_count * 10 ))%)"
echo ""
echo "Commands to use:"
echo "  Normal request:  curl $GATEWAY_URL/ping"
echo "  Force v2:        curl -H 'X-Feature-Enabled: true' $GATEWAY_URL/ping"
echo "  Feature access:  curl -H 'X-Feature-Enabled: true' $GATEWAY_URL/feature"

