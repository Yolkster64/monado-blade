# Monado Runtime Setup Checklist

This document is the operational setup spine for Monado Blade. It defines the target runtime, host requirements, install paths, service/runtime activation, smoke testing, troubleshooting, known limitations, and upstream references.

## 1. Supported target

Primary target:

- Host OS: Linux desktop/workstation first, with Ubuntu/Debian as the initial scripted path.
- Runtime layer: Monado as the active OpenXR runtime.
- Application target: native OpenXR applications first, then compatibility paths for SteamVR/OpenVR only after the base runtime is proven.
- Hardware path: begin with one known headset/controller pair, one GPU, and one display mode before expanding the matrix.
- Developer path: support AI-assisted diagnostics and setup automation without storing raw secrets in the repo.

Non-primary targets:

- Android support is tracked later.
- Windows support is tracked later because upstream notes that direct mode access for existing HMDs remains a major blocker outside NDA APIs.
- Multi-headset and multi-GPU configurations are expansion targets after single-device smoke tests pass.

Definition of done:

- Monado starts cleanly.
- OpenXR loader resolves Monado.
- XR hardware is visible without root-only access.
- `hello_xr` or equivalent OpenXR sample launches.
- Logs identify GPU, driver, compositor mode, and selected runtime.
- Failure cases have documented fixes.

## 2. Hardware / OS / GPU requirements

Minimum project matrix:

| Layer | Requirement | Notes |
|---|---|---|
| OS | Ubuntu/Debian Linux first | Package path should be tested before source build. |
| GPU API | Vulkan-capable driver | Monado compositor requires Vulkan driver support for external memory/semaphore extensions. |
| OpenGL apps | `GL_EXT_memory_object_fd` support | Needed for OpenXR applications that render through OpenGL. |
| XR permissions | udev rules | Install `xr-hardware` where available so devices do not require root. |
| Loader | Khronos OpenXR loader | Apps link to `libopenxr_loader.so` and discover a runtime. |
| Runtime | Monado OpenXR runtime | Provided by distro package or source build. |
| Tools | `monado-cli`, `monado-gui`, OpenXR utilities | Used for probing hardware, calibration, and smoke tests. |

Recommended first workstation:

- Modern AMD RADV, Intel ANV, or NVIDIA proprietary driver.
- One tethered HMD or known supported device.
- No exotic multi-GPU routing until basic compositor logs are clean.

## 3. Install option A: distro packages

Use this path first on Debian/Ubuntu when packages are available.

```bash
sudo apt update
sudo apt install \
  libopenxr-loader1 \
  libopenxr-dev \
  libopenxr1-monado \
  xr-hardware \
  libopenxr-utils \
  openxr-layer-corevalidation \
  openxr-layer-apidump \
  monado-cli \
  monado-gui
```

Validate package presence:

```bash
dpkg -l | grep -E 'openxr|monado|xr-hardware'
which monado-service || true
which monado-cli || true
which hello_xr || true
```

Package-path done state:

- OpenXR loader installed.
- Monado runtime JSON installed.
- `xr-hardware` installed or equivalent udev rules confirmed.
- `hello_xr`, `monado-cli`, or equivalent diagnostic tooling is present.

## 4. Install option B: build from source

Use this path when distro packages are unavailable, stale, or missing required drivers/features.

Install build tools and common dependencies:

```bash
sudo apt update
sudo apt install \
  build-essential git wget unzip cmake ninja-build \
  libeigen3-dev curl patch python3 pkg-config \
  libx11-dev libx11-xcb-dev libxxf86vm-dev libxrandr-dev libxcb-randr0-dev \
  libvulkan-dev glslang-tools libglvnd-dev libgl1-mesa-dev \
  ca-certificates libusb-1.0-0-dev libudev-dev libhidapi-dev \
  libwayland-dev libuvc-dev libavcodec-dev libopencv-dev libv4l-dev \
  libcjson-dev libsdl2-dev libegl1-mesa-dev libbsd-dev
```

Build and install:

```bash
git clone https://gitlab.freedesktop.org/monado/monado.git
cmake -G Ninja -S monado -B build -DCMAKE_INSTALL_PREFIX=/usr
sudo ninja -C build install
```

Optional debug build pattern:

```bash
cmake -G Ninja -S monado -B build-debug \
  -DCMAKE_BUILD_TYPE=Debug \
  -DCMAKE_INSTALL_PREFIX=/usr/local
ninja -C build-debug
```

Source-path done state:

- Build completes.
- `monado-service` exists.
- Runtime JSON exists under the expected OpenXR runtime path.
- Tools can run without missing shared-library errors.

## 5. Start `monado-service`

Manual mode, best for developers:

```bash
monado-service
```

Systemd user socket mode, best for packaged end-user setups:

```bash
systemctl --user enable monado.socket
systemctl --user start monado.socket
systemctl --user status monado.socket
```

Stop service:

```bash
systemctl --user stop monado.service || true
```

If a prior crash leaves a stale IPC socket, start Monado again first and let it clean the socket. If that fails, inspect:

```bash
ls -lah /run/user/$UID/ | grep monado || true
```

## 6. Select Monado as OpenXR runtime

System-wide active runtime:

```bash
sudo mkdir -p /etc/xdg/openxr/1/
sudo ln -sf /usr/share/openxr/1/openxr_monado.json /etc/xdg/openxr/1/active_runtime.json
```

User-level active runtime:

```bash
mkdir -p ~/.config/openxr/1
ln -sf /usr/share/openxr/1/openxr_monado.json ~/.config/openxr/1/active_runtime.json
```

One-command override:

```bash
XR_RUNTIME_JSON=/usr/share/openxr/1/openxr_monado.json hello_xr -G Vulkan
```

Runtime-selection checks:

```bash
ls -lah /usr/share/openxr/1/ | grep monado
ls -lah ~/.config/openxr/1/active_runtime.json 2>/dev/null || true
ls -lah /etc/xdg/openxr/1/active_runtime.json 2>/dev/null || true
```

## 7. Run smoke test

Hardware probe:

```bash
monado-cli probe || true
monado-cli test || true
```

Service + compositor logs:

```bash
XRT_PRINT_OPTIONS=1 \
PROBER_LOG=debug \
XRT_COMPOSITOR_LOG=debug \
monado-service
```

OpenXR sample:

```bash
XR_RUNTIME_JSON=/usr/share/openxr/1/openxr_monado.json hello_xr -G Vulkan
```

Validation layer, when debugging app API misuse:

```bash
XR_ENABLE_API_LAYERS=XR_APILAYER_LUNARG_core_validation \
XR_RUNTIME_JSON=/usr/share/openxr/1/openxr_monado.json \
hello_xr -G Vulkan
```

Smoke-test capture checklist:

- Date, OS version, kernel, GPU, driver version.
- HMD/controller model.
- Install path: distro package or source build.
- Runtime JSON path.
- `monado-service` startup logs.
- `monado-cli probe` / `monado-cli test` result.
- `hello_xr` result.
- Failure signature and fix, if any.

## 8. Common failures and fixes

| Failure | Likely cause | Fix |
|---|---|---|
| OpenXR app uses wrong runtime | `XR_RUNTIME_JSON` unset or `active_runtime.json` points elsewhere | Set `XR_RUNTIME_JSON` or update `active_runtime.json`. |
| Device only works as root | Missing udev permissions | Install `xr-hardware` or add proper udev rules. |
| Compositor starts but black screen | GPU/display/direct mode mismatch | Start with `XRT_COMPOSITOR_LOG=debug`; test `XRT_COMPOSITOR_FORCE_XCB=1`. |
| Wrong GPU selected | Multi-GPU routing | Use `XRT_COMPOSITOR_FORCE_GPU_INDEX` after reading compositor logs. |
| OpenGL OpenXR app fails | Missing `GL_EXT_memory_object_fd` support | Test Vulkan first; update Mesa/NVIDIA driver; avoid AMDVLK for OpenGL path if rendering breaks. |
| HMD mode is wrong | Default display mode selection | Use `XRT_COMPOSITOR_PRINT_MODES=1`, then set `XRT_COMPOSITOR_DESIRED_MODE`. |
| App starts but nothing presents | Multiple non-overlay apps running | Use `monado-ctl` where available or close competing clients. |
| Calibration missing | Camera-tracked setup needs config | Use `monado-gui`; config generally lives under `~/.config/monado/`. |
| SteamVR compatibility weirdness | Input profile / plugin mismatch | Keep native OpenXR path separate; test SteamVR plugin only after base runtime passes. |

## 9. Known limitations

- Linux is the primary serious hardware-driver path.
- Android and Windows are not first-pass project targets.
- Windows has major practical limitations for direct mode with existing HMDs.
- Some drivers/features depend on optional source-build dependencies.
- OpenGL app support depends on specific OpenGL driver extension support.
- Multi-GPU, legacy OpenVR/SteamVR, overlays, and exotic tracking setups are second-pass work.
- This repo must not store raw OpenAI, GitHub, Slack, Azure, Hugging Face, or SharePoint secrets.

## 10. Links to upstream docs/issues

Primary upstream references:

- Monado developer site: https://monado.freedesktop.org/
- Monado getting started: https://monado.freedesktop.org/getting-started.html
- Monado upstream source: https://gitlab.freedesktop.org/monado/monado
- Khronos OpenXR SDK: https://github.com/KhronosGroup/OpenXR-SDK
- Khronos OpenXR SDK Source: https://github.com/KhronosGroup/OpenXR-SDK-Source

Internal project links to add:

- GitHub tracking issue for setup execution.
- Slack `#helios-ops` coordination thread or canvas.
- SharePoint governed setup page / runbook.
- OpenAI Platform project/key target for local assistant diagnostics.

## Immediate next actions

- [ ] Confirm first target HMD/controller and GPU.
- [ ] Confirm initial OS image and package manager.
- [ ] Run distro package install path.
- [ ] Capture smoke-test logs.
- [ ] Promote known-good commands into `scripts/setup-monado-linux.sh`.
- [ ] Add CI lint for setup scripts.
- [ ] Add SharePoint runbook mirror.
- [ ] Post Slack status and pin the coordination canvas.
