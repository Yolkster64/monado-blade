using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Cloud;

/// <summary>Unified cloud integration factory with support for Azure, AWS, and GCP</summary>
public interface ICloudIntegrationFactory
{
    ICloudProvider CreateAzureProvider(string subscriptionId, string tenantId);
    ICloudProvider CreateAWSProvider(string accessKeyId, string secretAccessKey, string region);
    ICloudProvider CreateGCPProvider(string projectId, string credentialsPath);
    Task<IEnumerable<ICloudProvider>> GetConfiguredProvidersAsync(CancellationToken ct = default);
}

/// <summary>Unified cloud provider interface</summary>
public interface ICloudProvider
{
    string ProviderName { get; }
    CloudProviderType ProviderType { get; }
    Task<bool> AuthenticateAsync(CancellationToken ct = default);
    Task<CloudProviderStatus> GetStatusAsync(CancellationToken ct = default);
}

/// <summary>Cloud provider type enumeration</summary>
public enum CloudProviderType
{
    Azure,
    AWS,
    GCP
}

/// <summary>Cloud provider status information</summary>
public record CloudProviderStatus
{
    public bool IsAuthenticated { get; init; }
    public bool IsAvailable { get; init; }
    public int ResourceCount { get; init; }
    public Dictionary<string, object> Metrics { get; init; } = new();
    public DateTime LastCheckTime { get; init; }
}

/// <summary>Azure cloud integration</summary>
public interface IAzureIntegration : ICloudProvider
{
    /// <summary>Get Azure resources (VMs, storage, containers)</summary>
    Task<IEnumerable<AzureResource>> GetResourcesAsync(CancellationToken ct = default);
    
    /// <summary>Deploy container to Azure Container Registry</summary>
    Task DeployContainerAsync(string imageName, string imageTag, CancellationToken ct = default);
    
    /// <summary>Submit compute job to Azure Batch</summary>
    Task<string> SubmitBatchJobAsync(AzureBatchJob job, CancellationToken ct = default);
    
    /// <summary>Upload data to Azure Storage</summary>
    Task UploadToStorageAsync(string containerName, string blobName, Stream data, CancellationToken ct = default);
    
    /// <summary>Access Azure DevOps pipelines</summary>
    Task<IEnumerable<AzureDevOpsProject>> GetDevOpsProjectsAsync(CancellationToken ct = default);
}

/// <summary>Azure resource representation</summary>
public record AzureResource
{
    public string ResourceId { get; init; } = string.Empty;
    public string ResourceName { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public Dictionary<string, string> Tags { get; init; } = new();
}

/// <summary>Azure Batch job specification</summary>
public record AzureBatchJob
{
    public string JobName { get; init; } = string.Empty;
    public string ContainerImage { get; init; } = string.Empty;
    public int TaskCount { get; init; }
    public Dictionary<string, string> EnvironmentVariables { get; init; } = new();
}

/// <summary>Azure DevOps project</summary>
public record AzureDevOpsProject
{
    public string ProjectId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string Visibility { get; init; } = string.Empty;
}

/// <summary>AWS cloud integration</summary>
public interface IAWSIntegration : ICloudProvider
{
    /// <summary>Get EC2 instances</summary>
    Task<IEnumerable<EC2Instance>> GetInstancesAsync(CancellationToken ct = default);
    
    /// <summary>Get S3 buckets</summary>
    Task<IEnumerable<S3Bucket>> GetBucketsAsync(CancellationToken ct = default);
    
    /// <summary>Submit Lambda function</summary>
    Task<LambdaExecutionResult> InvokeLambdaAsync(string functionName, string payload, CancellationToken ct = default);
    
    /// <summary>Upload to S3</summary>
    Task UploadToS3Async(string bucketName, string key, Stream data, CancellationToken ct = default);
    
    /// <summary>Access CodePipeline</summary>
    Task<IEnumerable<CodePipeline>> GetPipelinesAsync(CancellationToken ct = default);
}

/// <summary>EC2 instance representation</summary>
public record EC2Instance
{
    public string InstanceId { get; init; } = string.Empty;
    public string InstanceType { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PublicIpAddress { get; init; } = string.Empty;
}

/// <summary>S3 bucket representation</summary>
public record S3Bucket
{
    public string BucketName { get; init; } = string.Empty;
    public DateTime CreationDate { get; init; }
}

/// <summary>Lambda execution result</summary>
public record LambdaExecutionResult
{
    public int StatusCode { get; init; }
    public string Payload { get; init; } = string.Empty;
    public string? Error { get; init; }
}

/// <summary>AWS CodePipeline</summary>
public record CodePipeline
{
    public string PipelineName { get; init; } = string.Empty;
    public string PipelineVersion { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

/// <summary>GCP cloud integration</summary>
public interface IGCPIntegration : ICloudProvider
{
    /// <summary>Get Compute Engine instances</summary>
    Task<IEnumerable<ComputeInstance>> GetInstancesAsync(CancellationToken ct = default);
    
    /// <summary>Get Cloud Storage buckets</summary>
    Task<IEnumerable<StorageBucket>> GetBucketsAsync(CancellationToken ct = default);
    
    /// <summary>Submit Cloud Functions</summary>
    Task<string> InvokeCloudFunctionAsync(string functionName, object payload, CancellationToken ct = default);
    
    /// <summary>Upload to Cloud Storage</summary>
    Task UploadToBucketAsync(string bucketName, string objectName, Stream data, CancellationToken ct = default);
}

/// <summary>GCP Compute Engine instance</summary>
public record ComputeInstance
{
    public string InstanceName { get; init; } = string.Empty;
    public string MachineType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Zone { get; init; } = string.Empty;
}

/// <summary>GCP Cloud Storage bucket</summary>
public record StorageBucket
{
    public string BucketName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime TimeCreated { get; init; }
}

/// <summary>GitHub integration for CI/CD</summary>
public interface IGitHubIntegration : ICloudProvider
{
    /// <summary>Get repository workflows</summary>
    Task<IEnumerable<GitHubWorkflow>> GetWorkflowsAsync(string owner, string repo, CancellationToken ct = default);
    
    /// <summary>Trigger workflow run</summary>
    Task<string> TriggerWorkflowAsync(string owner, string repo, string workflowId, string branch, CancellationToken ct = default);
    
    /// <summary>Get workflow run status</summary>
    Task<GitHubWorkflowRun> GetWorkflowRunAsync(string owner, string repo, string runId, CancellationToken ct = default);
    
    /// <summary>Create release</summary>
    Task<GitHubRelease> CreateReleaseAsync(string owner, string repo, string tagName, string description, CancellationToken ct = default);
}

/// <summary>GitHub workflow</summary>
public record GitHubWorkflow
{
    public string WorkflowId { get; init; } = string.Empty;
    public string WorkflowName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

/// <summary>GitHub workflow run</summary>
public record GitHubWorkflowRun
{
    public string RunId { get; init; } = string.Empty;
    public string Conclusion { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

/// <summary>GitHub release</summary>
public record GitHubRelease
{
    public string ReleaseId { get; init; } = string.Empty;
    public string TagName { get; init; } = string.Empty;
    public string ReleaseName { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
}

/// <summary>Docker registry integration</summary>
public interface IDockerRegistryIntegration
{
    /// <summary>Push image to Docker registry</summary>
    Task PushImageAsync(string imageName, string tag, CancellationToken ct = default);
    
    /// <summary>Pull image from registry</summary>
    Task PullImageAsync(string imageName, string tag, CancellationToken ct = default);
    
    /// <summary>Get image metadata</summary>
    Task<DockerImageMetadata> GetImageMetadataAsync(string imageName, string tag, CancellationToken ct = default);
}

/// <summary>Docker image metadata</summary>
public record DockerImageMetadata
{
    public string ImageName { get; init; } = string.Empty;
    public string Tag { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public long SizeBytes { get; init; }
}

/// <summary>OneDrive sync integration</summary>
public interface IOneDriveSync
{
    /// <summary>Sync folder to OneDrive</summary>
    Task SyncFolderAsync(string localPath, string remotePath, CancellationToken ct = default);
    
    /// <summary>Get sync status</summary>
    Task<OneDriveSyncStatus> GetSyncStatusAsync(CancellationToken ct = default);
    
    /// <summary>Upload file to OneDrive</summary>
    Task UploadFileAsync(string localPath, string remotePath, CancellationToken ct = default);
}

/// <summary>OneDrive sync status</summary>
public record OneDriveSyncStatus
{
    public bool IsSynced { get; init; }
    public int FilesSync { get; init; }
    public long BytesSync { get; init; }
    public DateTime LastSyncTime { get; init; }
}
