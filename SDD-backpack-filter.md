# SDD：背包道具筛选与选中状态保持

## 1. 背景

当前项目是一个空 Unity 2022.3 工程。需要实现基础背包 UI 功能：展示道具列表，支持按类型筛选，正确处理筛选过程中的选中状态。

该功能用于验证 workline v1.4 的完整闭环：SDD（含非目标）→ Agent 编码 → 编译 → 审查 → 4 份结构化报告 → 复盘。

## 2. 功能目标

- 背包列表展示 mock 道具格子（图标占位 + 名称 + 数量 + 类型）
- 4 个筛选按钮：全部 / 装备 / 消耗品 / 材料
- 点击格子 → 唯一选中状态 + 详情区域显示信息
- 筛选后：选中道具在结果中 → 保持选中；不在 → 清除选中
- 空列表、空选中、重复点击边界处理

## 3. 非目标 / 本次不做

本次任务明确不处理以下内容：

- 不实现道具拖拽
- 不实现道具使用、出售、合成
- 不实现背包分页、排序
- 不接入后端协议、网络同步、存档系统
- 不引入 Addressables 或复杂资源加载
- 不引入新的 UI 框架
- 不重构项目架构
- 不修改公共协议、公共接口或全局配置
- 不修改与背包筛选无关的模块
- 不为了本功能批量格式化无关文件
- 不使用真实图标资源（占位图/纯色块即可）

如 Agent 认为必须修改以上内容，必须先在 REVIEW.md 中说明原因，并等待人工确认。

## 4. 涉及模块

| 模块 | 说明 |
|:---|:---|
| `Assets/Scripts/Inventory/` | 背包核心代码 |
| `Assets/Scenes/` | Demo 场景 |

## 5. 技术约束

### 修改范围

- 允许：`Assets/Scripts/Inventory/` 下所有文件、Demo 场景
- 禁止：ProjectSettings、Packages、Library、任何非 Inventory 目录

### 架构约束

- UI 显示逻辑和道具数据分离
- 筛选逻辑集中管理，不分散到各 Slot
- 选中状态由面板/控制器统一管理
- 不引入全局单例
- 不创建大型 Manager
- Mock 数据放独立文件，不混入正式数据层

### 性能约束

- 筛选时不频繁创建/销毁 UI 节点
- 优先复用已有 Slot GameObject
- 不频繁使用高 GC 写法（大量字符串拼接、无意义 LINQ 链）

## 6. 数据结构设计

```csharp
// 道具类型枚举
public enum ItemType { Equipment, Consumable, Material }

// 道具数据（纯数据，不继承 MonoBehaviour）
[System.Serializable]
public class ItemData
{
    public string Name;
    public ItemType Type;
    public int Count;
}

// Mock 数据提供
public static class InventoryMockData
{
    public static List<ItemData> GetItems() => new List<ItemData>
    {
        new ItemData { Name = "铁剑",   Type = ItemType.Equipment,   Count = 1 },
        new ItemData { Name = "木盾",   Type = ItemType.Equipment,   Count = 1 },
        new ItemData { Name = "小血瓶", Type = ItemType.Consumable,  Count = 5 },
        new ItemData { Name = "小蓝瓶", Type = ItemType.Consumable,  Count = 3 },
        new ItemData { Name = "铁矿石", Type = ItemType.Material,    Count = 12 },
        new ItemData { Name = "木材",   Type = ItemType.Material,    Count = 20 },
    };
}
```

## 7. UI 交互规则

### 筛选按钮
- 全部 → 显示所有道具
- 装备 → 只显示 Equipment
- 消耗品 → 只显示 Consumable
- 材料 → 只显示 Material

### 选中状态
- 同一时间最多一个格子选中
- 点 B → A 取消选中，B 选中
- 筛选后当前选中在结果中 → 保持选中
- 筛选后当前选中不在结果中 → 清除选中 + 清空详情
- 重复点击同格子 → 无异常

### 详情区域
- 有选中 → 显示名称 + 类型 + 数量
- 无选中 → 显示"未选择道具"

## 8. 任务拆分

| # | 任务 | 预估文件 |
|:--|:---|:--|
| T1 | 创建目录结构 + ItemType/ItemData/InventoryMockData | 2 文件 |
| T2 | 实现 InventoryPanel（面板控制器：筛选 + 选中 + 列表刷新） | 1 文件 |
| T3 | 实现 InventorySlotView（格子视图：显隐 + 选中标记 + 点击回调） | 1 文件 |
| T4 | 创建 Demo 场景（Canvas + Panel + 按钮 + 详情文本 + Slot prefab） | 1 场景 |
| T5 | 编译验证 + 手动测试步骤 | — |

## 9. 验收标准

### 功能验收
- [ ] 打开背包 → 6 个道具可见
- [ ] 筛选按钮切换 → 列表正确过滤
- [ ] 点击格子 → 选中 + 详情显示
- [ ] 唯一选中 + 切换正常
- [ ] 筛选后选中保持/清除正确
- [ ] 空列表不报错
- [ ] 无选中 → "未选择道具"
- [ ] 重复点击无异常

### 工程验收
- [ ] 编译 0 错误
- [ ] Agent 未修改无关文件
- [ ] Agent 未引入未声明依赖

## 10. 测试方式

- 编译检查：Unity Editor 内编译 0 错误
- 手动测试：Play 模式逐项验证
- 日志检查：Console 无 Error

## 11. 风险点

- Agent 过度设计 UI 框架
- Agent 引入不需要的资源加载
- 选中状态在筛选时丢失引用
- Mock 数据被混入正式代码路径

## 12. Agent 输出要求

Agent 完成后必须输出：
- `REVIEW.md` — 审查结论 + 越权检查
- `ChangedFiles.md` — 修改文件清单 + 无关文件检查
- `TestReport.md` — 验证记录（含未执行项说明）
- `RiskReport.md` — 风险分析（至少 2 类风险）
