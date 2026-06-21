# ClarificationQuestions：背包收藏与排序功能

> 以下问题必须在编码前由需求方（Mentor）回答。当前状态：BLOCKED。

## C1：持久化

当前约束禁止所有存储手段。以下至少放松一项：

- [ ] 是否允许在 ItemData 中新增 `bool IsFavorite` 字段？
- [ ] 是否允许新增一个 `Dictionary<int, bool>` 作为收藏状态缓存？
- [ ] 是否允许使用 PlayerPrefs 存储收藏状态？
- [ ] 是否允许使用本地 JSON 文件存储收藏状态？
- [ ] 是否放弃"关闭后仍然存在"需求，改为仅当前会话有效？

## C2：UI 控件

- [ ] 是否允许修改 SlotPrefab 增加收藏按钮/星标？
- [ ] 是否允许在运行时动态创建收藏按钮（不修改 Prefab）？
- [ ] 是否允许使用现有 Text/Image 组件的颜色变化作为收藏指示（不新增控件）？
- [ ] 是否允许在 SlotView 代码中切换现有 UI 元素状态来显示收藏？

## C3：排序

- [ ] 是否允许在 RefreshList 中增加"收藏优先"排序逻辑？
- [ ] 是否将"不允许改变显示顺序"理解为"不改变非收藏道具之间的顺序"？
- [ ] 还是严格禁止任何排序变更？

## C4：修改范围

- [ ] 是否允许修改 InventoryPanel.RefreshList() 方法？
- [ ] 是否允许修改 InventorySlotView.Setup() 方法签名？
- [ ] 是否允许在 InventorySlotView 中新增 OnFavoriteClicked 回调？

## 建议放松方案

如果以上问题无法全部回答，建议至少放松以下 3 项即可执行：

```text
1. ItemData 加 IsFavorite 字段（或等价的状态缓存）
2. SlotPrefab 加一个收藏指示 UI（不改架构）
3. RefreshList 加收藏优先排序
```

其他约束保持不动。
