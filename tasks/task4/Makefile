IMAGE_NAME=booking-service
IMAGE_TAG=latest
HELM_RELEASE=booking-service
HELM_CHART=helm/booking-service

.PHONY: build load deploy all clean ci

build:
	@echo "Building Docker image..."
	cd booking-service && docker build -t $(IMAGE_NAME):$(IMAGE_TAG) .

load:
	@echo "Loading image to minikube..."
	minikube image load $(IMAGE_NAME):$(IMAGE_TAG)

deploy:
	@echo "Deploying with Helm..."
	helm upgrade --install $(HELM_RELEASE) $(HELM_CHART) --set image.tag=$(IMAGE_TAG)
	kubectl rollout status deployment/$(HELM_RELEASE) --timeout=120s
	kubectl get all -l app=$(HELM_RELEASE)

all: build load deploy

clean:
	@echo "Cleaning up..."
	helm uninstall $(HELM_RELEASE) || true
	docker rmi $(IMAGE_NAME):$(IMAGE_TAG) || true

ci:
	npx gitlab-ci-local --shell-executor-no-image