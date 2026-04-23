using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using MonadoBlade.Security;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace MonadoBlade.Tests.Unit
{
    /// <summary>
    /// Unit tests for Security Hardening Stream (v3.2.0)
    /// Tests for Secure Enclave Operations and Zero-Trust Boot Verification
    /// </summary>
    public class SecurityHardeningTests
    {
        private readonly Mock<ILogger<SecureEnclaveManager>> _loggerEnclaveManager;
        private readonly Mock<ILogger<EncryptionKeySealing>> _loggerKeySealing;
        private readonly Mock<ILogger<SecureBootstrap>> _loggerBootstrap;
        private readonly Mock<ILogger<SideChannelDefense>> _loggerSideChannel;
        private readonly Mock<ILogger<BootComponentVerifier>> _loggerBootVerifier;
        private readonly Mock<ILogger<TrustedComputingBase>> _loggerTCB;
        private readonly Mock<ILogger<AttestationReporter>> _loggerAttestation;

        public SecurityHardeningTests()
        {
            _loggerEnclaveManager = new Mock<ILogger<SecureEnclaveManager>>();
            _loggerKeySealing = new Mock<ILogger<EncryptionKeySealing>>();
            _loggerBootstrap = new Mock<ILogger<SecureBootstrap>>();
            _loggerSideChannel = new Mock<ILogger<SideChannelDefense>>();
            _loggerBootVerifier = new Mock<ILogger<BootComponentVerifier>>();
            _loggerTCB = new Mock<ILogger<TrustedComputingBase>>();
            _loggerAttestation = new Mock<ILogger<AttestationReporter>>();
        }

        #region SecureEnclaveManager Tests

        [Fact]
        public void SecureEnclaveManager_Initialize_ShouldDetectEnclaveType()
        {
            // Arrange
            var manager = new SecureEnclaveManager(_loggerEnclaveManager.Object);

            // Act
            bool result = manager.Initialize();

            // Assert
            Assert.True(result);
            Assert.NotEqual(SecureEnclaveManager.EnclaveType.None, manager.GetEnclaveType());
        }

        [Fact]
        public void SecureEnclaveManager_LoadOperation_ShouldSucceedAfterInitialization()
        {
            // Arrange
            var manager = new SecureEnclaveManager(_loggerEnclaveManager.Object);
            manager.Initialize();
            byte[] operationCode = new byte[256];
            new Random().NextBytes(operationCode);

            // Act
            bool result = manager.LoadOperationIntoEnclave("TestOperation", operationCode);

            // Assert
            Assert.True(result);
            Assert.Contains("TestOperation", manager.GetLoadedOperations());
        }

        [Fact]
        public void SecureEnclaveManager_LoadOperation_ShouldFailBeforeInitialization()
        {
            // Arrange
            var manager = new SecureEnclaveManager(_loggerEnclaveManager.Object);
            byte[] operationCode = new byte[256];

            // Act
            bool result = manager.LoadOperationIntoEnclave("TestOperation", operationCode);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SecureEnclaveManager_IsEnclaveAvailable_ShouldReturnCorrectStatus()
        {
            // Arrange
            var manager = new SecureEnclaveManager(_loggerEnclaveManager.Object);

            // Act
            bool availableBefore = manager.IsEnclaveAvailable();
            manager.Initialize();
            bool availableAfter = manager.IsEnclaveAvailable();

            // Assert
            Assert.False(availableBefore);
            Assert.True(availableAfter);
        }

        #endregion

        #region EncryptionKeySealing Tests

        [Fact]
        public void EncryptionKeySealing_SealKey_ShouldSucceed()
        {
            // Arrange
            var manager = new SecureEnclaveManager(_loggerEnclaveManager.Object);
            manager.Initialize();
            var sealing = new EncryptionKeySealing(_loggerKeySealing.Object, manager);

            byte[] keyMaterial = new byte[32];
            new Random().NextBytes(keyMaterial);

            // Act
            bool result = sealing.SealKey("TestKey", keyMaterial);

            // Assert
            Assert.True(result);
            Assert.Contains("TestKey", sealing.GetSealedKeyIds());
        }

        [Fact]
        public void EncryptionKeySealing_UnsealKey_ShouldReturnKeyAfterSealing()
        {
            // Arrange
            var manager = new SecureEnclaveManager(_loggerEnclaveManager.Object);
            manager.Initialize();
            var sealing = new EncryptionKeySealing(_loggerKeySealing.Object, manager);

            byte[] keyMaterial = new byte[32];
            new Random().NextBytes(keyMaterial);
            sealing.SealKey("TestKey", keyMaterial);

            // Act
            byte[] unsealed = sealing.UnsealKey("TestKey");

            // Assert
            Assert.NotNull(unsealed);
            Assert.NotEmpty(unsealed);
        }

        [Fact]
        public void EncryptionKeySealing_RotateKey_ShouldUpdateRotationDate()
        {
            // Arrange
            var manager = new SecureEnclaveManager(_loggerEnclaveManager.Object);
            manager.Initialize();
            var sealing = new EncryptionKeySealing(_loggerKeySealing.Object, manager);

            byte[] keyMaterial = new byte[32];
            new Random().NextBytes(keyMaterial);
            sealing.SealKey("TestKey", keyMaterial);

            byte[] newKeyMaterial = new byte[32];
            new Random().NextBytes(newKeyMaterial);

            // Act
            bool result = sealing.RotateKey("TestKey", newKeyMaterial);

            // Assert
            Assert.True(result);
            Assert.False(sealing.IsKeyRotationRequired("TestKey"));
        }

        [Fact]
        public void EncryptionKeySealing_GetKeyMetadata_ShouldReturnMetadata()
        {
            // Arrange
            var manager = new SecureEnclaveManager(_loggerEnclaveManager.Object);
            manager.Initialize();
            var sealing = new EncryptionKeySealing(_loggerKeySealing.Object, manager);

            byte[] keyMaterial = new byte[32];
            new Random().NextBytes(keyMaterial);
            sealing.SealKey("TestKey", keyMaterial);

            // Act
            var metadata = sealing.GetKeyMetadata("TestKey");

            // Assert
            Assert.NotNull(metadata);
            Assert.Contains("CreatedAt", metadata.Keys);
            Assert.Contains("AccessCount", metadata.Keys);
        }

        #endregion

        #region SecureBootstrap Tests

        [Fact]
        public void SecureBootstrap_VerifyBootloader_ShouldSucceed()
        {
            // Arrange
            var manager = new SecureEnclaveManager(_loggerEnclaveManager.Object);
            manager.Initialize();
            var bootstrap = new SecureBootstrap(_loggerBootstrap.Object, manager);

            byte[] bootloaderData = new byte[1024];
            new Random().NextBytes(bootloaderData);

            // Act
            bool result = bootstrap.VerifyBootloader(bootloaderData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SecureBootstrap_VerifyBootChain_ShouldSucceed()
        {
            // Arrange
            var manager = new SecureEnclaveManager(_loggerEnclaveManager.Object);
            manager.Initialize();
            var bootstrap = new SecureBootstrap(_loggerBootstrap.Object, manager);

            byte[] bootloaderData = new byte[1024];
            byte[] kernelData = new byte[2048];
            byte[] initramfsData = new byte[512];
            new Random().NextBytes(bootloaderData);
            new Random().NextBytes(kernelData);
            new Random().NextBytes(initramfsData);

            // Act
            bool bootloaderOk = bootstrap.VerifyBootloader(bootloaderData);
            bool kernelOk = bootstrap.VerifyKernel(kernelData);
            bool initramfsOk = bootstrap.VerifyInitramfs(initramfsData);
            bool completionOk = bootstrap.CompleteBootVerification();

            // Assert
            Assert.True(bootloaderOk);
            Assert.True(kernelOk);
            Assert.True(initramfsOk);
            Assert.True(completionOk);
            Assert.True(bootstrap.IsBootVerified());
        }

        [Fact]
        public void SecureBootstrap_GetBootIntegrityReport_ShouldContainAllComponents()
        {
            // Arrange
            var manager = new SecureEnclaveManager(_loggerEnclaveManager.Object);
            manager.Initialize();
            var bootstrap = new SecureBootstrap(_loggerBootstrap.Object, manager);

            byte[] bootloaderData = new byte[1024];
            byte[] kernelData = new byte[2048];
            byte[] initramfsData = new byte[512];
            new Random().NextBytes(bootloaderData);
            new Random().NextBytes(kernelData);
            new Random().NextBytes(initramfsData);

            bootstrap.VerifyBootloader(bootloaderData);
            bootstrap.VerifyKernel(kernelData);
            bootstrap.VerifyInitramfs(initramfsData);
            bootstrap.CompleteBootVerification();

            // Act
            var report = bootstrap.GetBootIntegrityReport();

            // Assert
            Assert.NotNull(report);
            Assert.True((bool)report["IsBootVerified"]);
            Assert.NotNull(report["BootloaderHashHex"]);
            Assert.NotNull(report["CumulativeBootChainHashHex"]);
        }

        #endregion

        #region SideChannelDefense Tests

        [Fact]
        public void SideChannelDefense_Initialize_ShouldEnableAllMitigations()
        {
            // Arrange
            var defense = new SideChannelDefense(_loggerSideChannel.Object);

            // Act
            defense.Initialize();
            var status = defense.GetMitigationStatus();

            // Assert
            Assert.NotNull(status);
            Assert.True((bool)status["SpectreMitigationEnabled"]);
            Assert.True((bool)status["MeltdownMitigationEnabled"]);
            Assert.True((bool)status["TimingDefenseEnabled"]);
            Assert.True((bool)status["CacheDefenseEnabled"]);
        }

        [Fact]
        public void SideChannelDefense_ConstantTimeComparison_ShouldSucceed()
        {
            // Arrange
            var defense = new SideChannelDefense(_loggerSideChannel.Object);
            byte[] a = new byte[32];
            byte[] b = new byte[32];
            new Random().NextBytes(a);
            Array.Copy(a, b, a.Length);

            // Act
            bool result = defense.ConstantTimeComparison(a, b);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SideChannelDefense_ConstantTimeComparison_ShouldFailOnDifference()
        {
            // Arrange
            var defense = new SideChannelDefense(_loggerSideChannel.Object);
            byte[] a = new byte[32];
            byte[] b = new byte[32];
            new Random().NextBytes(a);
            new Random().NextBytes(b);

            // Act
            bool result = defense.ConstantTimeComparison(a, b);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SideChannelDefense_ExecuteWithTimingDefense_ShouldRecordMetric()
        {
            // Arrange
            var defense = new SideChannelDefense(_loggerSideChannel.Object);
            defense.Initialize();

            // Act
            for (int i = 0; i < 5; i++)
            {
                defense.ExecuteWithTimingDefense(() =>
                {
                    System.Threading.Thread.Sleep(10);
                    return 42;
                }, "TestOperation");
            }

            var metrics = defense.GetTimingMetrics("TestOperation");

            // Assert
            Assert.NotNull(metrics);
            Assert.Equal(5L, (long)metrics["TotalSamples"]);
        }

        #endregion

        #region BootComponentVerifier Tests

        [Fact]
        public void BootComponentVerifier_VerifyBootloader_ShouldSucceed()
        {
            // Arrange
            var verifier = new BootComponentVerifier(_loggerBootVerifier.Object);
            byte[] bootloaderData = new byte[1024];
            new Random().NextBytes(bootloaderData);

            // Act
            bool result = verifier.VerifyBootloaderComponent("BOOTX64.EFI", bootloaderData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void BootComponentVerifier_VerifyKernelModule_ShouldSucceed()
        {
            // Arrange
            var verifier = new BootComponentVerifier(_loggerBootVerifier.Object);
            byte[] moduleData = new byte[2048];
            new Random().NextBytes(moduleData);

            // Act
            bool result = verifier.VerifyKernelModule("kernel.ko", moduleData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void BootComponentVerifier_VerifyDriver_ShouldSucceed()
        {
            // Arrange
            var verifier = new BootComponentVerifier(_loggerBootVerifier.Object);
            byte[] driverData = new byte[4096];
            new Random().NextBytes(driverData);

            // Act
            bool result = verifier.VerifyDriver("network.sys", driverData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void BootComponentVerifier_GetAuditLog_ShouldContainVerifications()
        {
            // Arrange
            var verifier = new BootComponentVerifier(_loggerBootVerifier.Object);
            byte[] data1 = new byte[1024];
            byte[] data2 = new byte[2048];
            new Random().NextBytes(data1);
            new Random().NextBytes(data2);

            verifier.VerifyBootloaderComponent("BOOTX64.EFI", data1);
            verifier.VerifyKernelModule("kernel.ko", data2);

            // Act
            var auditLog = verifier.GetAuditLog();

            // Assert
            Assert.NotNull(auditLog);
            Assert.Equal(2, auditLog.Count);
        }

        [Fact]
        public void BootComponentVerifier_GetVerificationStatistics_ShouldBeAccurate()
        {
            // Arrange
            var verifier = new BootComponentVerifier(_loggerBootVerifier.Object);
            byte[] data = new byte[1024];
            new Random().NextBytes(data);

            verifier.VerifyBootloaderComponent("BOOTX64.EFI", data);
            verifier.VerifyBootloaderComponent("BOOTX86.EFI", data);

            // Act
            var stats = verifier.GetVerificationStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(2, (int)stats["TotalComponentsVerified"]);
        }

        #endregion

        #region ZeroTrustManifest Tests

        [Fact]
        public void ZeroTrustManifest_RegisterComponent_ShouldSucceed()
        {
            // Arrange
            var manifest = new ZeroTrustManifest(_loggerBootVerifier.Object);
            byte[] componentHash = new byte[32];
            new Random().NextBytes(componentHash);

            // Act
            bool result = manifest.RegisterComponent(
                "kernel",
                "Linux Kernel",
                "Kernel",
                "5.15.0",
                componentHash
            );

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ZeroTrustManifest_UpdateComponent_ShouldSucceed()
        {
            // Arrange
            var manifest = new ZeroTrustManifest(_loggerBootVerifier.Object);
            byte[] componentHash1 = new byte[32];
            byte[] componentHash2 = new byte[32];
            new Random().NextBytes(componentHash1);
            new Random().NextBytes(componentHash2);

            manifest.RegisterComponent(
                "kernel",
                "Linux Kernel",
                "Kernel",
                "5.15.0",
                componentHash1
            );

            // Act
            bool result = manifest.UpdateComponent(
                "kernel",
                "5.16.0",
                componentHash2
            );

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ZeroTrustManifest_IsVersionRollback_ShouldDetectDowngrade()
        {
            // Arrange
            var manifest = new ZeroTrustManifest(_loggerBootVerifier.Object);
            byte[] componentHash = new byte[32];
            new Random().NextBytes(componentHash);

            manifest.RegisterComponent(
                "kernel",
                "Linux Kernel",
                "Kernel",
                "5.15.0",
                componentHash
            );

            // Act
            bool isRollback = manifest.IsVersionRollback("kernel", "5.14.0");

            // Assert
            Assert.True(isRollback);
        }

        [Fact]
        public void ZeroTrustManifest_VerifyManifestIntegrity_ShouldSucceed()
        {
            // Arrange
            var manifest = new ZeroTrustManifest(_loggerBootVerifier.Object);
            byte[] componentHash = new byte[32];
            new Random().NextBytes(componentHash);

            manifest.RegisterComponent(
                "kernel",
                "Linux Kernel",
                "Kernel",
                "5.15.0",
                componentHash
            );

            // Act
            bool result = manifest.VerifyManifestIntegrity();

            // Assert
            Assert.True(result);
        }

        #endregion

        #region AttestationReporter Tests

        [Fact]
        public void AttestationReporter_GenerateBootChainAttestation_ShouldSucceed()
        {
            // Arrange
            var reporter = new AttestationReporter(_loggerAttestation.Object);
            byte[] bootloaderHash = new byte[32];
            byte[] kernelHash = new byte[32];
            byte[] initramfsHash = new byte[32];
            byte[] cumulativeHash = new byte[32];
            new Random().NextBytes(bootloaderHash);
            new Random().NextBytes(kernelHash);
            new Random().NextBytes(initramfsHash);
            new Random().NextBytes(cumulativeHash);

            // Act
            string proof = reporter.GenerateBootChainAttestation(
                bootloaderHash,
                kernelHash,
                initramfsHash,
                cumulativeHash
            );

            // Assert
            Assert.NotNull(proof);
            Assert.NotEmpty(proof);
        }

        [Fact]
        public void AttestationReporter_ReportToAuditSystem_ShouldSucceed()
        {
            // Arrange
            var reporter = new AttestationReporter(_loggerAttestation.Object);
            byte[] bootloaderHash = new byte[32];
            byte[] kernelHash = new byte[32];
            byte[] initramfsHash = new byte[32];
            byte[] cumulativeHash = new byte[32];
            new Random().NextBytes(bootloaderHash);
            new Random().NextBytes(kernelHash);
            new Random().NextBytes(initramfsHash);
            new Random().NextBytes(cumulativeHash);

            string proof = reporter.GenerateBootChainAttestation(
                bootloaderHash,
                kernelHash,
                initramfsHash,
                cumulativeHash
            );

            // Act
            bool result = reporter.ReportToAuditSystem(proof);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AttestationReporter_GenerateFIPS140_2Report_ShouldSucceed()
        {
            // Arrange
            var reporter = new AttestationReporter(_loggerAttestation.Object);
            var attestationData = new Dictionary<string, object>
            {
                { "SystemName", "MonadoBlade" },
                { "Version", "3.2.0" }
            };

            // Act
            string report = reporter.GenerateFIPS140_2ComplianceReport(attestationData);

            // Assert
            Assert.NotNull(report);
            Assert.Contains("FIPS 140-2", report);
            Assert.Contains("COMPLIANT", report);
        }

        [Fact]
        public void AttestationReporter_GenerateDISAStigReport_ShouldSucceed()
        {
            // Arrange
            var reporter = new AttestationReporter(_loggerAttestation.Object);
            var systemData = new Dictionary<string, object>
            {
                { "SystemName", "MonadoBlade" },
                { "OSVersion", "Windows 11" }
            };

            // Act
            string report = reporter.GenerateDISAStigComplianceReport(systemData);

            // Assert
            Assert.NotNull(report);
            Assert.Contains("DISA STIG", report);
            Assert.Contains("COMPLIANT", report);
        }

        #endregion

        #region TrustedComputingBase Tests

        [Fact]
        public void TrustedComputingBase_EstablishBaseline_ShouldSucceed()
        {
            // Arrange
            var tcb = new TrustedComputingBase(_loggerTCB.Object);
            var components = new Dictionary<string, (byte[], string)>
            {
                { "bootloader", (new byte[1024], "bootloader") },
                { "kernel", (new byte[2048], "kernel") }
            };

            // Act
            bool result = tcb.EstablishBaseline(components);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TrustedComputingBase_MeasureTCB_ShouldSucceed()
        {
            // Arrange
            var tcb = new TrustedComputingBase(_loggerTCB.Object);
            var components = new Dictionary<string, (byte[], string)>
            {
                { "bootloader", (new byte[1024], "bootloader") },
                { "kernel", (new byte[2048], "kernel") }
            };

            tcb.EstablishBaseline(components);

            // Act
            bool result = tcb.MeasureTCB(components);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TrustedComputingBase_GetTCBState_ShouldReturnValidState()
        {
            // Arrange
            var tcb = new TrustedComputingBase(_loggerTCB.Object);
            var components = new Dictionary<string, (byte[], string)>
            {
                { "bootloader", (new byte[1024], "bootloader") },
                { "kernel", (new byte[2048], "kernel") }
            };

            tcb.EstablishBaseline(components);
            tcb.MeasureTCB(components);

            // Act
            var state = tcb.GetTCBState();

            // Assert
            Assert.NotNull(state);
            Assert.True((bool)state["BaselineEstablished"]);
            Assert.NotNull(state["Measurements"]);
        }

        [Fact]
        public void TrustedComputingBase_DetectModification_ShouldIdentifyChanges()
        {
            // Arrange
            var tcb = new TrustedComputingBase(_loggerTCB.Object);
            var baselineComponents = new Dictionary<string, (byte[], string)>
            {
                { "bootloader", (new byte[1024], "bootloader") },
                { "kernel", (new byte[2048], "kernel") }
            };

            tcb.EstablishBaseline(baselineComponents);

            var modifiedComponents = new Dictionary<string, (byte[], string)>
            {
                { "bootloader", (new byte[1024], "bootloader") },
                { "kernel", (new byte[2048], "kernel") }
            };

            // Modify the kernel component
            modifiedComponents["kernel"].Item1[0] = 255;

            // Act
            bool result = tcb.MeasureTCB(modifiedComponents);
            var anomalies = tcb.GetDetectedAnomalies();

            // Assert
            Assert.False(result);
            Assert.NotEmpty(anomalies);
        }

        [Fact]
        public void TrustedComputingBase_GetTCBStatistics_ShouldReturnValidStats()
        {
            // Arrange
            var tcb = new TrustedComputingBase(_loggerTCB.Object);
            var components = new Dictionary<string, (byte[], string)>
            {
                { "bootloader", (new byte[1024], "bootloader") },
                { "kernel", (new byte[2048], "kernel") }
            };

            tcb.EstablishBaseline(components);

            // Act
            var stats = tcb.GetTCBStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(2, (int)stats["TotalComponents"]);
            Assert.Equal("HEALTHY", stats["TCBHealthStatus"]);
        }

        #endregion
    }
}
