# AI Unity Dev - 商业级Unity游戏项目

基于TEngine框架的Unity游戏项目，支持热更新、模块化架构。

## 技术栈

| 技术 | 版本/说明 |
|------|-----------|
| Unity | 2022.3.62f3 |
| 渲染管线 | URP 14.0 |
| 热更新 | HybridCLR |
| UI系统 | FairyGUI |
| 资源管理 | YooAsset |
| 异步编程 | UniTask |
| 框架 | TEngine |

## 项目结构

```
Assets/
├── TEngine/           # 框架核心
├── GameScripts/       # 游戏逻辑
│   ├── Procedure/     # 流程管理
│   └── HotFix/        # 热更新代码
│       └── GameLogic/ # 业务逻辑
├── FairyGUI/          # UI系统
├── Launcher/          # 启动器
└── AssetRaw/          # 热更资源
```

## 核心模块

- **GameModule** - 统一管理UI、音频、资源、场景等
- **SingletonSystem** - 全局单例生命周期管理
- **Procedure** - 启动流程状态机

## 启动流程

ProcedureLaunch → ProcedureSplash → ProcedureInitPackage → ProcedureDownloadFile → ProcedureStartGame

## 开发说明

热更新代码放在 `Assets/GameScripts/HotFix/GameLogic/` 目录。
UI使用FairyGUI制作，代码生成后在对应View目录实现业务逻辑。

## UI工程

FairyGUI工程位于 `unity-dev/AIProject/Assets/External~/ui-project/`，详见 [FairyGUI工程说明](unity-dev/AIProject/Assets/External~/ui-project/README.md)。
