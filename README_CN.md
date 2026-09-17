<div align="center">

# ⚡ IDEA内存管理 (IdeaMemoryManager)

**专为全栈与微服务开发者打造的工业级、无损内存优化与防卡死守护工具**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6.svg)](https://microsoft.com/windows)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)

[English Documentation](README.md) · [功能特性](#-核心特性) · [工作原理](#-核心工作原理) · [架构设计](#-架构设计) · [快速开始](#-快速开始)

</div>

---

## 💡 痛点背景

使用 **IntelliJ IDEA** 配合本地 Spring Boot 微服务与 Node 前端工具链开发时，开发者通常面临以下挑战：
1. **内存急剧膨胀**：多模块开发过程中，IDEA、后台微服务与前端守护进程常驻内存，数小时内可累积占用 **6 GB ~ 10 GB** 物理内存，导致开发机系统卡顿甚至转圈假死。
2. **常规清理工具反弹**：基于通用内核 API 的普通内存清理软件无法触发 Java 虚拟机内部回收，内存短暂下降后数秒内即会反弹回原始高位。
3. **高频重启打断开发**：传统方案通常只能依赖每日重启 IDE，打断编码状态。

**IdeaMemoryManager** 采用 **「应用层 JVM 真实 Full GC + 操作系统内核工作集裁剪」** 双层回收架构，在 **100% 保证项目活跃连接、调试会话及热更新不中断** 的前提下，一键将物理内存开销安全压缩，并有效杜绝反弹。

---

## 🚀 核心特性

- 🛡️ **非破坏性安全优化**：
  - 基于 Windows 内核工作集安全收缩机制，不杀死任何进程。
  - 正在运行的 Spring Boot 接口、Vite/Webpack 热更新、终端控制台及数据库连接池 **持续平稳运行**。
- ⚡ **双层联动 · 彻底杜绝反弹**：
  - **第一层 (JVM 深度 GC)**：自动调用运行环境的 `jcmd` 工具指令触发 Full GC，真正销毁 Java 堆内废弃的语法分析与软引用缓存；
  - **第二层 (内核工作集裁剪)**：随后安全释放物理内存页，从根源消除反弹诱因。
- 🌐 **多语言一键切换 (i18n)**：
  - 原生支持 **English** 与 **简体中文**，随系统语言自动识别，并支持在界面右上角一键切换。
- 🌐 **开发进程拓扑自动感知**：
  - 自动识别并聚类 IDE 宿主进程、关联的 Java 微服务、Node.js 前端构建服务及语言服务器守护进程。
- 🎯 **智能高水位自愈 (Smart Threshold)**：
  - 支持设置警戒水位（如内存超过 3.5 GB），自动在后台静默完成优化，无需手动干预。
- 🏆 **累计释放成效看板**：
  - 本地记录累计节省的物理内存数据，直观量化优化成果。


---

## 🔬 核心工作原理

```mermaid
sequenceDiagram
    autonumber
    actor Dev as 开发者 / 自动化调度器
    participant Mgr as IdeaMemoryManager
    participant JVM as IDE & Java 虚拟机 (jcmd)
    participant Kernel as Windows 内核 (psapi.dll)

    Dev->>Mgr: 触发内存优化
    Mgr->>Mgr: 扫描并聚类关联的 Java / Node 进程
    Note over Mgr,JVM: 第一层：应用层真实销毁 (防止反弹)
    Mgr->>JVM: 调用 jcmd <PID> GC.run 触发 Full GC
    JVM-->>Mgr: 堆内废弃对象彻底回收，Java 堆收缩
    Note over Mgr,Kernel: 第二层：内核级物理内存卸载
    Mgr->>Kernel: 调用 EmptyWorkingSet(hProcess)
    Kernel-->>Mgr: 物理 RAM 立即清空归还给操作系统
    Mgr->>Dev: 优化完成！内存稳定保持低位，项目平稳运行
```

---

## 🏗️ 架构设计

遵循清晰的分层解耦规范：

```
IdeaMemoryManager/
├── src/
│   ├── Common/             # 基础设施 (ByteSizeFormatter, Logger, I18n)
│   ├── Interop/            # Windows 原生 P/Invoke 接口定义
│   ├── Config/             # 强类型配置实体、持久化与自启管理
│   ├── Core/
│   │   ├── Models/         # 领域数据模型
│   │   ├── Scanner/        # ProcessScanner (进程拓扑发现引擎)
│   │   ├── Strategy/       # 策略模式: ICleanStrategy
│   │   ├── Engine/         # MemoryCleanEngine (统一优化调度引擎)
│   │   └── Automation/     # SmartScheduler (自愈与计划任务调度)
│   ├── UI/
│   │   ├── Views/          # MainForm (现代深色微悬浮面板)
│   │   └── Tray/           # TrayService (系统托盘集成)
│   └── Program.cs          # 应用程序入口与未处理异常守护
├── .github/workflows/      # 自动化 CI/CD 构建与 Release 发布脚本
├── IdeaMemoryManager.csproj# .NET 8 工程项目文件
├── LICENSE                 # MIT 开源协议
├── CONTRIBUTING.md         # 社区贡献指引
├── README.md               # 中文主文档
├── README_EN.md            # 英文官方文档
└── build.bat               # 本地自动化构建脚本
```

---

## 🚀 快速开始

### 方式 A：运行预构建版本
直接运行发布产物 `IdeaMemoryCleaner.exe` 即可使用。

### 方式 B：源码构建
要求环境：[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

```bash
# 1. 克隆代码仓库
git clone https://github.com/BowlongRise/IdeaMemoryManager.git
cd IdeaMemoryManager

# 2. 编译项目
dotnet build -c Release

# 3. 运行程序
./bin/Release/net8.0-windows/IdeaMemoryCleaner.exe
```

或直接在 Windows 环境中双击运行 `build.bat`。

---

## 📄 开源许可证

本项目基于 [MIT License](LICENSE) 协议开源。
