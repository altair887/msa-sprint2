#!/bin/bash

# Script to deploy and configure canary release

echo "Deploying Canary Release Setup"
echo "==============================="
echo ""

# Build Docker images
echo "Step 1: Building Docker images..."
echo "---------------------------------"

cd booking-service || exit 1

echo "Building v1 image..."
docker build -t booking-service:latest -t booking-service:v1 .

echo "Building v2 image..."
docker build -t booking-service:v2 .

cd ..

echo "✅ Docker images built"
echo ""

# Deploy to Kubernetes
echo "Step 2: Deploying to Kubernetes..."
echo "-----------------------------------"

# Deploy v1
echo "Deploying v1..."
kubectl apply -f booking-service-deployment.yml

# Wait for v1 to be ready
echo "Waiting for v1 to be ready..."
kubectl wait --for=condition=available --timeout=60s deployment/booking-service-task5

# Deploy Service
echo "Deploying Service..."
kubectl apply -f booking-service-service.yml

# Deploy v2
echo "Deploying v2..."
kubectl apply -f booking-service-deployment-v2.yml

# Wait for v2 to be ready
echo "Waiting for v2 to be ready..."
kubectl wait --for=condition=available --timeout=60s deployment/booking-service-task5-v2

# Deploy Istio traffic rules
echo "Deploying Istio traffic configuration..."
kubectl apply -f booking-service-traffic.yml

echo "✅ All resources deployed"
echo ""

# Show deployment status
echo "Step 3: Checking deployment status..."
echo "--------------------------------------"
echo ""
echo "Deployments:"
kubectl get deployments -l app=booking-service-task5
echo ""
echo "Pods:"
kubectl get pods -l app=booking-service-task5
echo ""
echo "Service:"
kubectl get service booking-service-task5
echo ""
echo "VirtualService:"
kubectl get virtualservice booking-service-task5
echo ""
echo "DestinationRule:"
kubectl get destinationrule booking-service-task5
echo ""

echo "✅ Canary deployment complete!"
echo ""
echo "Next steps:"
echo "  1. Run ./test-canary.sh to verify traffic distribution"
echo "  2. Run ./test-feature-flags.sh to test feature flags"
echo "  3. Monitor metrics and gradually increase v2 traffic"
echo ""
echo "To adjust traffic split, edit booking-service-traffic.yml"
echo "and change the weight values (currently 90% v1, 10% v2)"

