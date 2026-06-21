# TestReport.md — Backpack Filter UI Verification

## Scope

Static code review only (no Unity Editor available). This report distinguishes what can be verified by reading source code vs. what requires a running Unity Editor instance.

---

## 1. Verifiable by Code Review (Static Analysis)

### 1.1 Data Model

| Check | Status | Notes |
|---|---|---|
| ItemType enum has 3 values (Equipment, Consumable, Material) | PASS | `ItemType.cs:3-8`, matches SDD §6 |
| ItemData is [Serializable], plain class, no MonoBehaviour | PASS | `ItemData.cs:5-6`, matches SDD §6 |
| ItemData fields: Name (string), Type (ItemType), Count (int) | PASS | `ItemData.cs:8-10`, matches SDD §6 |
| InventoryMockData.GetItems() returns 6 items | PASS | `InventoryMockData.cs:7-15` |
| Mock item names/types/counts match SDD exactly | PASS | 铁剑/Equipment/1, 木盾/Equipment/1, 小血瓶/Consumable/5, 小蓝瓶/Consumable/3, 铁矿石/Material/12, 木材/Material/20 |
| Mock data is standalone (not mixed into production code) | PASS | Separate `InventoryMockData.cs` file, static class |

### 1.2 Filter Logic

| Check | Status | Notes |
|---|---|---|
| "全部" button passes null filter (show all) | PASS | `InventoryPanel.cs:37` → `OnFilterClicked(null)` |
| Equipment button passes ItemType.Equipment | PASS | `InventoryPanel.cs:38` |
| Consumable button passes ItemType.Consumable | PASS | `InventoryPanel.cs:39` |
| Material button passes ItemType.Material | PASS | `InventoryPanel.cs:40` |
| Filter delegates to RefreshList which applies predicate | PASS | `InventoryPanel.cs:69-78` |
| AllItems copied to new list on "show all" (no mutation) | PASS | `InventoryPanel.cs:78` — `new List<ItemData>(allItems)` |

### 1.3 Selection Logic

| Check | Status | Notes |
|---|---|---|
| Single selection: SelectItem replaces previous | PASS | `InventoryPanel.cs:117-125`, `selectedItem = item` overwrites |
| Click same item → no-op (early return) | PASS | `InventoryPanel.cs:139-143`, `OnSlotClicked` returns if `selectedItem == item` |
| ClearSelection sets selectedItem to null | PASS | `InventoryPanel.cs:130-135` |
| Duplicate click on already-selected slot → no RefreshList call | PASS | `OnSlotClicked` returns before calling `SelectItem` |

### 1.4 Filter + Selection Interaction

| Check | Status | Notes |
|---|---|---|
| Filter to category not containing selection → selection cleared | PASS | `InventoryPanel.cs:54-59`: checks `selectedItem.Type != filter.Value` |
| Filter to category containing selection → selection preserved | PASS | `InventoryPanel.cs:103`: `selectedItem == item` reference equality, item remains in filtered list |
| Filter to "全部" → selection preserved | PASS | `filter.HasValue` is false, skips the clear check |
| After clear, detail shows "未选择道具" | PASS | `InventoryPanel.cs:164` |
| After select, detail shows Name + Type + Count | PASS | `InventoryPanel.cs:160` |

### 1.5 Performance / Architecture Compliance

| Check | Status | Notes |
|---|---|---|
| Slot pool reuses GameObjects (SetActive, no Destroy) | PASS | `InventoryPanel.cs:87-111` — Instantiate only when pool too small, SetActive for show/hide |
| No Destroy/Instantiate on filter change | PASS | RefreshList only calls SetActive, never Destroy |
| Filter logic centralized in InventoryPanel | PASS | All filtering in `OnFilterClicked`/`RefreshList` |
| Selection state centralized in InventoryPanel | PASS | `selectedItem` field is single source of truth |
| UI logic and data separated | PASS | ItemData (pure data) vs InventoryPanel/InventorySlotView (UI) |
| No global singletons | PASS | No `Instance` pattern, no `DontDestroyOnLoad` |
| No large Manager class | PASS | InventoryPanel is 168 lines, focused |
| No LINQ chains | PASS | Uses `List<T>.FindAll` (predicate) — no LINQ allocations |
| No string concatenation in loops | PASS | String interpolation only in UpdateDetailDisplay (single call per selection) |

### 1.6 InventorySlotView

| Check | Status | Notes |
|---|---|---|
| Null-safe text field assignments | PASS | `nameText != null`, `typeText != null`, `countText != null` guards |
| Button auto-added if missing | PASS | `InventorySlotView.cs:30-33` |
| Selected visual updates on Setup | PASS | `UpdateSelectedVisual(isSelected)` called at end of Setup |
| Type display uses Chinese labels | PASS | switch expression maps Equipment→"装备", Consumable→"消耗品", Material→"材料" |

### 1.7 Scope Check (No Unauthorized Changes)

| Check | Status | Notes |
|---|---|---|
| Only Inventory/ and Scenes/ touched | PASS | Git diff confirms: 18 files, all within allowed paths |
| ProjectSettings/ untouched | PASS | Not in git diff |
| Packages/ untouched | PASS | Not in git diff |
| Library/ untouched | PASS | Not in git diff |
| No drag-and-drop implemented | PASS | None found |
| No network/save/backend | PASS | None found |
| No Addressables | PASS | None found |
| No new UI framework | PASS | Uses Unity built-in UGUI only |

---

## 2. NOT Verifiable Without Unity Editor

These items are **honestly unknown** and marked as UNVERIFIED:

| # | Item | Requires | Why Blocked |
|---|---|---|---|
| 1 | **Compilation (0 errors)** | Unity Editor | C# files reference `UnityEngine`, `UnityEngine.UI`, `UnityEditor` — these assemblies only exist inside Unity. Cannot compile with `dotnet build` or `csc`. |
| 2 | **Editor script execution** (`Tools > Setup Backpack UI`) | Unity Editor | The script uses `MenuItem`, `PrefabUtility`, `AssetDatabase`, `SerializedObject` — all Editor-only APIs. |
| 3 | **SlotPrefab.prefab creation** | Editor script run | Prefab is generated at Editor time; does not exist in repo. |
| 4 | **Serialized field wiring** | Editor script run | `SetPrivateField` uses `SerializedObject.FindProperty` — must run in Editor to verify fields resolve correctly. |
| 5 | **Runtime Play mode behavior** | Unity Editor | Button clicks, scroll rect, layout, selection visuals all require Play mode. |
| 6 | **EventSystem behavior** | Play mode | `EnsureEventSystem()` creates one if missing, but input routing needs runtime. |
| 7 | **Canvas rendering** | Play mode | ScreenSpaceOverlay Canvas with ScaleWithScreenSize — visual correctness unknown. |
| 8 | **Console error-free** | Play mode | No runtime logs can be checked. |
| 9 | **Scene save persistence** | Unity Editor | Prefab references (e.g., `slotPrefab` in InventoryPanel) persist only after scene is saved. |

---

## 3. Editor Script vs. Scene Reality

### What the SDD Says (T4)

> "创建 Demo 场景（Canvas + Panel + 按钮 + 详情文本 + Slot prefab）"

The SDD implies the scene file should contain the Canvas and UI hierarchy.

### What Actually Exists

The scene file (`BackpackDemo.unity`, 309 lines of YAML) contains exactly:
- **Main Camera** (fileID 23983930) with Transform, Camera, AudioListener
- **Directional Light** (fileID 961786223) with Transform, Light

**No Canvas. No Panel. No buttons. No scroll view. No detail panel.** The scene at rest is a bare Unity default scene.

### How UI Gets Created

The 530-line Editor script `BackpackSceneSetup.cs` builds the entire UI hierarchy at Editor time when the user runs **Tools > Setup Backpack UI**. It:
1. Creates `SlotPrefab.prefab` in `Assets/Scripts/Inventory/`
2. Creates Canvas, PanelBg, Title, FilterButtons row, ScrollView, DetailPanel
3. Creates InventoryPanelController GameObject, attaches InventoryPanel component
4. Wires all serialized field references via `SerializedObject.FindProperty`
5. Ensures EventSystem exists

### Gap

The scene file does not match what the SDD describes for T4. The UI is not "in" the scene — it is generated by a tool. This is a one-time setup step, but:

- Opening the scene fresh in Unity will show only Camera + Light
- Running Play mode before running the menu item will show nothing (or errors)
- The scene must be manually saved after running the setup script (noted in the script's log message on line 46)

---

## 4. Summary

| Category | Count | Status |
|---|---|---|
| Code review checks passed | 28 | PASS |
| Code review checks failed | 0 | — |
| Requires Unity Editor to verify | 9 | UNVERIFIED |
| Compilation verified | 0 | NOT POSSIBLE |

**Bottom line:** The code reads correctly for all SDD requirements. All logic paths (filter, select, clear, edge cases) are implemented. Architecture constraints are met. However, **nothing has been compiled or run**. The Editor script approach means the scene is not self-contained — it requires a manual setup step before first use.
