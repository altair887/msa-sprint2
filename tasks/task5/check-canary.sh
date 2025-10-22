#!/bin/bash

set -e

echo "▶️ Checking canary release (90% v1, 10% v2)..."

# Посылаем 100 запросов
for i in {1..100}
do
    echo -n "Request $i: "
    curl -s http://127.0.0.1:65033/ping
    echo ""
done