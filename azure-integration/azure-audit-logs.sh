#!/bin/bash
# azure-audit-logs.sh: Collect and display Azure audit logs for resource actions
set -e

RESOURCE_GROUP="my-resource-group"

# Query activity logs for the resource group
echo "Fetching Azure activity logs for $RESOURCE_GROUP"
az monitor activity-log list --resource-group $RESOURCE_GROUP --max-events 50 --output table

echo "Audit log query completed at $(date)" | tee -a azure-audit-logs.log
