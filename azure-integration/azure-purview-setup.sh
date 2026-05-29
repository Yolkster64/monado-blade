#!/bin/bash
# azure-purview-setup.sh: Automate Azure Purview account setup
set -e

RESOURCE_GROUP="my-resource-group"
PURVIEW_NAME="my-purview-account"
LOCATION="eastus"

# Create Purview account
echo "Creating Azure Purview account: $PURVIEW_NAME"
az purview account create --name $PURVIEW_NAME --resource-group $RESOURCE_GROUP --location $LOCATION

echo "Purview account $PURVIEW_NAME created at $(date)" | tee -a azure-purview-audit.log
