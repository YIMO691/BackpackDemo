using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryItemType
{
    All,
    Equipment,
    Consumable,
    Material
}

[Serializable]
public class InventoryItemData
{
    public int Id;
    public string Name;
    public InventoryItemType Type;
    public int Count;
}

public class InventorySearchPanel : MonoBehaviour
{
    [Header("UI")]
    public InputField SearchInput;
    public Dropdown TypeDropdown;
    public Transform ContentRoot;
    public GameObject SlotPrefab;
    public Text DetailText;
    public Text CountText;

    [Header("Mock Data")]
    public List<InventoryItemData> Items = new List<InventoryItemData>();

    private InventoryItemData _selectedItem;

    // Slot reuse pool (GC Fix #4, #5)
    private List<InventoryItemSlot> _activeSlots = new List<InventoryItemSlot>();

    // Cached result list (GC Fix #2)
    private List<InventoryItemData> _filteredResults = new List<InventoryItemData>();

    // Cached comparison delegate to avoid lambda allocation in Sort (GC Fix #2)
    private static readonly Comparison<InventoryItemData> NameComparison =
        (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

    private void Start()
    {
        if (Items.Count == 0)
        {
            Items.Add(new InventoryItemData { Id = 1, Name = "Iron Sword", Type = InventoryItemType.Equipment, Count = 1 });
            Items.Add(new InventoryItemData { Id = 2, Name = "Wood Shield", Type = InventoryItemType.Equipment, Count = 1 });
            Items.Add(new InventoryItemData { Id = 3, Name = "Small HP Potion", Type = InventoryItemType.Consumable, Count = 5 });
            Items.Add(new InventoryItemData { Id = 4, Name = "Small MP Potion", Type = InventoryItemType.Consumable, Count = 3 });
            Items.Add(new InventoryItemData { Id = 5, Name = "Iron Ore", Type = InventoryItemType.Material, Count = 12 });
            Items.Add(new InventoryItemData { Id = 6, Name = "Wood", Type = InventoryItemType.Material, Count = 20 });
        }

        SearchInput.onValueChanged.AddListener(_ => RefreshList());
        TypeDropdown.onValueChanged.AddListener(_ => RefreshList());

        RefreshList();
    }

    // GC Fix #1: Removed Update() timer-based refresh. RefreshList is now event-driven
    // via SearchInput.onValueChanged, TypeDropdown.onValueChanged, and OnClickItem.

    private void RefreshList()
    {
        string keyword = SearchInput.text;
        InventoryItemType selectedType = (InventoryItemType)TypeDropdown.value;

        // GC Fix #2: Replace LINQ Where/OrderBy/ToList with for-loop + cached list
        _filteredResults.Clear();

        bool filterByType = selectedType != InventoryItemType.All;
        bool filterByKeyword = !string.IsNullOrEmpty(keyword);

        for (int i = 0; i < Items.Count; i++)
        {
            InventoryItemData item = Items[i];

            if (filterByType && item.Type != selectedType)
                continue;

            // GC Fix #3: IndexOf with OrdinalIgnoreCase instead of ToLower().Contains()
            if (filterByKeyword && item.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            _filteredResults.Add(item);
        }

        _filteredResults.Sort(NameComparison);

        CountText.text = "Count: " + _filteredResults.Count + " / " + Items.Count;

        // GC Fix #4 & #5: Reuse slots instead of Destroy/Instantiate; cache component refs
        int resultCount = _filteredResults.Count;

        // Grow pool if needed
        while (_activeSlots.Count < resultCount)
        {
            GameObject slotObj = Instantiate(SlotPrefab, ContentRoot);
            InventoryItemSlot slot = slotObj.GetComponent<InventoryItemSlot>();
            _activeSlots.Add(slot);
        }

        for (int i = 0; i < _activeSlots.Count; i++)
        {
            InventoryItemSlot slot = _activeSlots[i];

            if (i < resultCount)
            {
                InventoryItemData item = _filteredResults[i];
                bool selected = _selectedItem != null && _selectedItem.Id == item.Id;

                slot.gameObject.SetActive(true);
                // GC Fix #7: display string computed inside SetData, not pre-interpolated here
                slot.SetData(item, selected, OnClickItem);
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }

        // GC Fix #2 continued: Replace LINQ Any with manual loop
        if (_selectedItem != null)
        {
            bool found = false;
            for (int i = 0; i < _filteredResults.Count; i++)
            {
                if (_filteredResults[i].Id == _selectedItem.Id)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                _selectedItem = null;
                DetailText.text = "No item selected";
            }
        }

        // GC Fix #6: Removed high-frequency Debug.Log
    }

    private void OnClickItem(InventoryItemData item)
    {
        _selectedItem = item;
        DetailText.text = "Name: " + item.Name + "\nType: " + item.Type + "\nCount: " + item.Count;
        RefreshList();
    }
}

public class InventoryItemSlot : MonoBehaviour
{
    public Text NameText;
    public Text CountText;
    public Text TypeText;
    public Image SelectedFrame;
    public Button Button;

    private InventoryItemData _item;
    private Action<InventoryItemData> _onClick;

    // GC Fix #7: Removed displayName parameter. Display text is now built inside SetData
    // to avoid intermediate string allocation in RefreshList.
    public void SetData(InventoryItemData item, bool selected, Action<InventoryItemData> onClick)
    {
        _item = item;
        _onClick = onClick;

        NameText.text = item.Name + " x" + item.Count + " [" + item.Type + "]";
        CountText.text = "x" + item.Count;
        TypeText.text = item.Type.ToString();
        SelectedFrame.gameObject.SetActive(selected);

        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() =>
        {
            _onClick?.Invoke(_item);
        });
    }
}
