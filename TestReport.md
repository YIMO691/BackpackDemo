# TestReport.md — InventorySearchPanel GC Fix

## Test Environment

- **Unity Version**: 2022.3 (project configured)
- **Test Runner**: UNVERIFIED — Unity Editor not available in this environment
- **Profiler**: UNVERIFIED — cannot capture Profiler data

## Functional Tests

All functional tests below are marked **UNVERIFIED** because the Unity Editor cannot be executed in this environment. The following analysis is based on code review of the modified logic.

### Test 1: Search by name (case-insensitive)

| Aspect | Status |
|--------|--------|
| Exact match (e.g., "Iron Sword") | UNVERIFIED — code uses IndexOf with OrdinalIgnoreCase, should match |
| Case-insensitive (e.g., "iron sword") | UNVERIFIED — OrdinalIgnoreCase handles this correctly |
| Partial match (e.g., "Potion") | UNVERIFIED — IndexOf finds substring, returns 2 items (Small HP, Small MP) |
| No match (e.g., "zzz") | UNVERIFIED — should return empty list |
| Empty search field | UNVERIFIED — filterByKeyword=false, shows all items matching type filter |

### Test 2: Type dropdown filter

| Aspect | Status |
|--------|--------|
| "All" shows all items | UNVERIFIED — filterByType=false, all items pass type check |
| "Equipment" shows Iron Sword, Wood Shield | UNVERIFIED — type comparison correct |
| "Consumable" shows Small HP Potion, Small MP Potion | UNVERIFIED — type comparison correct |
| "Material" shows Iron Ore, Wood | UNVERIFIED — type comparison correct |

### Test 3: Combined search + type filter

| Aspect | Status |
|--------|--------|
| Search "iron" + Type "Equipment" = Iron Sword | UNVERIFIED — both filters applied with AND logic |
| Search "potion" + Type "Consumable" = 2 results | UNVERIFIED |

### Test 4: Item click selection

| Aspect | Status |
|--------|--------|
| Click item sets _selectedItem | UNVERIFIED — OnClickItem sets field and refreshes |
| DetailText updates on click | UNVERIFIED — DetailText.text set with Name/Type/Count |
| SelectedFrame shows on correct slot | UNVERIFIED — `selected` bool computed from _selectedItem.Id == item.Id |

### Test 5: CountText display

| Aspect | Status |
|--------|--------|
| Shows "Count: X / Y" format | UNVERIFIED — string concatenation preserved |
| X matches filtered count | UNVERIFIED — uses _filteredResults.Count |
| Y matches total Items count | UNVERIFIED — uses Items.Count |

### Test 6: Selected item persistence across filter changes

| Aspect | Status |
|--------|--------|
| Selected item stays selected if still in results | UNVERIFIED — _selectedItem retained; selection check passes if ID matches |
| Selected item clears when filtered out | UNVERIFIED — manual loop checks all results; if not found, _selectedItem=null, DetailText="No item selected" |

## GC Test Items

All marked **UNVERIFIED** — no Profiler access.

| GC Issue | Verification Method | Status |
|----------|-------------------|--------|
| #1: No Update() timer | Code review — Update() method removed | VERIFIED (code) |
| #2: No LINQ | Code review — `using System.Linq` removed, no LINQ calls remain | VERIFIED (code) |
| #3: No ToLower() | Code review — `IndexOf(..., OrdinalIgnoreCase)` used | VERIFIED (code) |
| #4: No Destroy in refresh | Code review — Destroy loop removed, SetActive used | VERIFIED (code) |
| #5: No GetComponent per refresh | Code review — GetComponent only in pool-grow path | VERIFIED (code) |
| #6: No Debug.Log | Code review — line removed | VERIFIED (code) |
| #7: No intermediate string in RefreshList | Code review — SetData signature changed | VERIFIED (code) |

## Remaining GC Allocations (not in scope of this fix)

These allocations still exist but were NOT among the 7 targets:

- `Button.onClick.RemoveAllListeners()` + `AddListener(...)` in `SetData` — closure allocation per slot update
- `item.Type.ToString()` in `SetData` — enum to string allocation
- `SearchInput.text` / `TypeDropdown.value` property access — may allocate depending on Unity version
- `CountText.text` string concatenation — once per refresh, acceptable with event-driven model

## Conclusion

All 7 GC issues have been addressed in code. Functional equivalence to the original behavior has been preserved based on code review. Runtime verification with Unity Profiler is **not possible** in this environment — all runtime assertions are marked UNVERIFIED.
