using System;
using MonadoBlade.Core.Builders;
using Xunit;

namespace MonadoBlade.Tests.Unit.Builders
{
    /// <summary>
    /// Unit tests for fluent builder pattern implementation.
    /// Tests all three fluent builders with various configurations.
    /// </summary>
    public class FluentProfileBuilderTests
    {
        [Fact]
        public void WithName_SetsProfileName()
        {
            var builder = new FluentProfileBuilder().WithName("TestProfile");
            var config = builder.Build();
            Assert.Equal("TestProfile", config.Name);
        }

        [Fact]
        public void WithGPU_EnablesGPU()
        {
            var builder = new FluentProfileBuilder().WithName("Test").WithGPU();
            var config = builder.Build();
            Assert.True(config.GPUEnabled);
        }

        [Fact]
        public void WithSecureMode_EnablesSecureBoot()
        {
            var builder = new FluentProfileBuilder().WithName("Test").WithSecureMode();
            var config = builder.Build();
            Assert.True(config.SecureModeEnabled);
        }

        [Fact]
        public void WithDualBoot_EnablesDualBoot()
        {
            var builder = new FluentProfileBuilder().WithName("Test").WithDualBoot();
            var config = builder.Build();
            Assert.True(config.DualBootEnabled);
        }

        [Fact]
        public void WithCPUCores_SetsCores()
        {
            var builder = new FluentProfileBuilder().WithName("Test").WithCPUCores(8);
            var config = builder.Build();
            Assert.Equal(8, config.CPUCores);
        }

        [Fact]
        public void WithRAM_SetsMemory()
        {
            var builder = new FluentProfileBuilder().WithName("Test").WithRAM(16384);
            var config = builder.Build();
            Assert.Equal(16384, config.RAMMB);
        }

        [Fact]
        public void WithoutGPU_DisablesGPU()
        {
            var builder = new FluentProfileBuilder().WithName("Test").WithGPU().WithoutGPU();
            var config = builder.Build();
            Assert.False(config.GPUEnabled);
        }

        [Fact]
        public void ChainedBuilding_AppliesAllSettings()
        {
            var config = new FluentProfileBuilder()
                .WithName("CompleteProfile")
                .WithGPU()
                .WithSecureMode()
                .WithDualBoot()
                .WithCPUCores(16)
                .WithRAM(32768)
                .WithBootDevice("C:")
                .Build();

            Assert.Equal("CompleteProfile", config.Name);
            Assert.True(config.GPUEnabled);
            Assert.True(config.SecureModeEnabled);
            Assert.True(config.DualBootEnabled);
            Assert.Equal(16, config.CPUCores);
            Assert.Equal(32768, config.RAMMB);
        }

        [Fact]
        public void Build_WithoutName_ThrowsException()
        {
            var builder = new FluentProfileBuilder();
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Fact]
        public void WithSetting_AddsCustomSetting()
        {
            var config = new FluentProfileBuilder()
                .WithName("Test")
                .WithSetting("gpu-driver", "nvidia")
                .Build();

            Assert.Contains("gpu-driver", config.CustomSettings.Keys);
            Assert.Equal("nvidia", config.CustomSettings["gpu-driver"]);
        }
    }

    /// <summary>
    /// Unit tests for fluent USB builder.
    /// </summary>
    public class FluentUSBBuilderTests
    {
        [Fact]
        public void ForDevice_SetsDeviceName()
        {
            var builder = new FluentUSBBuilder().ForDevice("MyPC").WithProfile("Profile1").ToUSBDrive("E:");
            var config = builder.Build();
            Assert.Equal("MyPC", config.DeviceName);
        }

        [Fact]
        public void WithProfile_SetsProfile()
        {
            var builder = new FluentUSBBuilder().ForDevice("PC").WithProfile("Gaming").ToUSBDrive("E:");
            var config = builder.Build();
            Assert.Equal("Gaming", config.ProfileName);
        }

        [Fact]
        public void BuildInBackground_SetBackground()
        {
            var builder = new FluentUSBBuilder().ForDevice("PC").WithProfile("Profile").ToUSBDrive("E:").BuildInBackground();
            var config = builder.Build();
            Assert.True(config.BuildInBackground);
        }

        [Fact]
        public void BuildInForeground_SetsForeground()
        {
            var builder = new FluentUSBBuilder().ForDevice("PC").WithProfile("Profile").ToUSBDrive("E:").BuildInForeground();
            var config = builder.Build();
            Assert.False(config.BuildInBackground);
        }

        [Fact]
        public void WithTimeout_SetsTimeout()
        {
            var builder = new FluentUSBBuilder().ForDevice("PC").WithProfile("Profile").ToUSBDrive("E:").WithTimeout(600);
            var config = builder.Build();
            Assert.Equal(600, config.TimeoutSeconds);
        }

        [Fact]
        public void VerifyIntegrity_EnablesVerification()
        {
            var builder = new FluentUSBBuilder().ForDevice("PC").WithProfile("Profile").ToUSBDrive("E:").VerifyIntegrity();
            var config = builder.Build();
            Assert.True(config.VerifyIntegrity);
        }

        [Fact]
        public void Build_WithoutRequiredFields_ThrowsException()
        {
            var builder = new FluentUSBBuilder();
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Fact]
        public void ChainedBuilding_CompleteConfig()
        {
            var config = new FluentUSBBuilder()
                .ForDevice("MyDesktop")
                .WithProfile("Developer")
                .ToUSBDrive("F:")
                .BuildInBackground()
                .WithTimeout(900)
                .VerifyIntegrity()
                .WithVerboseLogging()
                .Build();

            Assert.Equal("MyDesktop", config.DeviceName);
            Assert.Equal("Developer", config.ProfileName);
            Assert.Equal("F:", config.USBDrivePath);
            Assert.True(config.BuildInBackground);
            Assert.Equal(900, config.TimeoutSeconds);
        }
    }

    /// <summary>
    /// Unit tests for fluent boot config builder.
    /// </summary>
    public class FluentBootConfigBuilderTests
    {
        [Fact]
        public void SetBootTimeout_SetsTimeout()
        {
            var builder = new FluentBootConfigBuilder().SetBootTimeout(30).SetDefaultOS("Monado");
            var config = builder.Build();
            Assert.Equal(30, config.BootTimeout);
        }

        [Fact]
        public void EnableSecureBoot_EnablesBoot()
        {
            var builder = new FluentBootConfigBuilder().EnableSecureBoot().SetDefaultOS("Monado");
            var config = builder.Build();
            Assert.True(config.SecureBootEnabled);
        }

        [Fact]
        public void DisableSecureBoot_DisablesBoot()
        {
            var builder = new FluentBootConfigBuilder().EnableSecureBoot().DisableSecureBoot().SetDefaultOS("Monado");
            var config = builder.Build();
            Assert.False(config.SecureBootEnabled);
        }

        [Fact]
        public void EnableTPM2_EnablesTPM()
        {
            var builder = new FluentBootConfigBuilder().EnableTPM2().SetDefaultOS("Monado");
            var config = builder.Build();
            Assert.True(config.TPM2Enabled);
        }

        [Fact]
        public void EnableUEFI_EnablesUEFI()
        {
            var builder = new FluentBootConfigBuilder().EnableUEFI().SetDefaultOS("Monado");
            var config = builder.Build();
            Assert.True(config.UEFIEnabled);
        }

        [Fact]
        public void SetDefaultOS_SetsOS()
        {
            var builder = new FluentBootConfigBuilder().SetDefaultOS("Windows");
            var config = builder.Build();
            Assert.Equal("Windows", config.DefaultOS);
        }

        [Fact]
        public void Build_WithoutDefaultOS_ThrowsException()
        {
            var builder = new FluentBootConfigBuilder();
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Fact]
        public void ChainedBuilding_CompleteBootConfig()
        {
            var config = new FluentBootConfigBuilder()
                .SetBootTimeout(45)
                .EnableSecureBoot()
                .SetDefaultOS("Monado")
                .EnableUEFI()
                .EnableTPM2()
                .SetBootOrder("USB,HDD,CD")
                .FastBoot()
                .VerboseMode()
                .Build();

            Assert.Equal(45, config.BootTimeout);
            Assert.True(config.SecureBootEnabled);
            Assert.Equal("Monado", config.DefaultOS);
            Assert.True(config.UEFIEnabled);
            Assert.True(config.TPM2Enabled);
        }
    }
}
