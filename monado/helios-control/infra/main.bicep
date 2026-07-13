param location string = resourceGroup().location
param environmentName string = 'dev'
param serviceName string = 'helios-connect'

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${serviceName}-${environmentName}-kv'
  location: location
  properties: {
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    sku: { family: 'A', name: 'standard' }
    networkAcls: { bypass: 'AzureServices', defaultAction: 'Deny' }
  }
}

resource bus 'Microsoft.ServiceBus/namespaces@2023-01-01-preview' = {
  name: '${serviceName}-${environmentName}-sb'
  location: location
  sku: { name: 'Standard', tier: 'Standard' }
}

resource topic 'Microsoft.ServiceBus/namespaces/topics@2023-01-01-preview' = {
  parent: bus
  name: 'events'
  properties: { enablePartitioning: true, supportOrdering: true }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take(replace('${serviceName}${environmentName}${uniqueString(resourceGroup().id)}', '-', ''), 24)
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

output keyVaultName string = vault.name
output serviceBusNamespace string = bus.name
output auditStorageName string = storage.name
