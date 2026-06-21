# RiskReport.md — Backpack Filter UI Risk Analysis

---

## Risk Category 1: Editor Script Dependency & Scene Integrity

### Risk Description

The Demo scene (`BackpackDemo.unity`) contains only a Main Camera and Directional Light. The full UI hierarchy (Canvas, Panel, filter buttons, ScrollView, DetailPanel, InventoryPanelController) is generated at Editor time by `BackpackSceneSetup.cs` via the **Tools > Setup Backpack UI** menu item. This means:

- The scene is **not self-contained** — it requires a manual Editor step before it becomes functional.
- Opening the scene and pressing Play without running the setup will produce no visible UI (or NullReferenceException errors from missing serialized field references).
- The scene must be **manually saved** after running the setup script to persist the generated GameObjects.

### Why It Matters

The SDD §8 T4 states:

> "创建 Demo 场景（Canvas + Panel + 按钮 + 详情文本 + Slot prefab）"

This implies the scene file itself should contain the UI hierarchy. The Editor script approach deviates from this expectation. While the REVIEW.md frames this as "intentional and documented" (to avoid hand-crafted YAML fragility), it introduces a **mandatory manual step** that:
- Any new team member must discover and execute
- Could be forgotten, leading to confusion
- Breaks the "clone and run" expectation for demo projects

### Specific Sub-Risks

| # | Risk | Severity | Likelihood |
|---|---|---|---|
| 1a | User opens scene, presses Play, sees empty screen | Medium | High (if setup not run) |
| 1b | Prefab reference (`slotPrefab`) lost if scene not saved after setup | Medium | Medium |
| 1c | Editor script run twice creates duplicate GameObjects (no cleanup of existing UI) | Low | Low (uses `GameObject.Find` for controller, but Canvas/Panel/etc. created unconditionally) |
| 1d | `SlotPrefab.prefab` reference breaks if prefab is moved/deleted after scene save | Medium | Low |
| 1e | Folder creation mismatch: creates `Assets/Prefabs/` instead of `Assets/Scripts/Inventory/` if the Inventory folder doesn't exist (line 53) | Low | Very Low (Inventory folder exists by the time setup runs) |

### Mitigation

- Document the setup step prominently (already done in REVIEW.md)
- Consider baking the UI into the scene YAML as a one-time commit after running the setup script, so the scene is self-contained
- Add idempotency: check for existing Canvas/Panel before creating

---

## Risk Category 2: Reflection-Based Serialized Field Wiring

### Risk Description

`BackpackSceneSetup.cs` wires all `[SerializeField]` private fields on `InventoryPanel` and `InventorySlotView` using `SerializedObject.FindProperty()` and string-based field names:

```csharp
SetPrivateField(panel, "slotPrefab", slotPrefab);
SetPrivateField(panel, "contentParent", contentParent);
SetPrivateField(panel, "detailText", dt);
SetPrivateField(panel, "buttonAll", buttonAll);
// ... etc
SetPrivateField(slotView, "nameText", nameText);
SetPrivateField(slotView, "typeText", typeText);
SetPrivateField(slotView, "countText", countText);
SetPrivateField(slotView, "selectedBackground", selImage);
```

### Why It Matters

- **Field renames break silently.** If a developer renames `slotPrefab` to `itemSlotPrefab` in `InventoryPanel.cs`, the Editor script will log a warning (`"Could not find serialized field..."`) but **will not produce a compile error**. The field will remain null at runtime.
- **No compile-time safety.** Unlike direct assignment (`panel.slotPrefab = slotPrefab`), string-based reflection has zero compiler validation.
- **Type mismatches are runtime-only.** `SetPrivateField` only handles `GameObject`, `Component`, and `null` types (lines 513-520). If a field type changes to something else (e.g., a custom class), it silently fails.
- **This pattern exists for 10 field assignments** across two components, creating 10 potential silent failure points.

### Specific Sub-Risks

| # | Risk | Severity | Likelihood |
|---|---|---|---|
| 2a | Developer renames a serialized field → runtime NullReferenceException | High | Medium |
| 2b | Field type changed from Component to something else → silent wiring skip | Medium | Low |
| 2c | `SerializedObject.ApplyModifiedProperties()` fails silently (scene dirty but field not saved) | Low | Very Low |

### Mitigation

- Add a post-setup validation step in the Editor script that verifies all wired fields are non-null
- Consider adding `[FormerlySerializedAs]` attributes when renaming fields
- Add XML comments on serialized fields noting they are wired by `BackpackSceneSetup`
- Add unit/editor tests that run the setup and assert field values

---

## Risk Category 3: Legacy Unity UI (Non-TMP) & Runtime Compatibility

### Risk Description

The implementation uses Unity's legacy `UnityEngine.UI.Text` component throughout — for the title, filter button labels, slot name/type/count, and detail text. It also uses `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` as the font source.

### Why It Matters

- **Text (legacy) is deprecated** in Unity 2022.3 in favor of TextMeshPro (TMP). While it still works, it may produce warnings and will eventually be removed.
- **`Resources.GetBuiltinResource<Font>` may fail in standalone builds.** The "LegacyRuntime.ttf" built-in resource is typically Arial, but its availability in IL2CPP or non-Editor builds is not guaranteed. Some build configurations strip built-in resources.
- **No font fallback.** If the built-in resource is unavailable, all text will render with Unity's default font (which may not support Chinese characters, causing tofu/boxes for the mock item names like "铁剑", "小血瓶", etc.).
- **No error handling.** The font lookup on lines like `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` has no null check — if it returns null, Text components will silently fail to render.

### Specific Sub-Risks

| # | Risk | Severity | Likelihood |
|---|---|---|---|
| 3a | Chinese characters render as tofu/boxes in standalone build | High | Medium |
| 3b | Console warnings about deprecated Text API in Unity 2022.3+ | Low | High |
| 3c | Font asset missing in IL2CPP build → all text invisible | High | Low |

### Mitigation

- Migrate to TextMeshPro (TMP) with a font asset that includes CJK character coverage
- Include a fallback font asset in the project
- Add null checks after `Resources.GetBuiltinResource<Font>()` calls

---

## Risk Category 4: Runtime Null-Reference Resilience

### Risk Description

Both `InventoryPanel` and `InventorySlotView` have `[SerializeField]` fields that are populated by the Editor setup script. If these fields are null at runtime (setup script not run, scene not saved, or reference lost), the code has inconsistent null handling:

- **InventorySlotView**: `Setup()` null-checks `nameText`, `typeText`, `countText` before assignment. Silent degradation (text doesn't update, but no crash).
- **InventoryPanel**: `Start()` calls `buttonAll.onClick.AddListener(...)` without null-checking `buttonAll`. If any filter button is null, this is a **NullReferenceException** and the panel will not initialize.
- **InventoryPanel**: `RefreshList()` uses `slotPrefab` to `Instantiate()` — null here causes a NullReferenceException.
- **InventoryPanel**: `UpdateDetailDisplay()` null-checks `detailText` (line 149: `if (detailText == null) return;`), so missing detail text degrades gracefully.

### Specific Sub-Risks

| # | Risk | Severity | Likelihood |
|---|---|---|---|
| 4a | Filter button null → NullReferenceException in Start() | High | Medium (if setup not run) |
| 4b | slotPrefab null → NullReferenceException in RefreshList() | High | Medium (if setup not run) |
| 4c | contentParent null → Instantiate with null parent (places at scene root) | Medium | Low |
| 4d | Inconsistent null-handling philosophy across the codebase | Low | N/A (design observation) |

### Mitigation

- Add `Awake()` validation in `InventoryPanel` that logs errors for null serialized fields instead of crashing
- Or add `[Required]` / `Debug.Assert` guards at the top of `Start()`

---

## Risk Category 5: Idempotency & Editor Script Re-Execution

### Risk Description

The Editor script `BuildUI()` calls several creation methods unconditionally:

- `CreateCanvas()` — calls `Object.FindObjectOfType<Canvas>()` first (line 25), so it's idempotent
- `CreatePanel()`, `CreateTitle()`, `CreateFilterButtons()`, `CreateScrollView()`, `CreateDetailPanel()` — these are called **every time** the menu item is invoked, creating **duplicate GameObjects** on re-run
- `CreateController()` — uses `GameObject.Find("InventoryPanelController")` (line 448), so the controller GameObject is idempotent, but its child references are re-wired each time
- `CreateSlotPrefab()` — deletes and recreates the prefab each time

Running **Tools > Setup Backpack UI** twice without reverting the scene will produce duplicate UI elements (two PanelBg, two TitleText, two ScrollViews, etc.).

### Mitigation

- Add checks before creating each UI element (e.g., `panelBg.Find("TitleText")` before creating)
- Or add a "Cleanup Existing UI" step at the start of `BuildUI()`

---

## Summary

| Risk Category | Overall Severity | Primary Concern |
|---|---|---|
| 1. Editor Script Dependency | **Medium** | Scene not self-contained; manual step required |
| 2. Reflection-Based Wiring | **Medium** | Field renames cause silent runtime failures |
| 3. Legacy UI (non-TMP) | **Medium** | Chinese text may not render in builds |
| 4. Null-Reference Resilience | **Medium** | Missing setup → hard crash in Start() |
| 5. Editor Script Idempotency | **Low** | Duplicate UI on re-run; recoverable |

**Overall Assessment:** The code meets the functional requirements of the SDD, but the Editor-script approach introduces operational risks that the original SDD did not anticipate. The reflection-based wiring pattern is the most fragile technical choice. None of these risks block the demo's functionality when the setup is run correctly, but they represent debt for production use or team handoff.
