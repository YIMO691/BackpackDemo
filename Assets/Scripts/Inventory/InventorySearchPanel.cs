using System;
using System.Collections.Generic;
using System.Linq;
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
    private float _refreshTimer;

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

    private void Update()
    {
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer > 0.2f)
        {
            _refreshTimer = 0f;
            RefreshList();
        }
    }

    private void RefreshList()
    {
        string keyword = SearchInput.text;
        InventoryItemType selectedType = (InventoryItemType)TypeDropdown.value;

        for (int i = ContentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(ContentRoot.GetChild(i).gameObject);
        }

        List<InventoryItemData> result = Items
            .Where(item =>
                (selectedType == InventoryItemType.All || item.Type == selectedType) &&
                (string.IsNullOrEmpty(keyword) || item.Name.ToLower().Contains(keyword.ToLower())))
            .OrderBy(item => item.Name)
            .ToList();

        CountText.text = "Count: " + result.Count + " / " + Items.Count;

        foreach (InventoryItemData item in result)
        {
            GameObject slotObj = Instantiate(SlotPrefab, ContentRoot);
            InventoryItemSlot slot = slotObj.GetComponent<InventoryItemSlot>();

            bool selected = _selectedItem != null && _selectedItem.Id == item.Id;

            slot.SetData(item, $"{item.Name} x{item.Count} [{item.Type}]", selected, OnClickItem);
        }

        if (_selectedItem != null && !result.Any(item => item.Id == _selectedItem.Id))
        {
            _selectedItem = null;
            DetailText.text = "No item selected";
        }

        Debug.Log($"Inventory refreshed at {DateTime.Now}, result count = {result.Count}");
    }

    private void OnClickItem(InventoryItemData item)
    {
        _selectedItem = item;
        DetailText.text = $"Name: {item.Name}\nType: {item.Type}\nCount: {item.Count}";
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

    public void SetData(InventoryItemData item, string displayName, bool selected, Action<InventoryItemData> onClick)
    {
        _item = item;
        _onClick = onClick;

        NameText.text = displayName;
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
