# EventLoggerPlugin

`EventLoggerPlugin` records event, training, succession, and race statistics in its own workspace.

Its manifest declares optional linkage to `LegendScenarioAnalyzer`, `RamenScenarioAnalyzer`, and `PioneerScenarioAnalyzer`. EventLogger tracks Legend (scenario ID `10`), Pioneer (`11`), and Ramen (`14`) sessions. Ramen and Pioneer display integrations capture immutable data for `(single_mode_chara_id, turn)` and update the producer-owned `EventLogger` Extra section for that display ID. Legend display integration is disabled. Missing analyzers do not prevent EventLogger from loading or using its own workspace; each display bridge is guarded by `IPluginContext.IsPluginAvailable`.

Pioneer tracking covers `start`, `load`, `change_short_cut`, `check_event`, `check_point`, `continue`, `exec_command`, `finish_claw_crane`, `gain_skills`, `pioneer_complete`, `pioneer_live`, `planning`, `race_end`, `race_entry`, `race_out`, and `shima_training_exec`. Training records include scenario gains, summer commands, failed-training results, and training-level progress. Island training is recorded as command `3101` separately from the five regular training types. Session changes clear event, inheritance, and race statistics. Pioneer Extra shows card events, training failures, event counts, inheritance gains, and race wins; history retains the captured statistics for its own character and turn.

Complete succession choices from `check_event`, `exec_command`, and `load` are published by priority `3` analyzers after `EventResponseAnalyzer` (priority `2`). Candidates render side by side through native Terminal.Gui Line and Label views in a square-bordered table with 52-column content cells; each candidate lists skill Hint changes before white-factor names without displaying factor source positions. The outer table exposes only a horizontal scrollbar, while each candidate's Hint rows and white-factor rows use separate native vertical scroll views (four total). Hint rows receive spare height first while each factor region keeps at least six content rows; wheel input is consumed only by the hovered detail region with a visible vertical scrollbar, or by the outer table with a visible horizontal scrollbar. Incomplete succession data does not create the panel or switch workspaces.

An `effect_type`-only succession event records its baseline and reports the revised five-stat gain together with the skill-point gain in the linked ScenarioAnalyzer `Extra` rows after the following response.

轮次快照的 `KeyEvents` 记录友人首次点击事件与携带支援卡的连续事件；`SkillPtSpent` 按相邻状态的技能点减少量累计。普通与 Pioneer 的 `gain_skills` 响应更新消耗统计，重复响应不重复累计。`IsAfterTraining(turn)` 表示该回合已发出行动请求，包含训练、休息和 Pioneer 岛训练，不表示行动成功。

## Build

The plugin references the latest stable `UmamusumeResponseAnalyzer` NuGet package and pins linked plugin sources with Git submodules. From the repository root after cloning:

```powershell
git -c core.longpaths=true submodule update --init --recursive
dotnet build .\EventLoggerPlugin.csproj -c Release -m:1 -p:RuntimeIdentifier=win-x64 -p:SelfContained=false -p:PlatformTarget=AnyCPU -p:DeployUraPluginToLocalAppDataOnBuild=false
```

## 验证与发布

本仓 `tests/EventLoggerPluginSmoke` 验证分析器分发、真实 EventResponse/Legend 优先级、继承选择布局、训练失败和剧本统计，并加载本轮 ZIP 验证实际业务回调、程序集来源及清理。`deps/EventResponseAnalyzer` 仅供测试，不进入生产引用或 manifest 的 `Dependencies`。

`tests/PioneerEventLoggerSmoke` 使用本仓固定的 Pioneer 源码与 fixture，覆盖请求与响应联动、事件链、训练失败、夏合宿、岛训练、继承、历史快照及缺失 Pioneer 时的独立加载。

测试使用 `UraTestHostRoot` 指定匹配的 Host 源码；生产插件继续引用 NuGet。运行默认业务 smoke 前，将 `URA_PLUGIN_SMOKE_PACKAGE_ROOT` 设为本轮构建 ZIP 所在目录；测试按 manifest 的 `InternalName` 唯一匹配，不接受缺包或重复包。GitHub Actions 使用该变量，未设置时使用 workflow 的 `GITHUB_WORKSPACE/artifacts`。`--package-load` 可单独运行 ZIP 验证。所有入口先执行本仓发布边界检查，测试工具由本仓 `tests/Support` 持有。

`tests/EventLoggerPluginReplaySmoke` 默认运行捕获解析与三语言错误识别自测；真实回放需传 `--corpus <目录>`。目录顶层 `*.txt` 必须以 `[整数序号]` 开头，包含 notify HTTP 头和原始 body。空语料、长度或 URL/SID 配对错误、未知端点、分发异常及可见 Error 均失败。Error 检查仅针对当前 framebuffer 的可见内容，不检查屏幕外历史或其他 workspace 的日志。真实捕获和临时回放输出不入库。

在 Windows 仓库根执行 `act workflow_dispatch --artifact-server-path "$env:TEMP/ura-act-artifacts"`。本地与 GitHub 使用同一份 workflow；版本 tag 触发 GitHub Release 发布。环境要求、共用 workflow 本地映射和发布规则见 [URA plugin workflows](https://github.com/URA-Plugins/.github/blob/v1/README.md)。
