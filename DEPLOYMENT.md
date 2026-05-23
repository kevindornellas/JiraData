# Quick Start Guide

## Local Development with Docker Compose

The easiest way to get started locally is using Docker Compose, which sets up both the backend API, frontend dashboard, and SQL Server database.

### Prerequisites
- Docker and Docker Compose installed
- .NET 8 SDK (for local development without Docker)

### Quick Start

1. **Clone/Navigate to the project:**
   ```bash
   cd c:\repos\JiraData
   ```

2. **Update Configuration:**
   Edit `docker-compose.yml` and replace these values with your actual Jira credentials:
   ```
   Jira__BaseUrl=https://your-jira-instance.atlassian.net
   Jira__Username=your-email@example.com
   Jira__ApiToken=your-api-token
   ```

3. **Start the Stack:**
   ```bash
   docker-compose up -d
   ```

4. **Access the Application:**
   - **Frontend Dashboard**: http://localhost:5002
   - **Backend API**: http://localhost:5000
   - **SQL Server**: localhost:1433 (user: sa, password: YourPassword123!)

5. **Stop the Stack:**
   ```bash
   docker-compose down
   ```

### Initial Database Setup

Before running syncs, you need to create the database schema. Connect to the SQL Server and run:

```sql
CREATE DATABASE JiraData;

USE JiraData;

CREATE TABLE JiraIssue (
    Id INT PRIMARY KEY IDENTITY(1,1),
    StoryKey NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(MAX),
    Status NVARCHAR(50),
    Assignee NVARCHAR(255),
    Created NVARCHAR(50),
    Updated NVARCHAR(50),
    StoryPoints INT,
    QAPoints INT,
    Parent NVARCHAR(50),
    Sprint INT,
    CompletedDate NVARCHAR(50),
    Developer NVARCHAR(255),
    Tester NVARCHAR(255)
);

CREATE TABLE JiraComment (
    Id INT PRIMARY KEY IDENTITY(1,1),
    StoryKey NVARCHAR(50) NOT NULL,
    Author NVARCHAR(255),
    Body NVARCHAR(MAX),
    Created NVARCHAR(50),
    Updated NVARCHAR(50)
);

CREATE TABLE JiraHistory (
    Id INT PRIMARY KEY IDENTITY(1,1),
    StoryKey NVARCHAR(50) NOT NULL,
    Author NVARCHAR(255),
    Created NVARCHAR(50)
);

CREATE TABLE JiraHistoryItem (
    Id INT PRIMARY KEY IDENTITY(1,1),
    StoryKey NVARCHAR(50) NOT NULL,
    Author NVARCHAR(255),
    Created NVARCHAR(50),
    Field NVARCHAR(255),
    FromValue NVARCHAR(MAX),
    ToValue NVARCHAR(MAX)
);
```

---

## Production Deployment to Kubernetes

### Prerequisites
- A Kubernetes cluster (AKS, EKS, GKE, etc.)
- kubectl configured to access your cluster
- Docker images built and pushed to a container registry
- SQL Server instance (Azure SQL, on-premises, or container)

### Step 1: Build and Push Docker Images

**Backend:**
```bash
docker build -t your-registry/jiradata:1.0 .
docker push your-registry/jiradata:1.0
```

**Frontend:**
```bash
cd frontend
docker build -t your-registry/jiradata-frontend:1.0 .
docker push your-registry/jiradata-frontend:1.0
cd ..
```

### Step 2: Update Kubernetes Manifests

Edit the image references in the K8s manifests:

1. **K8s/deployment.yaml** - Update the backend image:
   ```yaml
   image: your-registry/jiradata:1.0
   ```

2. **K8s/frontend-deployment.yaml** - Update the frontend image:
   ```yaml
   image: your-registry/jiradata-frontend:1.0
   ```

3. **K8s/secrets.yaml** - Update with your actual credentials:
   ```yaml
   stringData:
     jira-base-url: "https://your-jira-instance.atlassian.net"
     jira-username: "your-email@example.com"
     jira-api-token: "your-api-token-here"
     sql-connection-string: "Server=your-sql-server;Database=JiraData;User Id=sa;Password=your-password;TrustServerCertificate=true;Encrypt=false;"
   ```

### Step 3: Deploy to Kubernetes

```bash
# Create namespace (optional)
kubectl create namespace jiradata

# Apply manifests (use -n jiradata if you created a namespace)
kubectl apply -f K8s/secrets.yaml
kubectl apply -f K8s/deployment.yaml
kubectl apply -f K8s/service.yaml
kubectl apply -f K8s/frontend-deployment.yaml
kubectl apply -f K8s/frontend-service.yaml
```

### Step 4: Verify Deployment

```bash
# Check pods are running
kubectl get pods

# Check services have external IPs
kubectl get svc

# View backend logs
kubectl logs -l app=jiradata-api -f

# View frontend logs
kubectl logs -l app=jiradata-frontend -f
```

### Step 5: Access the Application

Get the external IP of the frontend service:
```bash
kubectl get svc jiradata-frontend-service
```

Open your browser to `http://<EXTERNAL-IP>/`

---

## Using the Application

### Dashboard Features

1. **Latest Data in Database** - Shows the most recent date of inserted data
2. **Suggested Start Date** - Automatically calculated as latest date + 1 day
3. **API Status** - Health check indicator for the backend
4. **Trigger Sync** - Form to manually trigger a sync with custom date ranges

### Example Workflow

1. Open the frontend dashboard
2. Note the "Suggested Start Date" 
3. Adjust the end date as needed
4. Click "Sync Now"
5. Wait for completion and view results

### API Direct Usage

If you want to call the API directly:

```bash
# Trigger a sync
curl -X POST "http://your-service-ip/api/jira/sync?startDate=2026-05-01&endDate=2026-05-22"

# Get latest date
curl "http://your-service-ip/api/jira/latest-date"

# Check API status
curl "http://your-service-ip/api/jira/status"

# Health check
curl "http://your-service-ip/health"
```

---

## Troubleshooting

### Frontend Can't Connect to Backend
- Check that `JiraDataApi__BaseUrl` environment variable is set correctly
- Verify the backend API is running and accessible
- Check pod logs: `kubectl logs -l app=jiradata-api`

### Database Connection Failed
- Verify SQL connection string in secrets
- Ensure database server is accessible from the pod network
- Check that the database and tables exist

### Pods Not Starting
- Check pod events: `kubectl describe pod <pod-name>`
- Check container logs: `kubectl logs <pod-name>`
- Verify image references are correct

### Sync Failures
- Check backend API logs for detailed error messages
- Verify Jira credentials are correct
- Check that date ranges are valid and in YYYY-MM-DD format
