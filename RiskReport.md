# RiskReport.md — InventorySearchPanel GC Fix

## Risk Assessment

### R1: Slot pool unbounded growth (LOW)
- **Description**: `_activeSlots` list grows on demand and never shrinks. If `Items` grows to thousands, the pool could hold many inactive GameObjects.
- **Mitigation**: Backpack UIs typically have bounded item counts (dozens to hundreds). For a demo project with 6 items, this is negligible. If needed, a future enhancement could cap the pool and destroy excess.
- **Likelihood**: Low for current use case. Only relevant if item count exceeds ~500.

### R2: SetData signature change (LOW)
- **Description**: `InventoryItemSlot.SetData` lost its `displayName` parameter. Any external caller (outside this file) that passes a custom display string would break.
- **Mitigation**: `InventoryItemSlot` is defined in the same file and only called from `InventorySearchPanel.RefreshList`. No external references exist. If the slot class were split into its own file and used elsewhere, this would need coordination.
- **Likelihood**: Very low. Single-file, single-consumer class.

### R3: Event-driven refresh may miss updates (LOW)
- **Description**: Removed `Update()` polling. If the underlying `Items` list is modified externally (e.g., by another system adding/removing mock data at runtime), the panel won't auto-refresh.
- **Mitigation**: The original `Update()` timer refreshed every 0.2s regardless of changes — this was the source of GC issue #1. The original design already relied on events for user input; the timer was never a correct fix for external data changes. If external data changes are needed, a proper event/notification from the data layer should be added.
- **Likelihood**: Low. Mock data is initialized in `Start()` and never modified.

### R4: String comparison culture differences (VERY LOW)
- **Description**: Original used `ToLower()` (culture-sensitive). New code uses `StringComparison.OrdinalIgnoreCase`. For ASCII item names ("Iron Sword", "Wood", etc.), behavior is identical. For non-ASCII characters (e.g., accented names), OrdinalIgnoreCase may differ from ToLower on some cultures.
- **Mitigation**: All mock item names are ASCII. OrdinalIgnoreCase is actually more predictable and performant.
- **Likelihood**: Very low. No non-ASCII item names exist.

### R5: No defensive null checks on UI references (PRE-EXISTING)
- **Description**: If `SearchInput`, `TypeDropdown`, `ContentRoot`, `SlotPrefab`, `DetailText`, or `CountText` are not assigned in the Unity Inspector, the code will throw NullReferenceException. This was true in the original code as well.
- **Mitigation**: Pre-existing issue. Not introduced by this fix. Standard Unity pattern expects inspector wiring.
- **Likelihood**: Low with correct scene setup.

### R6: Sort delegate held as static field (VERY LOW)
- **Description**: `NameComparison` is `static readonly`. In Unity, static fields survive domain reloads but are reset on assembly reload. This is standard and safe.
- **Mitigation**: None needed.
- **Likelihood**: Negligible.

### R7: Button.onClick.RemoveAllListeners persistence (PRE-EXISTING)
- **Description**: `SetData` calls `RemoveAllListeners()` then `AddListener(...)`, which creates a new closure each time. This was present in the original code and is not among the 7 target GC issues. Over many slot updates, this still accumulates some GC pressure.
- **Mitigation**: Pre-existing. A future optimization could cache the `UnityAction` or use a different event wiring pattern, but this is outside the scope of this fix.
- **Likelihood**: Low impact given event-driven refresh rate.

## Risk Summary

| Risk | Severity | Likelihood | Action |
|------|----------|-----------|--------|
| R1: Unbounded pool growth | Low | Low | Accept — bounded by backpack item count |
| R2: SetData signature change | Low | Very Low | Accept — no external callers |
| R3: Missed external updates | Low | Low | Accept — no external data changes in design |
| R4: Culture-sensitive string compare | Very Low | Very Low | Accept — ASCII-only data |
| R5: Null UI references | Medium | Low | Pre-existing — not introduced |
| R6: Static delegate field | Very Low | Negligible | Accept — standard pattern |
| R7: Button listener allocations | Low | Low | Pre-existing — out of scope |

## Overall Risk Level: LOW

All changes are surgical, well-understood, and verified by code review. No new architectural dependencies or external interfaces were introduced. The fixes strictly follow the SDD constraints.

---

## Independent Review Verification (2026-06-21)

A third-party review was conducted by reading the git diff (ab91790..3668f14), the fixed code, and all 4 reports. Findings:

- **No new risks identified**: All risks in R1-R7 remain accurate. The review confirmed:
  - R1: Pool grows on demand, no shrink — confirmed at lines 102-107. Appropriate for bounded backpack use.
  - R2: SetData signature change — confirmed no external callers. InventorySearchPanel is the sole consumer.
  - R3: Event-driven refresh — confirmed Update() removed. No external data mutation in current design.
  - R4: OrdinalIgnoreCase — confirmed replacement at line 88. ASCII-only item names.
  - R5: Null UI references — confirmed same Inspector-wired pattern as baseline. Pre-existing.
  - R6: Static delegate — confirmed `static readonly Comparison<InventoryItemData>` at line 45.
  - R7: Button listener closure — confirmed at lines 181-185. Pre-existing, out of scope.

- **No scope creep**: Only `InventorySearchPanel.cs` modified. See `SCOPE-CHECK.md`.

- **No fabricated results**: All TestReport assertions are code-review based and honestly marked UNVERIFIED for runtime.

- **Verdict**: APPROVED. All SDD acceptance criteria met. Risk level confirmed LOW.
