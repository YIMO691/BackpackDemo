using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory.Editor
{
    /// <summary>
    /// Editor utility to build the Backpack Demo UI scene.
    /// Select Tools > Setup Backpack UI to create/rebuild the entire UI hierarchy.
    /// This avoids hand-crafted YAML fragility.
    /// </summary>
    public static class BackpackSceneSetup
    {
        private const string PrefabPath = "Assets/Scripts/Inventory/SlotPrefab.prefab";
        private const string PrefabDir = "Assets/Scripts/Inventory";

        [MenuItem("Tools/Setup Backpack UI")]
        public static void BuildUI()
        {
            // 1. Create slot prefab
            GameObject slotPrefab = CreateSlotPrefab();

            // 2. Find or create Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            // 3. Build UI under Canvas
            RectTransform panelBg = CreatePanel(canvas.transform);
            CreateTitle(panelBg);
            CreateFilterButtons(panelBg);
            RectTransform scrollContent = CreateScrollView(panelBg);
            RectTransform detailPanel = CreateDetailPanel(panelBg);

            // 4. Create controller
            InventoryPanel controller = CreateController(slotPrefab, scrollContent, detailPanel, panelBg);

            // 5. Wire filter buttons
            WireFilterButtons(panelBg, controller);

            // 6. Ensure EventSystem
            EnsureEventSystem();

            Debug.Log("[BackpackSceneSetup] UI build complete. Save the scene to persist changes.");
            Selection.activeGameObject = controller.gameObject;
        }

        private static GameObject CreateSlotPrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabDir))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            // Delete existing if present
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(PrefabPath);
                AssetDatabase.Refresh();
            }

            // Root GameObject
            GameObject root = new GameObject("SlotPrefab", typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");

            // White background Image
            Image bgImage = root.AddComponent<Image>();
            bgImage.color = Color.white;

            // Button
            Button button = root.AddComponent<Button>();
            button.targetGraphic = bgImage;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            // InventorySlotView
            InventorySlotView slotView = root.AddComponent<InventorySlotView>();

            // LayoutElement for height
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.minHeight = 50;
            layout.preferredHeight = 50;

            // --- Selected Indicator (overlay) ---
            GameObject selectedIndicator = new GameObject("SelectedIndicator", typeof(RectTransform));
            selectedIndicator.transform.SetParent(root.transform, false);
            selectedIndicator.layer = LayerMask.NameToLayer("UI");
            Image selImage = selectedIndicator.AddComponent<Image>();
            selImage.color = new Color(1f, 1f, 0.6f, 0.5f);
            selImage.raycastTarget = false;
            RectTransform selRT = selectedIndicator.GetComponent<RectTransform>();
            selRT.anchorMin = Vector2.zero;
            selRT.anchorMax = Vector2.one;
            selRT.offsetMin = Vector2.zero;
            selRT.offsetMax = Vector2.zero;

            // --- Name Text ---
            GameObject nameGo = new GameObject("NameText", typeof(RectTransform));
            nameGo.transform.SetParent(root.transform, false);
            nameGo.layer = LayerMask.NameToLayer("UI");
            Text nameText = nameGo.AddComponent<Text>();
            nameText.text = "ItemName";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 16;
            nameText.color = Color.black;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.raycastTarget = false;
            RectTransform nameRT = nameGo.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0);
            nameRT.anchorMax = new Vector2(0.5f, 1);
            nameRT.offsetMin = new Vector2(10, 0);
            nameRT.offsetMax = new Vector2(0, 0);

            // --- Type Text ---
            GameObject typeGo = new GameObject("TypeText", typeof(RectTransform));
            typeGo.transform.SetParent(root.transform, false);
            typeGo.layer = LayerMask.NameToLayer("UI");
            Text typeText = typeGo.AddComponent<Text>();
            typeText.text = "Type";
            typeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            typeText.fontSize = 14;
            typeText.color = new Color(0.3f, 0.3f, 0.3f);
            typeText.alignment = TextAnchor.MiddleCenter;
            typeText.raycastTarget = false;
            RectTransform typeRT = typeGo.GetComponent<RectTransform>();
            typeRT.anchorMin = new Vector2(0.5f, 0);
            typeRT.anchorMax = new Vector2(0.75f, 1);
            typeRT.offsetMin = new Vector2(0, 0);
            typeRT.offsetMax = new Vector2(0, 0);

            // --- Count Text ---
            GameObject countGo = new GameObject("CountText", typeof(RectTransform));
            countGo.transform.SetParent(root.transform, false);
            countGo.layer = LayerMask.NameToLayer("UI");
            Text countText = countGo.AddComponent<Text>();
            countText.text = "0";
            countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countText.fontSize = 16;
            countText.color = Color.black;
            countText.alignment = TextAnchor.MiddleRight;
            countText.raycastTarget = false;
            RectTransform countRT = countGo.GetComponent<RectTransform>();
            countRT.anchorMin = new Vector2(0.75f, 0);
            countRT.anchorMax = new Vector2(1, 1);
            countRT.offsetMin = new Vector2(0, 0);
            countRT.offsetMax = new Vector2(-10, 0);

            // Wire up InventorySlotView serialized fields via reflection
            SetPrivateField(slotView, "nameText", nameText);
            SetPrivateField(slotView, "typeText", typeText);
            SetPrivateField(slotView, "countText", countText);
            SetPrivateField(slotView, "selectedBackground", selImage);

            // Save prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[BackpackSceneSetup] Slot prefab created at {PrefabPath}");
            return prefab;
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 600);

            return canvas;
        }

        private static RectTransform CreatePanel(Transform parent)
        {
            GameObject panelGo = new GameObject("PanelBg", typeof(RectTransform));
            panelGo.transform.SetParent(parent, false);
            panelGo.layer = LayerMask.NameToLayer("UI");

            Image panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.15f, 0.15f, 0.15f);

            RectTransform rt = panelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(750, 550);
            rt.anchoredPosition = Vector2.zero;

            return rt;
        }

        private static void CreateTitle(Transform parent)
        {
            GameObject titleGo = new GameObject("TitleText", typeof(RectTransform));
            titleGo.transform.SetParent(parent, false);
            titleGo.layer = LayerMask.NameToLayer("UI");

            Text title = titleGo.AddComponent<Text>();
            title.text = "背 包";
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 28;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleCenter;

            RectTransform rt = titleGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 40);
            rt.anchoredPosition = new Vector2(0, -10);
        }

        private static void CreateFilterButtons(Transform parent)
        {
            GameObject rowGo = new GameObject("FilterButtons", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            rowGo.layer = LayerMask.NameToLayer("UI");

            HorizontalLayoutGroup hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10;
            hlg.padding = new RectOffset(10, 10, 5, 5);
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            RectTransform rt = rowGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 45);
            rt.anchoredPosition = new Vector2(0, -55);

            string[] labels = { "全部", "装备", "消耗品", "材料" };
            string[] names = { "ButtonAll", "ButtonEquipment", "ButtonConsumable", "ButtonMaterial" };

            for (int i = 0; i < labels.Length; i++)
            {
                CreateFilterButton(rowGo.transform, names[i], labels[i]);
            }
        }

        private static Button CreateFilterButton(Transform parent, string goName, string label)
        {
            GameObject btnGo = new GameObject(goName, typeof(RectTransform));
            btnGo.transform.SetParent(parent, false);
            btnGo.layer = LayerMask.NameToLayer("UI");

            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.3f, 0.3f, 0.3f);

            Button button = btnGo.AddComponent<Button>();
            button.targetGraphic = btnImg;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f);
            colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f);
            button.colors = colors;

            LayoutElement layout = btnGo.AddComponent<LayoutElement>();
            layout.minWidth = 80;
            layout.preferredWidth = 100;

            // Label text
            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(btnGo.transform, false);
            labelGo.layer = LayerMask.NameToLayer("UI");

            Text labelText = labelGo.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.raycastTarget = false;

            RectTransform labelRT = labelGo.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            return button;
        }

        private static RectTransform CreateScrollView(Transform parent)
        {
            // ScrollView
            GameObject svGo = new GameObject("ScrollView", typeof(RectTransform));
            svGo.transform.SetParent(parent, false);
            svGo.layer = LayerMask.NameToLayer("UI");

            Image svImg = svGo.AddComponent<Image>();
            svImg.color = new Color(0.22f, 0.22f, 0.22f);

            ScrollRect scrollRect = svGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            RectTransform svRT = svGo.GetComponent<RectTransform>();
            svRT.anchorMin = new Vector2(0, 0);
            svRT.anchorMax = new Vector2(1, 1);
            svRT.offsetMin = new Vector2(10, 210);
            svRT.offsetMax = new Vector2(-10, -110);

            // Viewport
            GameObject vpGo = new GameObject("Viewport", typeof(RectTransform));
            vpGo.transform.SetParent(svGo.transform, false);
            vpGo.layer = LayerMask.NameToLayer("UI");

            Image vpImg = vpGo.AddComponent<Image>();
            vpImg.color = new Color(0.18f, 0.18f, 0.18f);

            Mask mask = vpGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform vpRT = vpGo.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            vpRT.anchoredPosition = Vector2.zero;

            // Content
            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(vpGo.transform, false);
            contentGo.layer = LayerMask.NameToLayer("UI");

            VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 4;
            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform contentRT = contentGo.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);
            contentRT.anchoredPosition = Vector2.zero;

            scrollRect.viewport = vpRT;
            scrollRect.content = contentRT;

            // Scrollbar
            GameObject sbGo = new GameObject("ScrollbarVertical", typeof(RectTransform));
            sbGo.transform.SetParent(svGo.transform, false);
            sbGo.layer = LayerMask.NameToLayer("UI");

            Image sbImg = sbGo.AddComponent<Image>();
            sbImg.color = new Color(0.4f, 0.4f, 0.4f);

            Scrollbar scrollbar = sbGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            RectTransform sbRT = sbGo.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(1, 0);
            sbRT.anchorMax = new Vector2(1, 1);
            sbRT.sizeDelta = new Vector2(15, 0);
            sbRT.anchoredPosition = Vector2.zero;
            sbRT.pivot = new Vector2(1, 0.5f);

            // SlidingArea
            GameObject saGo = new GameObject("SlidingArea", typeof(RectTransform));
            saGo.transform.SetParent(sbGo.transform, false);
            saGo.layer = LayerMask.NameToLayer("UI");
            RectTransform saRT = saGo.GetComponent<RectTransform>();
            saRT.anchorMin = Vector2.zero;
            saRT.anchorMax = Vector2.one;
            saRT.sizeDelta = new Vector2(-10, -10);
            saRT.anchoredPosition = Vector2.zero;

            // Handle
            GameObject handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(saGo.transform, false);
            handleGo.layer = LayerMask.NameToLayer("UI");
            Image handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(0.6f, 0.6f, 0.6f);
            RectTransform handleRT = handleGo.GetComponent<RectTransform>();
            handleRT.anchorMin = Vector2.zero;
            handleRT.anchorMax = Vector2.one;
            handleRT.sizeDelta = new Vector2(0, 0);
            handleRT.anchoredPosition = Vector2.zero;

            scrollbar.handleRect = handleRT;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            return contentRT;
        }

        private static RectTransform CreateDetailPanel(Transform parent)
        {
            GameObject dpGo = new GameObject("DetailPanel", typeof(RectTransform));
            dpGo.transform.SetParent(parent, false);
            dpGo.layer = LayerMask.NameToLayer("UI");

            Image dpImg = dpGo.AddComponent<Image>();
            dpImg.color = new Color(0.2f, 0.2f, 0.2f);

            RectTransform dpRT = dpGo.GetComponent<RectTransform>();
            dpRT.anchorMin = new Vector2(0, 0);
            dpRT.anchorMax = new Vector2(1, 0);
            dpRT.pivot = new Vector2(0.5f, 0);
            dpRT.sizeDelta = new Vector2(-20, 70);
            dpRT.anchoredPosition = new Vector2(0, 15);

            // Detail Text
            GameObject dtGo = new GameObject("DetailText", typeof(RectTransform));
            dtGo.transform.SetParent(dpGo.transform, false);
            dtGo.layer = LayerMask.NameToLayer("UI");

            Text detailText = dtGo.AddComponent<Text>();
            detailText.text = "未选择道具";
            detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailText.fontSize = 16;
            detailText.color = Color.white;
            detailText.alignment = TextAnchor.MiddleLeft;

            RectTransform dtRT = dtGo.GetComponent<RectTransform>();
            dtRT.anchorMin = Vector2.zero;
            dtRT.anchorMax = Vector2.one;
            dtRT.offsetMin = new Vector2(12, 5);
            dtRT.offsetMax = new Vector2(-12, -5);

            // Store DetailText reference on the panel for later wiring
            // We'll use Find() in CreateController
            dpGo.name = "DetailPanel";

            return dpRT;
        }

        private static InventoryPanel CreateController(GameObject slotPrefab, RectTransform contentParent, RectTransform detailPanel, Transform panelBg)
        {
            // Find existing or create new
            GameObject ctrlGo = GameObject.Find("InventoryPanelController");
            if (ctrlGo == null)
            {
                ctrlGo = new GameObject("InventoryPanelController");
            }

            InventoryPanel panel = ctrlGo.GetComponent<InventoryPanel>();
            if (panel == null)
                panel = ctrlGo.AddComponent<InventoryPanel>();

            // Wire serialized fields via reflection
            SetPrivateField(panel, "slotPrefab", slotPrefab);
            SetPrivateField(panel, "contentParent", contentParent);

            // Find DetailText in DetailPanel
            Transform detailTextTrans = detailPanel.Find("DetailText");
            if (detailTextTrans != null)
            {
                Text dt = detailTextTrans.GetComponent<Text>();
                SetPrivateField(panel, "detailText", dt);
            }

            Debug.Log("[BackpackSceneSetup] InventoryPanelController configured.");
            return panel;
        }

        private static void WireFilterButtons(Transform panelBg, InventoryPanel controller)
        {
            Transform filterRow = panelBg.Find("FilterButtons");
            if (filterRow == null)
            {
                Debug.LogWarning("[BackpackSceneSetup] FilterButtons row not found!");
                return;
            }

            Button buttonAll = filterRow.Find("ButtonAll")?.GetComponent<Button>();
            Button buttonEquipment = filterRow.Find("ButtonEquipment")?.GetComponent<Button>();
            Button buttonConsumable = filterRow.Find("ButtonConsumable")?.GetComponent<Button>();
            Button buttonMaterial = filterRow.Find("ButtonMaterial")?.GetComponent<Button>();

            SetPrivateField(controller, "buttonAll", buttonAll);
            SetPrivateField(controller, "buttonEquipment", buttonEquipment);
            SetPrivateField(controller, "buttonConsumable", buttonConsumable);
            SetPrivateField(controller, "buttonMaterial", buttonMaterial);

            Debug.Log("[BackpackSceneSetup] Filter buttons wired.");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Debug.Log("[BackpackSceneSetup] EventSystem created.");
            }
        }

        private static void SetPrivateField(Object obj, string fieldName, object value)
        {
            SerializedObject so = new SerializedObject(obj);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                if (value is GameObject go)
                    prop.objectReferenceValue = go;
                else if (value is Component comp)
                    prop.objectReferenceValue = comp;
                else if (value == null)
                    prop.objectReferenceValue = null;
                else
                    Debug.LogWarning($"[BackpackSceneSetup] Unsupported value type for field '{fieldName}': {value.GetType().Name}");

                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning($"[BackpackSceneSetup] Could not find serialized field '{fieldName}' on {obj.GetType().Name}");
            }
        }
    }
}
