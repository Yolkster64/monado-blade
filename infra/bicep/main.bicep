targetScope = 'resourceGroup'

@description('Deployment environment label used only for validation and naming.')
@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string = 'dev'

@description('Azure region for resources added in later guarded phases.')
param location string = resourceGroup().location

output validation object = {
  environmentName: environmentName
  location: location
  deploymentMode: 'what-if-only'
}
