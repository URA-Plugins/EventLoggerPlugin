# EventLoggerPlugin

`EventLoggerPlugin` records event, training, succession, and race statistics in its own workspace.

Its manifest declares optional linkage to `LegendScenarioAnalyzer`, `RamenScenarioAnalyzer`, `PioneerScenarioAnalyzer`, and `FestaScenarioAnalyzer`. EventLogger tracks Legend (scenario ID `10`), Pioneer (`11`), Ramen (`14`), and Festa (`15`) sessions. Ramen, Pioneer, and Festa display integrations capture immutable data for `(single_mode_chara_id, turn)` and update the producer-owned `EventLogger` Extra section for that display ID. Legend display integration is disabled. Missing analyzers do not prevent EventLogger from loading or using its own workspace; each display bridge is guarded by `IPluginContext.IsPluginAvailable`.

Pioneer tracking covers `start`, `load`, `change_short_cut`, `check_event`, `check_point`, `continue`, `exec_command`, `finish_claw_crane`, `gain_skills`, `pioneer_complete`, `pioneer_live`, `planning`, `race_end`, `race_entry`, `race_out`, and `shima_training_exec`. Training records include scenario gains, summer commands, failed-training results, and training-level progress. Island training is recorded as command `3101` separately from the five regular training types. Session changes clear event, inheritance, and race statistics. Pioneer Extra shows card events, training failures, event counts, inheritance gains, and race wins; history retains the captured statistics for its own character and turn.

Complete succession choices from `check_event`, `exec_command`, and `load` are published by priority `3` analyzers after `EventResponseAnalyzer` (priority `2`). Candidates render side by side through native Terminal.Gui Line and Label views in a square-bordered table with 52-column content cells; each candidate lists skill Hint changes before white-factor names without displaying factor source positions. The outer table exposes only a horizontal scrollbar, while each candidate's Hint rows and white-factor rows use separate native vertical scroll views (four total). Hint rows receive spare height first while each factor region keeps at least six content rows; wheel input is consumed only by the hovered detail region with a visible vertical scrollbar, or by the outer table with a visible horizontal scrollbar. Incomplete succession data does not create the panel or switch workspaces.

An `effect_type`-only succession event records its baseline and reports the revised five-stat gain together with the skill-point gain in the linked ScenarioAnalyzer `Extra` rows after the following response.

轮次快照的 `KeyEvents` 记录友人首次点击事件与携带支援卡的连续事件；`SkillPtSpent` 按相邻状态的技能点减少量累计。普通、Pioneer 与 Festa 的 `gain_skills` 响应更新消耗统计，重复响应不重复累计。`IsAfterTraining(turn)` 表示该回合已发出行动请求，包含训练、休息和 Pioneer 岛训练，不表示行动成功。

训练请求与结果通过 `GameHttpHeaders.RequestId` 关联；普通、Pioneer 岛训练与 Festa 共用一条待匹配训练记录。响应 ID 不匹配时继续解析响应，不消费其他请求的训练记录，也不将训练失败归入未知回合。会话重置及插件卸载时清空待匹配记录。

Festa 接收开局、载入、事件、稽古、教诲装备、检查点、演出、比赛及学技能响应。`5101–5105` 映射到五种训练，按完整命令 ID 合并 home 与 Festa 增量；可执行稽古优先，否则取普通训练，与 Festa 面板口径一致。连续事件根据 Host 名字库的支援卡类型排除友人卡（含 `30323`），并保留特殊卡排除规则。

Festa 联动使用 `deps/FestaScenarioAnalyzer` 源码依赖、manifest 软依赖与延迟桥接；Festa 不反向引用 EventLogger。两个插件须成对更新到 `2026.11.1`。附加区域及其历史显示连续事件、训练失败、事件数、继承收益和胜场；无内容不占位，同键更新不增加历史或未读。任一插件单独安装可使用自身功能。

2026-11-01 隔离回放验证 36 次稽古映射、55 次训练收益、17 场比赛与两次技能消费（8,470、1,163，累计 9,633）。六张支援卡中的友人 `30323` 不计入连续事件，总量为 15。

同一待处理事件及数值状态重复返回时，等待事件推进后结算。将每份响应重复回放一次，事件数保持 127，技能消费及继承收益与正常回放一致。

## Build

要求 Host SDK 和运行中的 Host 至少为 `2026.11.1.2`；转发器须在请求及对应响应中提供相同的 `X-Hachimi-Request-Id`。

Host SDK `2026.10.31.1` 对应的 Host 使用 Terminal.Gui `2.5.0`，无法加载依赖 Terminal.Gui `2.4.17` 的已归档 `LegendScenarioAnalyzer v2026.9.26` 正式 ZIP。Legend 为可选联动，本插件不要求安装该 ZIP。

The plugin references the `UmamusumeResponseAnalyzer` NuGet package and pins linked plugin sources with Git submodules. From the repository root after cloning:

```powershell
git -c core.longpaths=true submodule update --init --recursive
dotnet build .\EventLoggerPlugin.csproj -c Release -m:1 -p:RuntimeIdentifier=win-x64 -p:SelfContained=false -p:PlatformTarget=AnyCPU -p:DeployUraPluginToLocalAppDataOnBuild=false
```

## 验证与发布

在 Windows 仓库根执行 `act workflow_dispatch --artifact-server-path "$env:TEMP/ura-act-artifacts"`。本地与 GitHub 使用同一份 workflow；版本 tag 触发 GitHub Release 发布。环境要求、共用 workflow 本地映射和发布规则见 [URA plugin workflows](https://github.com/URA-Plugins/.github/blob/v1/README.md)。
