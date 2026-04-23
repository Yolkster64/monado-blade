# Architecture Diagrams Reference

Complete set of architecture diagrams for Monado Blade system design.

## Table of Contents

1. [Component Diagram](#component-diagram)
2. [Deployment Diagram](#deployment-diagram)
3. [Boot Sequence Diagram](#boot-sequence-diagram)
4. [Update Sequence Diagram](#update-sequence-diagram)
5. [Boot State Machine](#boot-state-machine)
6. [Update State Machine](#update-state-machine)
7. [Data Flow Diagram](#data-flow-diagram)
8. [Plugin Architecture](#plugin-architecture)
9. [Security Layers](#security-layers)
10. [Service Dependencies](#service-dependencies)

---

## Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Monado Blade System                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                     Presentation Layer                    │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │   │
│  │  │  CLI Tool    │  │   GUI Impl   │  │  Web API     │   │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                     Service Layer                         │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │   │
│  │  │ Boot Service │  │ USB Service  │  │Update Service│   │   │
│  │  ├──────────────┤  ├──────────────┤  ├──────────────┤   │   │
│  │  │ Profile      │  │ Device Mgmt  │  │  Package     │   │   │
│  │  │ Management   │  │              │  │  Management  │   │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │   │
│  │                                                            │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │   │
│  │  │ Config Srv   │  │ Security Srv │  │ Logging Srv  │   │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                     Core Infrastructure                   │   │
│  │  ┌────────────────────────────────────────────────────┐  │   │
│  │  │         Dependency Injection Container             │  │   │
│  │  │  - Service Registration & Resolution              │  │   │
│  │  │  - Lifecycle Management (Singleton, Transient)    │  │   │
│  │  └────────────────────────────────────────────────────┘  │   │
│  │                                                            │   │
│  │  ┌──────────────────────────────────────────────────┐  │   │
│  │  │              Event Bus & Messaging               │  │   │
│  │  │  - Domain Event Publishing                       │  │   │
│  │  │  - Subscriber Management                         │  │   │
│  │  └──────────────────────────────────────────────────┘  │   │
│  │                                                            │   │
│  │  ┌──────────────────────────────────────────────────┐  │   │
│  │  │         State Machine Framework                  │  │   │
│  │  │  - Boot State Machine                            │  │   │
│  │  │  - Update State Machine                          │  │   │
│  │  │  - Custom State Machines                         │  │   │
│  │  └──────────────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                     Data Access Layer                     │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │   │
│  │  │ Partition    │  │ Configuration│  │  Backup      │   │   │
│  │  │ Manager      │  │  Storage     │  │  Manager     │   │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
     ┌────────────────────────────────────────┐
     │     External Systems & Devices         │
     │  ┌─────────────┐  ┌─────────────┐     │
     │  │ USB Devices │  │ Network     │     │
     │  │ (MTP, UMS)  │  │ Services    │     │
     │  └─────────────┘  └─────────────┘     │
     └────────────────────────────────────────┘
```

---

## Deployment Diagram

```
┌─────────────────────────────────────────────────────────┐
│               Development Environment                    │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Developer Workstation (Windows 10/11)             │  │
│  │  ┌──────────────────────────────────────────────┐  │  │
│  │  │  Monado Blade (Debug Build)                  │  │  │
│  │  │  - Full logging enabled                      │  │  │
│  │  │  - Profiling enabled                         │  │  │
│  │  │  - Plugin loading enabled                    │  │  │
│  │  └──────────────────────────────────────────────┘  │  │
│  │  USB Devices Connected for Testing                 │  │
│  └────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│               Staging Environment                        │
│  ┌────────────────────────────────────────────────────┐  │
│  │  VM/Server (Windows Server 2022)                  │  │
│  │  ┌──────────────────────────────────────────────┐  │  │
│  │  │  Monado Blade (Release Build)                │  │  │
│  │  │  - Reduced logging                           │  │  │
│  │  │  - Profiling disabled by default             │  │  │
│  │  │  - Plugin loading disabled                   │  │  │
│  │  └──────────────────────────────────────────────┘  │  │
│  │  USB Hub with Multiple Test Devices                │  │
│  │  Network Configuration for Update Testing          │  │
│  └────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│               Production Environment                     │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Target System (Windows 10/11)                     │  │
│  │  ┌──────────────────────────────────────────────┐  │  │
│  │  │  Monado Blade (Production Build)             │  │  │
│  │  │  - Minimal logging (errors only)             │  │  │
│  │  │  - Profiling disabled                        │  │  │
│  │  │  - Plugin loading controlled by policy       │  │  │
│  │  │  - Security enhanced                         │  │  │
│  │  └──────────────────────────────────────────────┘  │  │
│  │  USB Device(s) Connected                           │  │
│  │  Optional: Network for Update Management           │  │
│  └────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘

                    Update Flow:
Prod Update Server (Network)
         │
         ▼
┌──────────────────────────┐
│  Update Check Service    │ (Monado Blade)
└──────────────────────────┘
         │
         ├──► Download Update Package
         │
         ├──► Verify Signature
         │
         ├──► Create Backup
         │
         ├──► Apply Update
         │
         └──► Verify Installation
```

---

## Boot Sequence Diagram

```
Start
  │
  ▼
Monado Blade Application Launched
  │
  ├─► Initialize Services (DI Container)
  │   ├─► Logger Service
  │   ├─► Configuration Service
  │   ├─► USB Service
  │   ├─► Event Bus
  │   └─► Boot Service
  │
  ├─► Load Configuration
  │   ├─► Default settings
  │   ├─► User preferences
  │   └─► Profile settings
  │
  ├─► State: Initializing
  │
  ├─► State: Waiting for USB
  │   ├─► Enumerate USB devices
  │   ├─► Monitor for device connection
  │   └─► Timeout after 30 seconds
  │
  ├─► [On USB Device Detected]
  │
  ├─► State: USB Detected
  │   ├─► Identify device type
  │   ├─► Load appropriate driver
  │   └─► Establish communication
  │
  ├─► State: Application Started
  │   ├─► Mount device if necessary
  │   ├─► Verify device accessibility
  │   └─► Load application from device
  │
  ├─► State: Running
  │   ├─► Execute boot sequence steps
  │   ├─► Apply profile settings
  │   └─► Monitor device status
  │
  ├─► State: Complete
  │   ├─► Cleanup resources
  │   ├─► Generate boot report
  │   └─► Save boot state
  │
  ▼
Success / Error
  │
  ├─► On Success: Publish BootCompletedEvent
  │
  ├─► On Error: Publish BootFailedEvent
  │   ├─► Log error details
  │   ├─► Attempt recovery if configured
  │   └─► Rollback changes
  │
  ▼
End
```

---

## Boot State Machine

```
┌─────────────────────────────────────────────────────────────┐
│                    Boot State Machine                        │
└─────────────────────────────────────────────────────────────┘

                      Uninitialized
                          │
                          ▼
                   ┌──────────────┐
                   │ Initializing │  ◄──── Start Boot
                   └──────────────┘
                          │
            ┌─────────────┴─────────────┐
            │                           │
            ▼                           ▼
    ┌──────────────┐        ┌───────────────┐
    │  WaitingForUSB│       │    Failed     │ (on error)
    └──────────────┘        └───────────────┘
            │
            │ (USB Device Found)
            ▼
    ┌──────────────┐
    │ USBDetected  │
    └──────────────┘
            │
            │ (Device Ready)
            ▼
    ┌────────────────────┐
    │ ApplicationStarted │
    └────────────────────┘
            │
            │ (App Running)
            ▼
    ┌──────────────┐
    │   Running    │
    └──────────────┘
            │
    ┌───────┴───────┐
    │               │
    ▼               ▼
┌──────────┐   ┌───────────┐
│ Complete │   │  Failed   │
└──────────┘   └───────────┘

Valid Transitions:
Initializing → WaitingForUSB
Initializing → Failed
WaitingForUSB → USBDetected
WaitingForUSB → Failed
USBDetected → ApplicationStarted
USBDetected → Failed
ApplicationStarted → Running
ApplicationStarted → Failed
Running → Complete
Running → Failed
Failed → WaitingForUSB (if auto-retry)
```

---

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                   Data Flow in System                        │
└─────────────────────────────────────────────────────────────┘

Configuration Data Flow:
  config.json (File)
    │
    ├──► ConfigurationService (Loads & Caches)
    │       │
    │       ├──► In-Memory Cache
    │       │
    │       └──► Service Registration Updates
    │
    ├──► Environment Variables (Override)
    │
    └──► Registry Settings (Windows)

USB Device Data Flow:
  Physical USB Device
    │
    ├──► USB Service (Detection)
    │       │
    │       └──► IUSBDevice Interface
    │           │
    │           ├──► Protocol Handler (MTP/UMS/Custom)
    │           │
    │           └──► Boot Service (Processing)

Update Data Flow:
  Update Server (Network)
    │
    ├──► DownloadUpdate
    │       │
    │       ├──► Verify Signature
    │       │
    │       └──► Store Locally
    │
    ├──► ApplyUpdate
    │       │
    │       ├──► Create Backup
    │       │
    │       ├──► Apply Changes
    │       │
    │       └──► Verify Installation

Event Flow:
  Service (Action)
    │
    ├──► Create DomainEvent
    │       │
    │       └──► Publish via EventBus
    │           │
    │           ├──► Handler 1 (Subscriber)
    │           │
    │           ├──► Handler 2 (Subscriber)
    │           │
    │           └──► Correlation ID Tracking

Logging Data Flow:
  All Services (Actions)
    │
    ├──► ILogger Interface
    │       │
    │       ├──► Structured Logging
    │       │       │
    │       │       ├──► Console Sink
    │       │       │
    │       │       └──► File Sink
    │       │
    │       └──► Correlation ID Enrichment
```

---

## Plugin Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Plugin System                             │
└─────────────────────────────────────────────────────────────┘

Plugin Discovery:
  Plugins/ Directory
    │
    ├─► manifest.json (Metadata)
    │
    ├─► [PluginName].dll (Assembly)
    │
    └─► signature.sig (Digital Signature)

Plugin Loading:
  Plugin Manager
    │
    ├─► Validate Manifest
    │
    ├─► Verify Signature
    │
    ├─► Check Permissions
    │
    ├─► Load Assembly
    │       │
    │       └─► Create IPlugin Instance
    │
    ├─► Initialize Plugin
    │       │
    │       └─► Inject Services
    │
    └─► Start Plugin

Plugin Execution:
  ┌─────────────────────────────────┐
  │    Plugin Sandbox (Isolated)    │
  │                                 │
  │  ┌──────────────────────────┐   │
  │  │   Plugin Instance        │   │
  │  │                          │   │
  │  │  ┌──────────────────┐   │   │
  │  │  │ Event Handlers   │   │   │
  │  │  │ - Subscribe      │   │   │
  │  │  │ - React to Event │   │   │
  │  │  └──────────────────┘   │   │
  │  │                          │   │
  │  │  ┌──────────────────┐   │   │
  │  │  │  Service Access  │   │   │
  │  │  │  (Via Interface) │   │   │
  │  │  └──────────────────┘   │   │
  │  │                          │   │
  │  └──────────────────────────┘   │
  │          ▲                      │
  │          │ (Permissions)        │
  │          │                      │
  │  Security Boundary              │
  │          │                      │
  └──────────┼──────────────────────┘
             │
             ▼
      Core Services
```

---

## Security Layers

```
┌──────────────────────────────────────────────────────────────┐
│                  Security Architecture                        │
└──────────────────────────────────────────────────────────────┘

Layer 1: Perimeter Security
├─► Authentication (Windows Auth / Tokens)
├─► Basic Input Validation
└─► API Endpoint Protection

         │
         ▼

Layer 2: Authorization
├─► Role-Based Access Control (RBAC)
├─► Permission Verification
└─► Operation-Level Authorization

         │
         ▼

Layer 3: Data Protection
├─► Cryptographic Signing (RSA-2048)
├─► Integrity Verification (HMAC-SHA256)
├─► Secure Key Management
└─► Configuration Encryption

         │
         ▼

Layer 4: Application Logic
├─► Input Validation & Sanitization
├─► Business Rule Enforcement
├─► State Machine Validation
└─► Operation Atomicity

         │
         ▼

Layer 5: Audit & Monitoring
├─► Security Event Logging
├─► Anomaly Detection
├─► Rate Limiting
└─► Access Logging

         │
         ▼

Layer 6: System Protection
├─► Process Isolation
├─► Plugin Sandboxing
├─► Resource Limits
└─► OS-Level Protections
```

---

## Service Dependencies

```
Core Services Dependency Graph:

BootService
├─► ILogger
├─► IUSBService
├─► IProfileService
├─► IEventBus
└─► IConfiguration

UpdateService
├─► ILogger
├─► IPackageService
├─► IBackupService
├─► IEventBus
└─► IConfiguration

USBService
├─► ILogger
├─► IEventBus
└─► IUSBDeviceFactory

ProfileService
├─► ILogger
├─► IProfileRegistry
├─► IProfileValidator
├─► ICache<T>
└─► IEventBus

ConfigurationService
├─► ILogger
└─► IConfigurationProvider

BackupService
├─► ILogger
├─► IPartitionManager
└─► IFileSystem

SecurityService
├─► ILogger
├─► IAuthenticationProvider
├─► IEncryptionService
└─► IAuditService

LoggingService
├─► StructuredLoggingEngine
├─► FileSink
└─► ConsoleSink
```

---

## Sequence Diagrams

### Update Sequence

```
User                Monado           Update           Device
 │                   Blade           Service
 │                    │                 │               │
 ├──Check Updates──►  │                 │               │
 │                    ├──Query────────►  │               │
 │                    │                 ├──Check────────►│
 │                    │◄────Available───┤               │
 │◄──Updates Found──  │                 │               │
 │                    │                 │               │
 ├──Apply Update──►   │                 │               │
 │                    ├──Validate──────►  │               │
 │                    │◄──Valid────────┤               │
 │                    │                 │               │
 │                    ├──Download──────►  │               │
 │                    │◄──Package──────┤               │
 │                    │                 │               │
 │                    ├──Backup────────►  │               │
 │                    │◄──Backed Up────┤               │
 │                    │                 │               │
 │                    ├──Apply────────►  │               │
 │                    │                 ├──Write───────►│
 │                    │                 │◄──Ack────────┤
 │                    │◄──Applied──────┤               │
 │                    │                 │               │
 │                    ├──Verify───────►  │               │
 │                    │                 ├──Verify──────►│
 │                    │                 │◄──Verified───┤
 │                    │◄──Verified─────┤               │
 │                    │                 │               │
 │◄──Success────────  │                 │               │
```

These diagrams provide visual references for system architecture, data flows, state transitions, and component relationships. They complement the ADRs and implementation guides for comprehensive understanding of the Monado Blade system.
