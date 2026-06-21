# 背包道具筛选与选中状态保持：实现复盘

## 任务背景

在空 Unity 2022.3 项目中，实现背包 UI 筛选功能，用于验证 workline v1.4 的完整闭环能力。

## 技术要点

- Unity UI（UGUI）：Canvas、Button、Text、ScrollView
- 道具类型枚举 + 数据模型 + Mock 数据分离
- 筛选逻辑集中管理（InventoryPanel）
- 选中状态唯一性（同时间一个格子选中）
- 筛选后选中保持/清除逻辑
- Editor 脚本自动构建 UI（避免手写 YAML）

## 遇到的问题

1. 项目缺少 `com.unity.ugui` 包 → 12 个编译错误 → 补 manifest.json 解决
2. Codex 使用 Editor 脚本方案而非直接创建完整场景 → 需要运行 Tools → Setup Backpack UI
3. ChangedFiles 和 REVIEW 完整，但 TestReport 和 RiskReport 因中断缺漏 → 补 delegate_task 完成

## 解决方案

- manifest.json 补 `com.unity.ugui: 1.0.0`
- Editor 脚本方案可以接受（避免手写 YAML），但增加了用户操作步骤
- 审查阶段补全了缺失报告

## 可复用经验

- 新建 Unity 项目要确认 UGUI 包是否已安装
- Agent 输出可能缺漏 → 审查 GATE 必须检查 4 份报告全部到位
- 编译回路生效：12 错误 → 补包 → 0 错误

## 可沉淀为 Harness 规则的内容

- Phase 5 编译阶段：Unity 项目检查 manifest.json 是否包含 `com.unity.ugui`
- 审查 checklist：确认 4 份报告全部存在，不只看 REVIEW.md

## 面试表达

> 我在实现背包筛选功能时，先通过 SDD 明确了任务范围、非目标和验收标准，避免 AI Agent 越权修改无关模块。
>
> 在实现过程中，我重点关注了 UI 状态管理：筛选列表时，如果当前选中道具仍然存在，需要保持选中；如果不存在，则需要清空选中状态和详情区域。同时要求 Agent 输出 ChangedFiles、TestReport 和 RiskReport 三份结构化报告，分别说明改了哪些文件、如何验证、还存在哪些风险。
>
> 在 Unity 客户端开发中，我会特别注意 UI 生命周期、列表刷新性能、GC Alloc、资源加载边界和模块边界，避免为了一个小功能引入过度设计或无关重构。
>
> 对于 AI 辅助生成的代码，我不会直接采用，而是通过审查阶段验证其是否修改了无关文件、是否遵守了非目标约束。
