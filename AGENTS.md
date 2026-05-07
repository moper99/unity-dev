# Open Code游戏工作室 — Unity游戏技术框架架构

制定高质量适配多平台Unity游戏技术架构。

## 技术栈

- **引擎**：[Unity]
- **语言**：[C#]
- **版本控制**：Git，基于主干的开发


## 协作协议

**用户驱动的协作，而非自主执行。**
每个任务遵循：**问题 -> 选项 -> 决策 -> 草稿 -> 批准**

- 代理在使用Write/Edit工具之前必须询问"我可以将此写入[文件路径]吗？"
- 代理在请求批准之前必须显示草稿或摘要
- 多文件更改需要对整个更改集进行明确批准
- 未经用户指示不得提交

## 项目技术要点

### 框架架构
- TEngine框架 + 模块化设计
- GameModule静态类统一管理所有模块（UI/Audio/Resource/Scene/Timer/Fsm等）
- SingletonSystem管理全局单例生命周期（支持Update/FixedUpdate/LateUpdate）

### 热更新流程
- HybridCLR实现C#热更新
- Procedure状态机管理启动流程
- 热更新入口: GameApp.Entrance()
- 热更新代码目录: Assets/GameScripts/HotFix/

### UI开发规范
- FairyGUI制作界面，生成代码到View目录
- 使用 [WindowAttribute(UILayer.UI)] 标记窗口
- 实现 override 方法: OnCreate/OnShow/OnHide/OnDestroy/RegisterEvent/OnRefresh

### FairyGUI代码生成规范
- `tempCodes/` 目录是FairyGUI插件生成的临时代码目录，可查看但无需修改
- `*.Designer.cs` 文件由FairyGUI GenCode插件自动生成，每次生成时会覆盖，不要修改
- 业务逻辑文件（`*.cs`）需手动创建，使用 `protected override` 方法（OnCreate/OnShow等）实现业务逻辑

### 目录规范
- Assets/TEngine/ - 框架核心（勿随意修改）
- Assets/GameScripts/Procedure/ - 流程代码
- Assets/GameScripts/HotFix/GameLogic/ - 业务逻辑
- Assets/Launcher/ - 启动器相关


