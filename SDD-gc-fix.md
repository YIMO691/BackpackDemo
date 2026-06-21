# SDD：修复 InventorySearchPanel GC Alloc

## 1. 背景

`InventorySearchPanel.cs` 功能可用但存在 7 个 GC 分配问题：
- Update 定时刷新（0.2s）
- LINQ + ToList + OrderBy + Any
- ToLower 临时字符串
- Destroy/Instantiate 重建 Slot
- 重复 GetComponent
- 高频 Debug.Log 字符串插值
- 字符串拼接

## 2. 功能目标

修复 GC 问题，保持原有功能行为不变：
- 搜索过滤、类型筛选、点击选中、详情显示
- 筛选后选中保持/清除

## 3. 非目标 / 本次不做

- 不重写整个背包系统
- 不引入对象池框架
- 不引入新 UI 框架、Addressables、第三方依赖
- 不修改 InventoryItemData/InventoryItemType 字段含义
- 不把 mock 数据接入后端
- 不修改网络、协议、热更入口、全局配置
- 不修改与该面板无关的模块
- 不改变现有功能表现
- 不伪造 Profiler 结果

## 4. 允许范围

- `Assets/Scripts/Inventory/InventorySearchPanel.cs`
- `Assets/Scripts/Inventory/InventoryItemSlot` 类（同一文件内）

## 5. 验收标准

### 功能
- 搜索、筛选、选中、详情全部保持原有行为

### GC
- 移除 Update 无条件刷新
- 移除 LINQ（for 循环替代）
- 移除 ToLower（IndexOf + OrdinalIgnoreCase）
- 避免 Destroy/Instantiate（复用 + SetActive）
- 避免重复 GetComponent（缓存引用）
- 移除高频 Debug.Log

### 工程
- 无越权修改
- 4 份报告齐全
