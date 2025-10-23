Для выполнения тестовы скриптов потреовалось создание конфигурационной записи типа gateway gateway.yml и запуск её перед каждым скриптом, для доступа к подам из хоста

minikube service istio-ingressgateway -n istio-system

Ддя ServiceDiscovery и DNS Resolving, была создага запись типа Service booking-service-service.yml

Для эмуляции недоступности пода при fallback тестировании применялась команда

kubectl scale deployment booking-service-task5 --replicas=0