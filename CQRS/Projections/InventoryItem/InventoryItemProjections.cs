using CQRS.Events.InventoryItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQRS.Projections.InventoryItem
{
    public class InventoryItemProjection : Projection, ProjectionHandles<InventoryItemCreated>
    {
        public string Name { get; set; }

        public void Handle(InventoryItemCreated message)
        {
            if (GetKeyFromMessage(message) == Key)
            {
                Name = message.Name;
            }
        }

        public string GetKeyFromMessage(InventoryItemCreated message)
        {
            return message.InventoryId.ToString();
        }

        public InventoryItemProjection()
        {

        }
    }

    public class InventoryItemListProjection : Projection, ProjectionHandles<InventoryItemCreated>
    {
        public List<InventoryItemProjection> Items = new List<InventoryItemProjection>();
        public string GetKeyFromMessage(InventoryItemCreated message)
        {
            return "";
        }

        public void Handle(InventoryItemCreated message)
        {
            var proj = new InventoryItemProjection();
            proj.Key = proj.GetKeyFromMessage(message);
            proj.Handle(message);
            Items.Add(proj);
        }
    }

    public class InventoryItemDetailsProjection : Projection, ProjectionHandles<ItemsCheckedInToInventory>, ProjectionHandles<InventoryItemCreated>
    {
        public string Name { get; set; }
        public int CurrentCount { get; set; }
        public int Version { get; set; }
        public InventoryItemDetailsProjection()
        {

        }
        public void Handle(InventoryItemCreated message)
        {
            if (message.InventoryId.ToString() == Key)
            {
                Name = message.Name;
            }
        }
        public void Handle(ItemsCheckedInToInventory message)
        {
            if (message.InventoryId.ToString() == Key)
            {
                CurrentCount = message.Count;
            }
        }

        public string GetKeyFromMessage(ItemsCheckedInToInventory message)
        {
            return message.InventoryId.ToString();
        }

        public string GetKeyFromMessage(InventoryItemCreated message)
        {
            return message.InventoryId.ToString();
        }
    }
}
