using System;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory
{
    /// <summary>
    /// Individual item slot view. Displays item info and selected state.
    /// Setup() is called by InventoryPanel when refreshing the list.
    /// </summary>
    public class InventorySlotView : MonoBehaviour
    {
        [Header("Text Fields")]
        [SerializeField] private Text nameText;
        [SerializeField] private Text typeText;
        [SerializeField] private Text countText;

        [Header("Selected Indicator")]
        [SerializeField] private Image selectedBackground;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(1f, 1f, 0.6f); // pale yellow highlight

        private ItemData currentItem;
        private Action<ItemData> onClickCallback;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }
            button.onClick.AddListener(HandleClick);
        }

        /// <summary>
        /// Populate the slot with item data and selection state.
        /// </summary>
        public void Setup(ItemData item, bool isSelected, Action<ItemData> onClick)
        {
            currentItem = item;
            onClickCallback = onClick;

            if (nameText != null)
                nameText.text = item.Name;

            if (typeText != null)
            {
                typeText.text = item.Type switch
                {
                    ItemType.Equipment => "装备",
                    ItemType.Consumable => "消耗品",
                    ItemType.Material => "材料",
                    _ => item.Type.ToString()
                };
            }

            if (countText != null)
                countText.text = item.Count.ToString();

            UpdateSelectedVisual(isSelected);
        }

        private void HandleClick()
        {
            onClickCallback?.Invoke(currentItem);
        }

        private void UpdateSelectedVisual(bool isSelected)
        {
            if (selectedBackground != null)
            {
                selectedBackground.color = isSelected ? selectedColor : normalColor;
            }
        }
    }
}
