#!/bin/bash
# azure-register.sh: Automate Azure app registration and Entra ID setup
set -e

# Variables (set these as needed)
APP_NAME="my-app-registration"
RESOURCE_GROUP="my-resource-group"
LOCATION="eastus"

# Register Azure AD application
echo "Registering Azure AD application: $APP_NAME"
APP_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)

# Create service principal
echo "Creating service principal for app: $APP_ID"
SP_PASSWD=$(az ad sp create-for-app --id $APP_ID --query password -o tsv)
SP_ID=$(az ad sp show --id $APP_ID --query objectId -o tsv)

# Assign Reader role to the service principal
echo "Assigning Reader role to service principal"
az role assignment create --assignee $SP_ID --role Reader --scope "/subscriptions/$(az account show --query id -o tsv)"

# Output credentials
echo "AppId: $APP_ID"
echo "Password: $SP_PASSWD"

# Audit log
echo "App registration and service principal created for $APP_NAME at $(date)" | tee -a azure-register-audit.log
