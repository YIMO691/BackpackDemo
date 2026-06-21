using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory
{
    /// <summary>
    /// Main inventory panel controller. Manages filtering, selection, and list refresh.
    /// Uses object-pool-style slot reuse (enable/disable) — no Destroy/Instantiate on filter.
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        [Header("Slot Setup")]
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Transform contentParent;

        [Header("Detail Display")]
        [SerializeField] private Text detailText;

        [Header("Filter Buttons")]
        [SerializeField] private Button buttonAll;
        [SerializeField] private Button buttonEquipment;
        [SerializeField] private Button buttonConsumable;
        [SerializeField] private Button buttonMaterial;

        private List<ItemData> allItems;
        private ItemData selectedItem;
        private ItemType? currentFilter;
        private List<InventorySlotView> slotPool = new List<InventorySlotView>();

        private void Start()
        {
            allItems = InventoryMockData.GetItems();

            // Wire up filter buttons
            buttonAll.onClick.AddListener(() => OnFilterClicked(null));
            buttonEquipment.onClick.AddListener(() => OnFilterClicked(ItemType.Equipment));
            buttonConsumable.onClick.AddListener(() => OnFilterClicked(ItemType.Consumable));
            buttonMaterial.onClick.AddListener(() => OnFilterClicked(ItemType.Material));

            RefreshList();
            ClearSelection();
        }

        /// <summary>
        /// Filter button handler. Pass null for "show all".
        /// </summary>
        public void OnFilterClicked(ItemType? filter)
        {
            currentFilter = filter;

            // Clear selection if the currently selected item would be filtered out
            if (selectedItem != null)
            {
                if (filter.HasValue && selectedItem.Type != filter.Value)
                {
                    ClearSelection();
                }
            }

            RefreshList();
        }

        /// <summary>
        /// Refresh the visible slot list based on current filter.
        /// Reuses pooled slots — enables needed ones, disables excess ones.
        /// </summary>
        public void RefreshList()
        {
            List<ItemData> filtered;
            if (currentFilter.HasValue)
            {
                filtered = allItems.FindAll(item => item.Type == currentFilter.Value);
            }
            else
            {
                filtered = new List<ItemData>(allItems);
            }

            int displayCount = filtered.Count;
            int poolCount = slotPool.Count;

            // Ensure we have enough pooled slots
            while (slotPool.Count < displayCount)
            {
                GameObject go = Instantiate(slotPrefab, contentParent);
                InventorySlotView slotView = go.GetComponent<InventorySlotView>();
                if (slotView == null)
                {
                    slotView = go.AddComponent<InventorySlotView>();
                }
                slotPool.Add(slotView);
            }

            // Setup visible slots
            for (int i = 0; i < displayCount; i++)
            {
                ItemData item = filtered[i];
                InventorySlotView slot = slotPool[i];
                slot.gameObject.SetActive(true);

                bool isSelected = (selectedItem != null && selectedItem == item);
                slot.Setup(item, isSelected, OnSlotClicked);
            }

            // Hide excess pooled slots
            for (int i = displayCount; i < slotPool.Count; i++)
            {
                slotPool[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Select a specific item and update detail display.
        /// </summary>
        public void SelectItem(ItemData item)
        {
            // If already selected, do nothing (prevents redundant refresh)
            if (selectedItem == item) return;

            selectedItem = item;
            RefreshList();
            UpdateDetailDisplay();
        }

        /// <summary>
        /// Clear current selection and detail display.
        /// </summary>
        public void ClearSelection()
        {
            selectedItem = null;
            RefreshList();
            UpdateDetailDisplay();
        }

        private void OnSlotClicked(ItemData item)
        {
            if (selectedItem == item)
            {
                // Clicking the same item — no change, no-op
                return;
            }
            SelectItem(item);
        }

        private void UpdateDetailDisplay()
        {
            if (detailText == null) return;

            if (selectedItem != null)
            {
                string typeName = selectedItem.Type switch
                {
                    ItemType.Equipment => "装备",
                    ItemType.Consumable => "消耗品",
                    ItemType.Material => "材料",
                    _ => selectedItem.Type.ToString()
                };
                detailText.text = $"名称: {selectedItem.Name}\n类型: {typeName}\n数量: {selectedItem.Count}";
            }
            else
            {
                detailText.text = "未选择道具";
            }
        }
    }
}
