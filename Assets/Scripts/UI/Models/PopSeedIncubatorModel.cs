// using System;
// using GameItem;
// using UI.Popups;

// namespace UI.Models
// {
//     public class PopSeedIncubatorModel : ModelBase<PopSeedIncubator>
//     {
//         public override string Path => "PopSeedIncubator";
//         public override ViewType ViewType => ViewType.Popup;
        
//         public SeedIncubatorItem SeedIncubatorItem => Data[0] as SeedIncubatorItem;

//         internal void SetNewItem(PropItem propItem, Inventory inventory)
//         {
//             PropItem temp = null;
//             if (SeedIncubatorItem != null)
//             {
//                 temp = SeedIncubatorItem.CultivatingItem;
//             }
//             inventory.RemoveItem(propItem, propItem.Quantity);
//             SeedIncubatorItem.ChangeItem(propItem);

//             if (temp != null)
//             {
//                 inventory.AddItem(temp);
//             }
//         }

//         public void TakeSeed(Inventory inventory)
//         {
//             SeedIncubatorItem.BeTake(inventory);
//         }
//     }
// }