# Hermes Fleet Platform Variants: Comparison Guide

## 1. C#-Only (MAUI/WPF)
- **Pros:** Unified stack, rapid dev, cross-platform (MAUI), strong MS/Azure integration
- **Cons:** Lower perf, less low-level access, .NET runtime required
- **Best for:** Enterprise, business, rapid prototyping

## 2. C# + JavaScript (Electron/Blazor)
- **Pros:** Modern UI, cross-platform, large dev pool
- **Cons:** Heavier apps, two stacks, more deployment complexity
- **Best for:** Web-like UIs, dashboards, API-driven systems

## 3. C++-Only (Qt/wxWidgets/Native)
- **Pros:** Max performance, resource efficiency, direct OS/hardware access
- **Cons:** Slower dev, complex UI, harder distribution
- **Best for:** Real-time, embedded, perf-critical

## 4. Mixed (C++ core, C# GUI, JS web)
- **Pros:** Best performance, rich desktop/web UI, extensibility
- **Cons:** Most complex integration, build, and maintenance
- **Best for:** Advanced, extensible, high-performance systems

---

See detailed variant docs for architecture, pros/cons, and integration notes.