# BookingMicroservices: Cloud-Native Hotel Booking System

[![.NET CI, Docker Build and Push](https://github.com/nikmorror37/BookingMicroservices/actions/workflows/ci.yml/badge.svg)](https://github.com/nikmorror37/BookingMicroservices/actions/workflows/ci.yml)

## 1. Project Overview

**BookingMicroservices** is a diploma project demonstrating a cloud-native microservices application for a hotel booking system. The primary goal is to showcase an understanding of microservice architecture principles, .NET 8 technologies, containerization, and modern development practices.

The system comprises seven independently deployable services:
*   **IdentityService.API:** Manages user authentication (ASP.NET Core Identity) and JWT issuance.
*   **CatalogService.API:** Handles hotel and room catalog information, including image management.
*   **RoomService.API:** Manages room inventory, availability, and types.
*   **BookingService.API:** Orchestrates the booking lifecycle, potentially using SAGA-like patterns for distributed transactions.
*   **PaymentService.API:** Processes payments and refunds, interacting with a (mocked) payment gateway.
*   **ApiGateway:** A YARP-based API Gateway serving as the single entry point, handling routing, rate limiting, and aggregated health checks.
*   **BookingWebApp:** An ASP.NET Core MVC application providing the user interface for guests and administrators.

Asynchronous communication between services is facilitated by **RabbitMQ** with **MassTransit**. Each core service maintains its own **SQL Server** database (Database-per-Service pattern). The entire system is containerized using **Docker** and orchestrated locally via **Docker Compose**.

## 2. Technologies Used

*   **.NET 8:** ASP.NET Core for API and MVC development, Entity Framework Core for data access.
*   **C# 12:** Primary programming language.
*   **Docker & Docker Compose:** For containerization and local orchestration.
*   **Kubernetes (K8s):** Example manifest (`k8s/catalog-service.yaml`) provided for `CatalogService.API` to demonstrate cloud-native deployment readiness, including secret management via Kubernetes Secrets.
*   **RabbitMQ & MassTransit:** For asynchronous event-driven communication.
*   **YARP (Yet Another Reverse Proxy):** Powers the API Gateway.
*   **SQL Server:** Relational database backend for each service.
*   **JWT (JSON Web Tokens):** For stateless authentication.
*   **Serilog:** For structured JSON logging (implemented in `CatalogService.API`).
*   **Prometheus-net:** For exposing application metrics in Prometheus format (implemented in `CatalogService.API`).
*   **AspNetCore.HealthChecks:** For implementing health check endpoints in all services and aggregating them in the API Gateway.
*   **xUnit & FluentAssertions:** For unit testing.
*   **GitHub Actions:** For CI/CD (automated testing, Docker image building, and publishing to GitHub Container Registry).

## 3. Getting Started

### 3.1. Prerequisites

*   .NET 8 SDK
*   Docker Desktop (with Docker Compose and Kubernetes enabled if testing K8s deployment)
*   Git
*   A code editor (e.g., Visual Studio, VS Code, JetBrains Rider)

### 3.2. Local Development Setup (Docker Compose)

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/nikmorror37/BookingMicroservices.git
    cd BookingMicroservices
    ```

2.  **Configure Secrets:**
    *   Copy the example environment file:
        ```powershell
        copy .env.example .env
        ```
    *   Edit the `.env` file and provide actual values for `SA_PASSWORD` and `JWT_KEY`. This file is ignored by Git.

3.  **Run the application:**
    ```bash
    docker compose up -d --build
    ```
    This command will build/pull necessary images and start all services, including SQL Server and RabbitMQ.

4.  **Accessing the application:**
    *   **BookingWebApp (UI):** `http://localhost:8081`
    *   **API Gateway:** `http://localhost:8080` (all API calls from BookingWebApp go through here)
    *   **RabbitMQ Management UI:** `http://localhost:15672` (guest/guest)

### 3.2.1. Accessing API Documentation (Swagger UI via API Gateway)

Individual Swagger UIs for each microservice are proxied through the API Gateway for centralized access:

*   **CatalogService API:** `http://localhost:8080/gateway/catalog/swagger/`
*   **IdentityService API:** `http://localhost:8080/gateway/identity/swagger/`
*   **RoomService API:** `http://localhost:8080/gateway/room/swagger/`
*   **BookingService API:** `http://localhost:8080/gateway/booking/swagger/`
*   **PaymentService API:** `http://localhost:8080/gateway/payment/swagger/`

Ensure all services and the API Gateway are running via `docker compose up -d`.

### 3.3. Stopping the application (Docker Compose):

```bash
docker compose down # Stops and removes containers
# To also remove volumes (e.g., database data):
# docker compose down -v
```

### 3.4. Running Individual Services Locally (dotnet run)

Each service can also be run individually using `dotnet run` from its project directory (e.g., `src/Services/CatalogService.API`). Ensure that SQL Server and RabbitMQ (from Docker Compose or a local installation) are accessible and connection strings/hostnames in `appsettings.json` are configured accordingly for this scenario. Default `launchSettings.json` profiles are configured for local development.

## 4. Testing

### 4.1. Unit Tests

The project includes unit tests written with xUnit and FluentAssertions, located in the `tests/BookingMicro.UnitTests` directory.

*   **To run all unit tests:**
    ```bash
    cd BookingMicroservices # Navigate to the solution root
    dotnet test
    ```
    Or, to run tests for a specific test project:
    ```bash
    dotnet test tests/BookingMicro.UnitTests/BookingMicro.UnitTests.csproj
    ```
    Currently, 8 unit tests are implemented, covering domain logic and controller actions for Booking, Payment, and Catalog services.

## 5. Observability (Health Checks, Metrics, Logs)

The system incorporates several mechanisms for monitoring and diagnostics.

### 5.1. Health Checks

*   Each microservice exposes a `GET /health` endpoint (e.g., `http://localhost:5023/health` for CatalogService if run locally, or `http://catalogservice/health` within the Docker network).
*   The **API Gateway** provides an aggregated health view of all downstream services:
    *   Docker Compose: `curl -s http://localhost:8080/health | jq`
    *   Local `dotnet run` (ApiGateway on port 5108): `curl -s http://localhost:5108/health | jq`

### 5.2. Prometheus Metrics

*   `CatalogService.API` is configured to export metrics in Prometheus format via `prometheus-net.AspNetCore`.
*   **Accessing metrics:**
    *   When running via Docker Compose (port 5023 mapped for CatalogService): `http://localhost:5023/metrics`
    *   When running `CatalogService.API` locally via `dotnet run` (default port 5023): `http://localhost:5023/metrics`
    *   To view from another container within the Docker network or from the Docker host via `exec`:
        ```bash
        # Using sh -c for pipes within the container
        docker compose exec catalogservice sh -c "curl -s http://localhost/metrics | head -n 20"
        # Or using PowerShell's Select-Object if piping on the host
        docker compose exec catalogservice curl -s http://localhost/metrics | Select-Object -First 20
        ```

### 5.3. Structured Logging (Serilog)

*   `CatalogService.API` uses Serilog to write structured logs (JSON format) to `stdout`. This is suitable for collection by log aggregation systems.
*   **Viewing logs:**
    *   Docker Compose: `docker compose logs -f catalogservice`
    *   Kubernetes (for the example deployment): `kubectl logs -f deployment/catalog-service`
    *   Local `dotnet run`: Logs will appear directly in the console window where the service is running.

## 6. CI/CD with GitHub Actions

A CI/CD pipeline is configured using GitHub Actions (`.github/workflows/ci.yml`). It automates:
1.  **Running `dotnet test`** for the entire solution on each push/pull_request to `main`/`master`.
2.  **Building Docker images** for all services and the web application.
3.  **Pushing tagged Docker images** to GitHub Container Registry (`ghcr.io/nikmorror37/`) if tests pass. Images are tagged with the commit SHA and `latest` for pushes to `main`/`master`.

The build status badge at the top of this README reflects the current state of this pipeline.

## 7. Kubernetes Deployment Example (CatalogService)

To demonstrate cloud-native deployment capabilities, a sample Kubernetes deployment is provided for `CatalogService.API`.

### 7.1. Files:
*   `k8s/catalog-secrets.yaml`: Defines a Kubernetes Secret for `SA_PASSWORD` and `JWT_KEY`.
*   `k8s/catalog-service.yaml`: Defines a Deployment and a ClusterIP Service for `CatalogService.API`, configured to use the aforementioned secret.

### 7.2. Deployment & Management (requires a running Kubernetes cluster, e.g., Docker Desktop Kubernetes or Minikube):

1.  **Ensure your local Docker image `catalogservice:demo` is built and accessible to your K8s cluster.**
    (e.g., `docker build -t catalogservice:demo -f src/Services/CatalogService.API/Dockerfile .` and for Minikube: `minikube image load catalogservice:demo`)
    Alternatively, update `k8s/catalog-service.yaml` to use an image from GHCR (e.g., `ghcr.io/nikmorror37/catalogservice:latest`).

2.  **Apply manifests:**
    ```bash
    kubectl apply -f k8s/catalog-secrets.yaml
    kubectl apply -f k8s/catalog-service.yaml
    ```

3.  **Check status:**
    ```bash
    kubectl get deployment catalog-service
    kubectl get pods -l app=catalog-service
    kubectl logs deployment/catalog-service -f # View logs
    ```

4.  **Verify secrets are used (PowerShell example):**
    ```powershell
    $POD_NAME = kubectl get pods -l app=catalog-service -o jsonpath='{.items[0].metadata.name}'
    kubectl exec $POD_NAME -- printenv | findstr "SA_PASSWORD ConnectionStrings__CatalogDb Jwt__Key"
    ```

5.  **Accessing the service (e.g., for testing):**
    ```bash
    kubectl port-forward svc/catalog-service 5023:80 # Forward local port 5023 to service port 80
    # Then access via http://localhost:5023 (e.g., /health, /metrics)
    ```

6.  **Stopping/Deleting the K8s deployment:**
    ```bash
    kubectl delete -f k8s/catalog-service.yaml
    kubectl delete -f k8s/catalog-secrets.yaml
    ```
    Or to temporarily stop without deleting definitions (scale to zero):
    ```bash
    kubectl scale deployment catalog-service --replicas 0
    ```

7.  **Advanced Management Operations (Rollouts, Restarts):**
    ```bash
    # Watch rollout status after an update
    kubectl rollout status deployment/catalog-service

    # Force a rolling restart of the pods in the deployment
    kubectl rollout restart deployment/catalog-service

    # Pause and resume a rollout (useful for canary or blue/green if configured)
    kubectl rollout pause deployment/catalog-service
    kubectl rollout resume deployment/catalog-service
    ```

## 8. Project Structure

(A brief overview of the main directories and their purpose - can be expanded if needed)
*   `.github/workflows/`: GitHub Actions CI/CD workflows.
*   `k8s/`: Kubernetes manifest files.
*   `src/`: Contains all source code.
    *   `Contracts/`: Shared data contracts and event definitions.
    *   `Services/`: Individual microservice projects.
    *   `Web/BookingWebApp/`: The ASP.NET Core MVC client application.
*   `tests/`: Unit test projects.
*   `docker-compose.yml`: Defines the multi-container application for Docker Compose.
*   `Dockerfile.rabbit`: Custom Dockerfile for RabbitMQ.
*   `BookingMicroservices.sln`: Visual Studio Solution file.


