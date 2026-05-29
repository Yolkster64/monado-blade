#!/bin/bash
# azure-keyvault-setup.sh: Automate Azure Key Vault setup and secret management
set -e

RESOURCE_GROUP="my-resource-group"
KEYVAULT_NAME="my-keyvault"
LOCATION="eastus"
SECRET_NAME="my-secret"
SECRET_VALUE="my-secret-value"

# Create Key Vault
echo "Creating Azure Key Vault: $KEYVAULT_NAME"
az keyvault create --name $KEYVAULT_NAME --resource-group $RESOURCE_GROUP --location $LOCATION

# Store a secret
echo "Storing secret $SECRET_NAME in Key Vault"
az keyvault secret set --vault-name $KEYVAULT_NAME --name $SECRET_NAME --value $SECRET_VALUE

echo "Key Vault $KEYVAULT_NAME and secret $SECRET_NAME created at $(date)" | tee -a azure-keyvault-audit.log
