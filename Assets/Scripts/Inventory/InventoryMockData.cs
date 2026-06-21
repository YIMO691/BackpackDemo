using System.Collections.Generic;

namespace Inventory
{
    public static class InventoryMockData
    {
        public static List<ItemData> GetItems() => new List<ItemData>
        {
            new ItemData { Name = "铁剑",   Type = ItemType.Equipment,   Count = 1 },
            new ItemData { Name = "木盾",   Type = ItemType.Equipment,   Count = 1 },
            new ItemData { Name = "小血瓶", Type = ItemType.Consumable,  Count = 5 },
            new ItemData { Name = "小蓝瓶", Type = ItemType.Consumable,  Count = 3 },
            new ItemData { Name = "铁矿石", Type = ItemType.Material,    Count = 12 },
            new ItemData { Name = "木材",   Type = ItemType.Material,    Count = 20 },
        };
    }
}
