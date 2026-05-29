# Getting Started Guide

Welcome to the Hermes Fleet Platforms super-suite! This live onboarding guide will help you, collaborators, or any AI/Copilot system get up and running.

---

## 1. Overview
This project integrates and extends legacy code and features from Hermes Fleet, Monado Blade, Helios Platform, and others to create an all-in-one AI-optimized, secure orchestration environment. It is modular, cleanly organized, and designed for both user and developer extensibility.

> Refer to the **project root README.md** for full vision, major folders, and short explanations.

---

## 2. Prerequisites
- Windows 10/11 (recommended for C#/.NET/PowerShell stack)
- Local admin (for install/setup steps)
- USB stick (if running installer/usb first-time setup)
- Optionally: Azure CLI, Power Platform CLI, GitHub CLI, Python3
- Visual Studio 2022+, VS Code, Dotnet SDK recommended for development

---

## 3. First-Time Setup

### a. USB/Installer Flow (Recommended)
1. Download/build the USB/installer tool from `/installer`.
2. Run on target machine with admin.
3. Animated walkthrough will guide partitioning, vault and quarantine setup, user/profile creation, driver scans, and initial hardening.
4. On completion, quickinstall runs to install key apps, devtools, and offer to connect to cloud (Azure, Power Platform, etc).

### b. Direct Deploy/Dev Mode
1. Clone repo.
2. See `/installer/README.md` for manual setup. Build/install core libs (`/core`), GUIs (`/gui`), AIs (`/ai`) as needed per dev task.
3. Follow onboarding prompts in each GUI and CLI tool. Check `/docs/PROFILES.md` for user/profile switching.

---

## 4. Next Steps / Using the Suite
- **Switch user/profile scenes:** Custom themes, parent/child, admin/unlock (see `/platform` and `/gui`).
- **AI/Automation:** Run or develop AI/ML projects in `/ai`, launch AI Builder or connect to cloud resources.
- **Integrate with Azure and other platforms:** See `/integrations` and `/docs/INTEGRATIONS.md`.
- **Security:** Review `/docs/SECURITY.md` before deploying in secure contexts.
- **Expand docs as you build!**

---
