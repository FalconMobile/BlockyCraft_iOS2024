using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using VoxelPlay;

namespace Sources.Scripts
{
    public static class SaveAndLoadingInventory
    {
        private const string SaveKeyInventory = "Inventory";

        private static readonly Dictionary<string, int> _saveItemDefinitionsList = new Dictionary<string, int>();

        public static void Save()
        {
            List<InventoryItem> items = VoxelPlayPlayer.instance.items;

            for (int k = 0; k < items.Count; k++)
            {
                ItemDefinition id = items[k].item;
                
                if (_saveItemDefinitionsList.ContainsKey(id.name))
                {
                    _saveItemDefinitionsList[id.name] += (int) items[k].quantity;
                }
                else
                {
                    _saveItemDefinitionsList.Add(id.name, (int) items[k].quantity);
                }
            }

            string inventoryData = JsonConvert.SerializeObject(_saveItemDefinitionsList);
            PlayerPrefs.SetString(SaveKeyInventory, inventoryData);

            _saveItemDefinitionsList.Clear();
        }

        public static void Loading(IVoxelPlayPlayer vpPlayer, List<InventoryItem> quickItems, List<InventoryItem> initialItems)
        {
            var inventoryData =
                JsonConvert.DeserializeObject<Dictionary<string, int>>(PlayerPrefs.GetString(SaveKeyInventory));

            if (inventoryData != null)
            {
                foreach (var item in inventoryData)
                {
                    vpPlayer.AddInventoryItem(VoxelPlayEnvironment.GetItemDefinition(item.Key), item.Value);
                }
            }
            else
            {
                LoadInitial(vpPlayer, quickItems, initialItems);
            }

            vpPlayer.MaxQuickItemsCount = quickItems.Count;
            for (int k = 0; k < quickItems.Count; k++)
            {
                vpPlayer.AddInventoryQuickItem(quickItems[k].item, k);
            }
        }

        public static void LoadInitial(IVoxelPlayPlayer vpPlayer, List<InventoryItem> quickItems, List<InventoryItem> initialItems)
        {
            if (initialItems != null)
            {
                for (int k = 0; k < initialItems.Count; k++)
                {
                    vpPlayer.AddInventoryItem(initialItems[k].item, initialItems[k].quantity);
                }
            }

            vpPlayer.MaxQuickItemsCount = quickItems.Count;
            for (int k = 0; k < quickItems.Count; k++)
            {
                vpPlayer.AddInventoryQuickItem(quickItems[k].item, k);
            }
        }
    }
}