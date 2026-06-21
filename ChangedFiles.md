# ChangedFiles.md — InventorySearchPanel GC Fix

## Commit

```
fix: GC optimization for InventorySearchPanel (event-driven + for-loop + slot reuse + component cache)
Branch: feature/gc-fix-search-panel
Commit: 3668f14
```

## Files Changed

| File | Change | Description |
|------|--------|-------------|
| `Assets/Scripts/Inventory/InventorySearchPanel.cs` | MODIFIED | All 7 GC fixes applied (see below) |

**No other files were created, modified, or deleted.**

## Change Summary (InventorySearchPanel.cs)

| # | Fix | Lines Changed | Details |
|---|-----|--------------|---------|
| 1 | Remove Update() timer | -9 lines (removed Update method) | Event-driven via onValueChanged + OnClickItem |
| 2 | Replace LINQ with for-loop | -5 lines LINQ, +20 lines manual loop + Sort | Cached `_filteredResults` list, static `NameComparison` delegate |
| 3 | Replace ToLower().Contains() | 1 line changed | `IndexOf(keyword, StringComparison.OrdinalIgnoreCase)` |
| 4 | Slot reuse instead of Destroy/Instantiate | Rewrote slot management section | Destroy loop removed; grow-on-demand pool with SetActive |
| 5 | Cache component references | Integrated with #4 | `_activeSlots` list of `InventoryItemSlot` (not GameObject) |
| 6 | Remove Debug.Log | -1 line | Removed entirely |
| 7 | Remove intermediate string interpolation | Changed SetData signature | `displayName` parameter removed; display built inside SetData |

**Net diff**: 77 insertions, 31 deletions (file grew from 142 to 188 lines).

## Lines of Code

- **Before**: 142 lines
- **After**: 188 lines
- **Delta**: +46 lines (comments + pool management + manual loops)

## Removed Import

- `using System.Linq;` — no longer needed
