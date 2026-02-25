# k8s — AgroSolution Kubernetes Manifests

Deploy order: apply files in numerical order. All resources go to namespace `agrosolution`.

## Prerequisites

```bash
# Install minikube (if not installed)
# https://minikube.sigs.k8s.io/docs/start/

minikube start --cpus=4 --memory=4096
minikube addons enable ingress
minikube addons enable metrics-server
```

## Build & push Docker images

```bash
# Log in to GitHub Container Registry
echo $GITHUB_TOKEN | docker login ghcr.io -u <github-user> --password-stdin

# Build and push from solution root
docker build -f AgroSolution.Api/Dockerfile      -t ghcr.io/joaotorres/agrosolution-api:latest      .
docker build -f AgroSolution.Identity/Dockerfile  -t ghcr.io/joaotorres/agrosolution-identity:latest  .
docker build -f AgroSolution.Worker/Dockerfile    -t ghcr.io/joaotorres/agrosolution-worker:latest    .

docker push ghcr.io/joaotorres/agrosolution-api:latest
docker push ghcr.io/joaotorres/agrosolution-identity:latest
docker push ghcr.io/joaotorres/agrosolution-worker:latest
```

> **minikube shortcut** (no push needed):
> ```bash
> eval $(minikube docker-env)   # point local Docker to minikube's daemon
> docker build ...              # build directly inside minikube
> # Change imagePullPolicy to Never in 05-api.yaml, 06-identity.yaml, 07-worker.yaml
> ```

## Deploy

```bash
kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/01-configmap.yaml
kubectl apply -f k8s/02-secret.yaml
kubectl apply -f k8s/03-postgres.yaml
kubectl apply -f k8s/04-rabbitmq.yaml

# Wait for infra to be ready
kubectl wait --for=condition=ready pod -l app=postgres  -n agrosolution --timeout=120s
kubectl wait --for=condition=ready pod -l app=rabbitmq  -n agrosolution --timeout=120s

kubectl apply -f k8s/05-api.yaml
kubectl apply -f k8s/06-identity.yaml
kubectl apply -f k8s/07-worker.yaml
kubectl apply -f k8s/08-prometheus.yaml
kubectl apply -f k8s/09-grafana.yaml
kubectl apply -f k8s/10-ingress.yaml
```

## One-liner (after infra is ready)

```bash
kubectl apply -f k8s/
```

## Configure local DNS

Add to `/etc/hosts` (Linux/Mac) or `C:\Windows\System32\drivers\etc\hosts` (Windows):

```
# Get minikube IP
minikube ip   # e.g. 192.168.49.2
```

```
192.168.49.2  api.agrosolution.local
192.168.49.2  identity.agrosolution.local
192.168.49.2  grafana.agrosolution.local
192.168.49.2  prometheus.agrosolution.local
```

## Access URLs

| Service       | URL                                      |
|---------------|------------------------------------------|
| API (Swagger) | http://api.agrosolution.local/swagger    |
| Identity      | http://identity.agrosolution.local/swagger |
| Grafana       | http://grafana.agrosolution.local        |
| Prometheus    | http://prometheus.agrosolution.local     |
| RabbitMQ UI   | `kubectl port-forward svc/agrosolution-rabbitmq 15672:15672 -n agrosolution` → http://localhost:15672 |

## Verify deployment

```bash
kubectl get pods -n agrosolution
kubectl get svc  -n agrosolution
kubectl get ingress -n agrosolution
```

Expected output:
```
NAME                           READY   STATUS    RESTARTS   AGE
agrosolution-api-xxx           1/1     Running   0          1m
agrosolution-identity-xxx      1/1     Running   0          1m
agrosolution-postgres-0        1/1     Running   0          2m
agrosolution-rabbitmq-xxx      1/1     Running   0          2m
agrosolution-worker-xxx        1/1     Running   0          1m
grafana-xxx                    1/1     Running   0          1m
prometheus-xxx                 1/1     Running   0          1m
```

## Teardown

```bash
kubectl delete namespace agrosolution   # removes all resources
minikube stop
```
