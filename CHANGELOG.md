# Changelog

All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](https://semver.org/).

---

## [v1.1.0] - 2026-09-18

### 🚀 New Features
- **Idle-Aware GC**:
  - Added user input idle detection (`GetLastInputInfo`) and CPU load check.
  - Automatically defers background cleaning when you are typing or compiling.
- **Zero Self-Footprint**:
  - Automatically trims its own memory when minimized or idle, keeping resident RAM at **10MB ~ 15MB**.
- **JetBrains Ecosystem Expansion**:
  - Supports `IntelliJ IDEA`, `PyCharm`, `WebStorm`, `GoLand`, `DataGrip`, `CLion`, `Rider`, `RustRover`, `Android Studio`, and `Fleet`.
- **Process Drawer & Whitelist**:
  - Expandable bottom drawer to inspect child processes (PID, RAM, Category).
  - Supports individual selection and right-click whitelisting to skip cleaning for specific services.
- **JVM Heap Telemetry**:
  - Parses `jcmd GC.heap_info` to view Eden, Old Gen, and Metaspace usage in hover tooltips.
- **60s Sparkline Chart**:
  - Real-time memory trend chart showing the memory drop after optimization.

### 🎨 Improvements
- Optimized window layout with expandable bottom panel.
- Bundled local vector SVG badges for reliable loading.

---

## [v1.0.0] - 2026-09-18

### 🚀 Initial Features
- Non-destructive memory optimizer for IntelliJ IDEA.
- Dual-layer anti-rebound reclamation: JVM Full GC (`jcmd`) + Windows WorkingSet trim.
- Real-time memory monitoring and lifetime savings tracker.
- Smart high-watermark threshold scheduler.
- System tray integration, Windows autostart toggle, and bilingual support (EN/ZH).