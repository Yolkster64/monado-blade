#!/bin/bash
# azure-fabric-setup.sh: Automate Azure Fabric workspace setup
set -e

RESOURCE_GROUP="my-resource-group"
FABRIC_NAME="my-fabric-workspace"
LOCATION="eastus"

# Create Fabric workspace
echo "Creating Azure Fabric workspace: $FABRIC_NAME"
az fabric workspace create --name $FABRIC_NAME --resource-group $RESOURCE_GROUP --location $LOCATION

echo "Fabric workspace $FABRIC_NAME created at $(date)" | tee -a azure-fabric-audit.log
