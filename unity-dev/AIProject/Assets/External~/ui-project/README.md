# FairyGUI UI工程

FairyGUI 3.9 UI设计工程，包含游戏所有UI界面资源。

## 设计分辨率

- 宽度：750px
- 高度：1334px
- 适配模式：ScaleWithScreenSize（MatchWidthOrHeight）

## UI包列表

| 包名 | 说明 | 主要组件 |
|------|------|----------|
| LoginUI | 登录界面 | LoginUIPanel、第三方登录按钮(Apple/Google/Twitter/Line) |
| BattleMainUI | 战斗主界面 | BattleMainUIPanel、CompMainUserInfo、摇杆、能量条 |
| Basics | 基础资源 | 通用组件(Button/ProgressBar/Slider等)、字体、音效 |
| LauncherUI | 启动器UI | AOT模式，使用FairyGUI默认生成 |

## 目录结构

```
ui-project/
├── assets/           # UI资源
│   ├── LoginUI/      # 登录界面
│   ├── BattleMainUI/ # 战斗主界面
│   └── Basics/       # 基础资源
├── settings/         # 配置文件
├── plugins/          # 插件（GenCode代码生成）
├── tempCodes/        # 生成的代码临时目录
└── test.fairy        # 工程文件
```

## 发布配置

- 输出路径：`../../AssetRaw/UIRaw/Atlas`
- 格式：二进制(.bytes)
- 图集尺寸：2048（支持分页）

## GenCode代码生成插件

### 概述

自定义Lua插件，发布时自动生成C#代码。

### 生成文件类型

| 文件 | 说明 | 是否覆盖 |
|------|------|----------|
| `XxxPanel.Designer.cs` | Window控件声明 | 每次发布覆盖 |
| `Xxx.Designer.cs` | View控件声明 | 每次发布覆盖 |
| `XxxBinder.cs` | 包绑定类 | 每次发布覆盖 |

> **注意**：业务逻辑文件（`XxxPanel.cs`）需手动创建，插件不会自动生成。

### 命名规则

- 类名以`Panel`结尾 → 生成Window类（继承`FairyUIWindow`）
- 其他导出组件 → 生成View类（继承`GComponent`）
- 变量名首字母大写：`_btnLogin` → `BtnLogin`

### 控件前缀过滤

只有符合前缀的控件才会生成代码字段：
```
comp, img, btn, label, progress, slider, combo, 
txt, rtxt, input, loader, loader3D, list, graph, 
ctrl, trans, scroll, group
```

### 业务逻辑方法

手动创建的业务逻辑类需实现以下override方法：
- `RegisterEvent()` - 注册事件监听
- `OnCreate()` - 窗口创建初始化
- `OnRefresh()` - 刷新窗口数据
- `OnShow()` - 窗口显示逻辑
- `OnHide()` - 窗口隐藏逻辑
- `OnDestroy()` - 窗口销毁清理

### 自定义数据支持

在FairyGUI编辑器的"自定义数据"字段可配置：
- `Sound=1003` → 自动生成音效加载代码
- 格式：`Key=Value,Key2=Value2`

### 验证规则

- 禁止跨包引用（Basic除外）
- 导出组件必须驼峰命名（首字母大写）
- 不同包不能有同名导出组件
- CommonTex只能包含图片

### 工具菜单

FairyGUI编辑器"工具"菜单提供：
- **导出多语言文本** → 导出到`GameRes/Configs/Localization/test.xml`

## 国际化

支持多语言：
- 中文：`GameRes/Configs/Localization/Localization_zh.xml`
- 日文：`GameRes/Configs/Localization/Localization_ja.xml`
