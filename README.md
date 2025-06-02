## Running tests

```bash
# execute unit tests
cd BookingMicroservices
 dotnet test
```

## Observability (Health-checks / Metrics / Logs)

### Health-checks
- Every microservice exposes `GET /health` powered by **AspNetCore.HealthChecks**.
- Aggregated view: `http://localhost:8080/health` (API-Gateway) returns JSON with status & latency for `catalog`, `room`, `booking`, `payment`, and `identity` services.

### Prometheus metrics
- `CatalogService` exports Prometheus metrics at `GET /metrics` via **prometheus-net**.
- Local Docker Compose:
  ```bash
  docker compose exec catalogservice curl -s http://localhost/metrics | head
  ```
- In Kubernetes the same path is reachable inside the cluster; attach a `ServiceMonitor` if you use the Prometheus Operator.

### Serilog JSON logs
- Structured logs are written to **stdout** in JSON format.
- Tail logs:
  ```bash
  # Docker Compose
  docker compose logs -f catalogservice

  # Kubernetes
  kubectl logs -f deployment/catalog-service
  ```

## Local development quick-checks

Run services directly with `dotnet run` (no Docker/K8s) and use `curl` / PowerShell to probe endpoints.

```bash
# start CatalogService (terminal 1)
dotnet run --project src/Services/CatalogService.API
# start ApiGateway (terminal 2) – optional for aggregated health
dotnet run --project src/Services/ApiGateway
```

Health-check:
```bash
curl -s http://localhost:5023/health        # CatalogService
curl -s http://localhost:5108/health | jq   # Aggregated via ApiGateway (local run, see launchSettings)
# If running via Docker Compose use:
# curl -s http://localhost:8080/health | jq
```

Prometheus metrics (first lines):
```bash
curl -s http://localhost:5023/metrics | head
```

Serilog JSON logs already stream to the same terminal window that runs `dotnet run`.

## Kubernetes quick-start (CatalogService)

`k8s/catalog-service.yaml` defines Deployment and Service.

```bash
# create / update deployment
kubectl apply -f k8s/catalog-service.yaml

# watch rollout
kubectl rollout status deployment/catalog-service

# hot-restart pods
kubectl rollout restart deployment/catalog-service

# pause / resume rollout (for progressive delivery)
kubectl rollout pause deployment/catalog-service
kubectl rollout resume deployment/catalog-service

# scale to zero (temporary stop)
kubectl scale deployment/catalog-service --replicas 0

# delete all resources
kubectl delete -f k8s/catalog-service.yaml
```

