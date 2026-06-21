# REVIEW.md — Codex GC Fix: Independent Evidence-Based Verdict

**Review Date**: 2026-06-21
**Branch**: `feature/gc-fix-search-panel`
**Baseline**: `ab91790` (broken code with 7 known GC issues)
**Fix Commit**: `3668f14` (Codex fix)
**SDD**: `SDD-gc-fix.md`

---

## VERDICT: APPROVED

All four check categories pass. The fix correctly addresses all 7 GC issues at the code level, stays within the approved scope (single file), preserves all documented behaviors, and reports honestly about what can and cannot be verified without Unity runtime access. No blocking issues found.

---

## 1. SCOPE CREEP CHECK — PASS

| Check | Result | Evidence |
|-------|:------:|----------|
| Only InventorySearchPanel.cs modified? | **YES** | `git diff --name-only ab91790..3668f14` → exactly 1 file |
| Any new files created? | **NO** | `git ls-tree` identical between commits |
| Any .meta/.prefab/.asset changes? | **NO** | Only `M  Assets/Scripts/Inventory/InventorySearchPanel.cs` in diff |
| ObjectPool/Addressables/framework introduced? | **NO** | No new `using` statements, no new namespaces |
| InventoryItemData fields changed? | **NO** | Class definition byte-identical between commits |
| InventoryItemType enum changed? | **NO** | Enum definition byte-identical between commits |
| Other .cs files (6 in Inventory/) affected? | **NO** | All 6 unchanged |

**Conclusion**: Zero scope creep. Strictly complies with SDD Section 3 (non-goals) and Section 4 (allowed range). Full analysis in `SCOPE-CHECK.md`.

---

## 2. SURFACE-FIX CHECK — PASS

Each of the 7 GC issues from the SDD was checked against the actual fixed code at `Assets/Scripts/Inventory/InventorySearchPanel.cs`. Evidence comes from `git diff ab91790..3668f14` and direct code inspection of the fixed file.

### Fix #1: Update() timer → Event-driven

| Aspect | Baseline (ab91790) | Fix (3668f14) | Verdict |
|--------|-------------------|---------------|:------:|
| Update() method | Lines 42-48: `_refreshTimer += Time.deltaTime; if (_refreshTimer > 0.2f) ...` | **REMOVED** entirely. Comment at line 66-67 documents rationale. | **FIXED** |
| _refreshTimer field | Line 38: `private float _refreshTimer;` | **REMOVED** | **FIXED** |
| Refresh trigger | Polling every 0.2s (5 Hz idle) | Event-driven: `onValueChanged` + `OnClickItem` → 0 Hz idle | **FIXED** |

**Evidence**: Diff shows `-private float _refreshTimer;` and removal of entire `Update()` block. Fixed file lines 60-63 show only event listeners in Start(). No time-based polling remains.

### Fix #2: LINQ → for-loop + cached list

| Aspect | Baseline (ab91790) | Fix (3668f14) | Verdict |
|--------|-------------------|---------------|:------:|
| `using System.Linq` | Line 3: present | **REMOVED** (line 1 of diff) | **FIXED** |
| `.Where(...).OrderBy(...).ToList()` | Lines 56-61: chain call with lambdas | **REPLACED** with manual `for` loop (lines 80-92) + `_filteredResults.Sort(NameComparison)` (line 94) | **FIXED** |
| `.Any(item => ...)` | Line 73: LINQ Any with lambda | **REPLACED** with manual `for` loop with early break (lines 131-139) | **FIXED** |
| Per-refresh list allocation | `new List<InventoryItemData>` each call (implicit from ToList) | Cached `_filteredResults` with `Clear()` (line 75) | **FIXED** |
| Lambda allocation in Sort | `OrderBy(item => item.Name)` (new lambda per call) | `static readonly Comparison<InventoryItemData> NameComparison` (line 45-46) — zero allocation | **FIXED** |

**Evidence**: `-using System.Linq;` removed. No `.Where`, `.OrderBy`, `.ToList`, or `.Any` anywhere in fixed file. Verified via grep: zero LINQ method calls in file.

### Fix #3: ToLower().Contains() → IndexOf + OrdinalIgnoreCase

| Aspect | Baseline (ab91790) | Fix (3668f14) | Verdict |
|--------|-------------------|---------------|:------:|
| Case-insensitive substring search | `item.Name.ToLower().Contains(keyword.ToLower())` (line 59) — 2 temp strings per item | `item.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0` (line 88) — 0 temp strings | **FIXED** |
| Culture sensitivity | Culture-sensitive (ToLower varies by locale) | Ordinal (predictable, correct for ASCII names) | **IMPROVED** |

**Evidence**: Diff shows replacement. Fixed code line 88: `item.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0`. No `.ToLower()` or `.Contains()` in filtering path.

### Fix #4: Destroy/Instantiate → Slot reuse + SetActive

| Aspect | Baseline (ab91790) | Fix (3668f14) | Verdict |
|--------|-------------------|---------------|:------:|
| Destroy in RefreshList | Lines 52-55: `for (int i = ContentRoot.childCount - 1; i >= 0; i--) { Destroy(ContentRoot.GetChild(i).gameObject); }` — every refresh destroys ALL children | **REMOVED**. No Destroy calls anywhere in file. | **FIXED** |
| Instantiate frequency | Per-item per-refresh: `foreach (item in result) { Instantiate(SlotPrefab, ContentRoot); }` (line 65) | On-demand pool growth only: `while (_activeSlots.Count < resultCount) { Instantiate(...); }` (lines 102-107) — fires only when pool needs to grow | **FIXED** |
| Excess slot handling | N/A (everything destroyed) | `slot.gameObject.SetActive(false)` (line 124) — hides without destroy | **FIXED** |

**Evidence**: Diff confirms Destroy loop removed (lines `-Destroy(ContentRoot.GetChild(i).gameObject);`). Fixed code has zero `Destroy()` calls. Instantiate at line 104 only inside pool-grow `while` loop. SetActive(false) at line 124 for hidden slots.

### Fix #5: GetComponent per refresh → Cached references

| Aspect | Baseline (ab91790) | Fix (3668f14) | Verdict |
|--------|-------------------|---------------|:------:|
| GetComponent in refresh path | Line 66: `slotObj.GetComponent<InventoryItemSlot>()` — every Instantiate triggers a GetComponent | Line 105: `slotObj.GetComponent<InventoryItemSlot>()` — only on pool growth (first-time slot creation) | **FIXED** |
| Slot reference storage | N/A (discarded after refresh because destroyed) | `_activeSlots` List<InventoryItemSlot> (line 39) — component references cached | **FIXED** |
| Slot lookup in refresh | N/A | Direct list index: `_activeSlots[i]` (line 111) — zero GetComponent | **FIXED** |

**Evidence**: GetComponent only at line 105, inside the `while` pool-grow loop. Refresh loop (lines 109-126) accesses `_activeSlots[i]` which is already typed as `InventoryItemSlot`. No GetComponent per refresh.

### Fix #6: Debug.Log → Removed

| Aspect | Baseline (ab91790) | Fix (3668f14) | Verdict |
|--------|-------------------|---------------|:------:|
| Debug.Log call | Line 76: `Debug.Log($"Inventory refreshed at {DateTime.Now}, result count = {result.Count}");` — per-refresh string allocation | **REMOVED** (line 147 comment documents removal) | **FIXED** |

**Evidence**: Diff shows `-Debug.Log($"Inventory refreshed at {DateTime.Now}, result count = {result.Count}");`. No `Debug.Log`, `Debug.LogWarning`, or `Debug.LogError` anywhere in fixed file.

### Fix #7: Intermediate string interpolation → Internalized

| Aspect | Baseline (ab91790) | Fix (3668f14) | Verdict |
|--------|-------------------|---------------|:------:|
| displayName parameter | `SetData(item, $"{item.Name} x{item.Count} [{item.Type}]", selected, OnClickItem)` — string allocated in RefreshList, passed as parameter | **Parameter removed**. SetData signature: `SetData(item, selected, OnClickItem)` (line 171) | **FIXED** |
| Display string construction | Two places: RefreshList (interpolation) + SetData (assignment) | One place: inside SetData at line 176 | **FIXED** |
| OnClickItem string | `$"Name: {item.Name}\nType: {item.Type}\nCount: {item.Count}"` (interpolation) | `"Name: " + item.Name + "\nType: " + item.Type + "\nCount: " + item.Count` (concatenation, line 153) | **MINOR** |

**Evidence**: SetData signature changed. The `$\"{item.Name} x{item.Count} [{item.Type}]\"` expression removed from RefreshList. Display built at line 176 in SetData. Note: concatenation at line 153 still triggers `item.Type.ToString()` (enum boxing), but this matches the original code's allocation profile for OnClickItem (which fires only on user click, not per-refresh).

---

## 3. BEHAVIOR PRESERVATION CHECK — PASS

All behavior checks are based on code-path analysis of the fixed code. Runtime execution cannot be verified without Unity Editor (noted honestly in TestReport).

### Search (case-insensitive)

| Scenario | Code Path | Expected Behavior | Preserved? |
|----------|-----------|-------------------|:----------:|
| Exact match ("Iron Sword") | `IndexOf("Iron Sword", OrdinalIgnoreCase)` → 0 | Match found | **YES** |
| Case-insensitive ("iron sword") | `IndexOf("iron sword", OrdinalIgnoreCase)` → 0 | Match found | **YES** |
| Partial match ("Potion") | `IndexOf("Potion", OrdinalIgnoreCase)` → 7 ("Small HP Potion") | Both potions match | **YES** |
| No match ("zzz") | `IndexOf("zzz", OrdinalIgnoreCase)` → -1 | No results | **YES** |
| Empty search field | `filterByKeyword = false` (line 78) | Shows all items matching type only | **YES** |

**Code**: Line 78 (`bool filterByKeyword = !string.IsNullOrEmpty(keyword)`), line 88 (`IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0` → skip). Logic preserved; OrdinalIgnoreCase is functionally identical to ToLower for ASCII strings.

### Type dropdown filter

| Scenario | Code Path | Expected Behavior | Preserved? |
|----------|-----------|-------------------|:----------:|
| "All" (value=0) | `filterByType = false` (line 77) → type check skipped | All 6 items | **YES** |
| "Equipment" (value=1) | `item.Type != InventoryItemType.Equipment` → skip non-equipment | Iron Sword, Wood Shield | **YES** |
| "Consumable" (value=2) | `item.Type != InventoryItemType.Consumable` → skip non-consumable | Small HP Potion, Small MP Potion | **YES** |
| "Material" (value=3) | `item.Type != InventoryItemType.Material` → skip non-material | Iron Ore, Wood | **YES** |

**Code**: Line 72: `(InventoryItemType)TypeDropdown.value`, line 77: `bool filterByType = selectedType != InventoryItemType.All`, line 84: `if (filterByType && item.Type != selectedType) continue;`. Logic identical to baseline.

### Combined search + type filter

| Scenario | Code Path | Expected Behavior | Preserved? |
|----------|-----------|-------------------|:----------:|
| Search "iron" + Type "Equipment" | Type filter passes only Equipment; keyword filter passes only "Iron" substring | Iron Sword only | **YES** |
| Search "potion" + Type "Consumable" | Type filter passes Consumable; keyword passes "Potion" substring | 2 results | **YES** |

**Code**: Both conditions applied with AND logic (lines 84-89: both `continue` checks must be bypassed for item to be added). Logic identical to baseline LINQ `.Where(item => conditionA && conditionB)`.

### Click selection + detail display

| Scenario | Code Path | Expected Behavior | Preserved? |
|----------|-----------|-------------------|:----------:|
| Click sets _selectedItem | `OnClickItem`: `_selectedItem = item;` (line 152) | Field updated | **YES** |
| DetailText updates on click | Line 153: `DetailText.text = "Name: " + item.Name + "\nType: " + item.Type + "\nCount: " + item.Count;` | Shows name, type, count | **YES** |
| SelectedFrame shows on correct slot | Line 116: `bool selected = _selectedItem != null && _selectedItem.Id == item.Id;` → passed to SetData → `SelectedFrame.SetActive(selected)` (line 179) | Visual highlight | **YES** |

**Code**: OnClickItem at lines 150-155 identical structure to baseline (only string building method changed from interpolation to concatenation). SetData selection logic at lines 116, 179 unchanged.

### Selected item persistence across filter changes

| Scenario | Code Path | Expected Behavior | Preserved? |
|----------|-----------|-------------------|:----------:|
| Selected item stays if still in results | `_selectedItem` retained; line 116: `_selectedItem.Id == item.Id` → true | Selection visible | **YES** |
| Selected item clears when filtered out | Lines 131-139: manual loop checks all `_filteredResults`; if ID not found → `_selectedItem = null`, `DetailText.text = "No item selected"` | Cleared | **YES** |

**Code**: Selection check at line 116 unchanged. Clear-check at lines 128-145 uses manual for-loop instead of LINQ `Any`, but logic is identical: iterate all results, early exit on match, clear if not found.

### CountText display

| Scenario | Code Path | Expected Behavior | Preserved? |
|----------|-----------|-------------------|:----------:|
| Shows "Count: X / Y" format | Line 96: `CountText.text = "Count: " + _filteredResults.Count + " / " + Items.Count;` | X = filtered, Y = total | **YES** |

**Code**: Line 96 identical format to baseline line 62. Uses `_filteredResults.Count` (was `result.Count`).

---

## 4. REPORT HONESTY CHECK — PASS

| Check | Result | Evidence |
|-------|:------:|----------|
| All runtime tests marked UNVERIFIED? | **YES** | TestReport.md lines 7-8: "UNVERIFIED — Unity Editor not available". Lines 16, 24, 33, 40, 49, 57: all 6 functional test tables show "UNVERIFIED" status. |
| GC code-review tests correctly labeled? | **YES** | TestReport.md lines 68-74: 7 GC items marked "VERIFIED (code)" — they distinguish code-level verification from runtime Profiler verification. This is honest. |
| Any "0 GC Alloc" claim without Profiler? | **NO** | No report claims "zero GC allocations." TestReport correctly states "Profiler: UNVERIFIED — cannot capture Profiler data" (line 8). RiskReport describes allocation reductions qualitatively ("Zero allocation for filtering/sorting", "Eliminates per-item string allocation") but never claims application-wide 0 GC. |
| Any fabricated Profiler data? | **NO** | No Profiler screenshots, no frame capture data, no specific byte-count allocations claimed. All assertions are code-review based. |
| Remaining allocations acknowledged? | **YES** | TestReport lines 78-83 list 4 remaining GC allocations (Button closure, Type.ToString, input property access, CountText concatenation). RiskReport R7 explicitly acknowledges the Button listener closure. |

---

## MINOR OBSERVATIONS (Non-blocking)

These are items noticed during review that do NOT block approval but are worth noting:

### M1: Button.onClick.RemoveAllListeners + AddListener(lambda) in SetData (line 181-185)

**Status**: PRE-EXISTING. Present in baseline code. Creates a new closure (`() => { _onClick?.Invoke(_item); }`) on every slot update. Not among the 7 target GC issues. Acknowledged in RiskReport R7. Low impact with event-driven refresh (only fires on actual user interaction, not 5 Hz polling).

### M2: item.Type.ToString() in SetData (line 178)

**Status**: PRE-EXISTING. Enum.ToString() allocates. Present in baseline. Not among the 7 target GC issues. Called once per visible slot per refresh. Acceptable given event-driven refresh rate. Acknowledged in TestReport line 81.

### M3: String concatenation produces intermediate strings (lines 96, 153, 176)

**Status**: ACCEPTABLE. These concatenations happen once per refresh (CountText, line 96), once per click (DetailText, line 153), and once per visible slot (NameText, line 176). Under event-driven refresh, this is negligible compared to the old 5 Hz polling. Not among the 7 target issues. The SDD explicitly allows string concatenation that isn't in a hot loop.

### M4: Sort delegate is static readonly (lines 45-46)

**Status**: CORRECT. `static readonly Comparison<InventoryItemData> NameComparison` is the correct pattern to avoid per-call delegate allocation. Acknowledged in RiskReport R6 as VERY LOW risk (safe pattern). Note: `static` in Unity survives domain reload but is reset on assembly reload — standard and safe.

---

## COMPLIANCE MATRIX (SDD Section 5)

| SDD Acceptance Criterion | Status | Evidence |
|--------------------------|:------:|----------|
| 移除 Update 无条件刷新 | **PASS** | Update() method removed. Event-driven via onValueChanged + OnClickItem. |
| 移除 LINQ（for 循环替代） | **PASS** | `using System.Linq` removed. Zero LINQ calls remain. Manual for-loops + static Sort delegate. |
| 移除 ToLower（IndexOf + OrdinalIgnoreCase） | **PASS** | `IndexOf(keyword, StringComparison.OrdinalIgnoreCase)` at line 88. |
| 避免 Destroy/Instantiate（复用 + SetActive） | **PASS** | Zero Destroy(). Instantiate only in pool-grow path. SetActive(false) for hidden slots. |
| 避免重复 GetComponent（缓存引用） | **PASS** | GetComponent only at line 105 (pool growth). Cached as InventoryItemSlot references in _activeSlots. |
| 移除高频 Debug.Log | **PASS** | Debug.Log removed entirely. |
| 搜索、筛选、选中、详情保持原有行为 | **PASS** | All code paths verified identical logic (see Section 3). |
| 无越权修改 | **PASS** | Only InventorySearchPanel.cs modified. See SCOPE-CHECK.md. |
| 4 份报告齐全 | **PASS** | TestReport.md, RiskReport.md, ChangedFiles.md, REVIEW.md all present. |

---

## FINAL VERDICT: APPROVED

The fix correctly and completely addresses all 7 GC allocation issues identified in the SDD. It stays within the approved scope (single file), introduces no new dependencies, preserves all documented functional behaviors, and reports honestly about verification limitations. No blocking issues found.
