#!/bin/bash
# filepath: c:\repos\JiraData\build.sh

# Build the Docker image
docker build -t repos/jiradata:latest .

# (Optional) Load the image into Minikube or Kind if using a local cluster
minikube image load repos/jiradata:latest
# kind load docker-image yourrepo/jiradata:latest

# Apply the Kubernetes CronJob
kubectl apply -f cronjob.yaml

echo "Build and deployment complete."