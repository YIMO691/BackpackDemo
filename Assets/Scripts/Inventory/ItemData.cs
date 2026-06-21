using System;

namespace Inventory
{
    [Serializable]
    public class ItemData
    {
        public string Name;
        public ItemType Type;
        public int Count;

        public override string ToString()
        {
            return $"{Name} ({Type}) x{Count}";
        }
    }
}
