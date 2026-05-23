# Jira Data Sync Application

A distributed Jira data synchronization system with a backend API and frontend dashboard, deployable to Kubernetes.

## Architecture

- **Backend API** (`JiraData`): Core service that syncs Jira issues to SQL database
- **Frontend** (`frontend`): ASP.NET Razor Pages dashboard for managing syncs and viewing status

## Project Structure

```
JiraData/
├── Program.cs                    # Backend API entry point
├── JiraData.csproj              # Backend project file
├── [other backend files]
├── frontend/                     # Frontend web app
│   ├── Program.cs               # Frontend entry point
│   ├── JiraDataFrontend.csproj  # Frontend project file
│   ├── appsettings.json         # Frontend config
│   ├── Dockerfile               # Frontend container image
│   ├── Pages/
│   │   ├── Index.cshtml         # Dashboard UI
│   │   └── Index.cshtml.cs      # Dashboard logic
├── K8s/                         # Kubernetes manifests
│   ├── secrets.yaml             # Credentials (backend)
│   ├── deployment.yaml          # Backend deployment
│   ├── service.yaml             # Backend service
│   ├── frontend-deployment.yaml # Frontend deployment
│   └── frontend-service.yaml    # Frontend service
└── dockerfile                    # Backend container image
```

## Backend API Endpoints

### POST /api/jira/sync
Triggers a sync of Jira data for a date range.

**Parameters:**
- `startDate` (query): Start date in YYYY-MM-DD format
- `endDate` (query): End date in YYYY-MM-DD format

**Example:**
```bash
curl -X POST "http://localhost:5000/api/jira/sync?startDate=2026-01-01&endDate=2026-05-31"
```

### GET /api/jira/latest-date
Returns the latest date when data was inserted into the database.

**Example:**
```bash
curl "http://localhost:5000/api/jira/latest-date"
```

**Response:**
```json
{
  "latestDate": "2026-05-20"
}
```

### GET /api/jira/status
Returns API status information.

**Example:**
```bash
curl "http://localhost:5000/api/jira/status"
```

### GET /health
Health check endpoint for Kubernetes probes.

## Frontend Dashboard Features

- **Display Latest Database Date**: Shows the most recent insertion date from the database
- **Suggest Start Date**: Automatically suggests the next start date (latest date + 1 day)
- **Trigger Sync**: Simple form to trigger a sync with custom date ranges
- **API Health Check**: Displays if the backend API is available
- **Sync Results**: Shows the results of the last sync operation

## Building Docker Images

### Backend
```bash
docker build -t jiradata:latest .
```

### Frontend
```bash
cd frontend
docker build -t jiradata-frontend:latest .
```

## Kubernetes Deployment

### 1. Update Secrets
Edit `K8s/secrets.yaml` with your actual credentials:

```yaml
stringData:
  jira-base-url: "https://your-jira-instance.atlassian.net"
  jira-username: "your-email@example.com"
  jira-api-token: "your-api-token-here"
  sql-connection-string: "Server=your-sql-server;Database=your-db;User Id=sa;Password=your-password;..."
```

### 2. Deploy to Kubernetes

```bash
# Create secrets
kubectl apply -f K8s/secrets.yaml

# Deploy backend
kubectl apply -f K8s/deployment.yaml
kubectl apply -f K8s/service.yaml

# Deploy frontend
kubectl apply -f K8s/frontend-deployment.yaml
kubectl apply -f K8s/frontend-service.yaml
```

### 3. Verify Deployment

```bash
# Check pods
kubectl get pods

# Check services
kubectl get svc

# View logs
kubectl logs -l app=jiradata-api
kubectl logs -l app=jiradata-frontend
```

### 4. Access the Application

Get the external IP of the frontend service:
```bash
kubectl get svc jiradata-frontend-service
```

Open your browser to `http://<EXTERNAL-IP>/`

## Environment Variables

Both applications support environment variables (prefixed with double underscore):

### Backend (JiraData)
- `Jira__BaseUrl`: Jira instance URL
- `Jira__Username`: Jira username
- `Jira__ApiToken`: Jira API token
- `Jira__StartDate`: Default start date (fallback)
- `Jira__EndDate`: Default end date (fallback)
- `Sql__ConnectionString`: SQL database connection string

### Frontend (JiraDataFrontend)
- `JiraDataApi__BaseUrl`: Backend API URL (default: `http://jiradata-api-service:5000`)

## Configuration Files

### Backend: appsettings.json
```json
{
  "Jira": {
    "BaseUrl": "https://your-jira.atlassian.net",
    "Username": "email@example.com",
    "ApiToken": "your-token",
    "StartDate": "2026-01-01",
    "EndDate": "2026-12-31"
  },
  "Sql": {
    "ConnectionString": "Server=localhost;Database=JiraData;..."
  }
}
```

### Frontend: appsettings.json
```json
{
  "JiraDataApi": {
    "BaseUrl": "http://jiradata-api-service:5000"
  }
}
```

## Local Development

### Backend
```bash
dotnet run
```
Runs on `https://localhost:5001` and `http://localhost:5000`

### Frontend
```bash
cd frontend
dotnet run
```
Runs on `https://localhost:5001` and `http://localhost:5000`

## Notes

- The frontend automatically discovers and connects to the backend API using the configured base URL
- The sync operation can take a long time for large date ranges; consider running syncs during off-hours
- SQL database must be pre-created with the appropriate schema
- All dates should be in YYYY-MM-DD format
