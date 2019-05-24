using System;
using InfiniMap;
using Newtonsoft.Json.Linq;
using OctoGhast.Cataclysm.Items;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Cataclysm.Loaders.Item;
using OctoGhast.Cataclysm.Loaders.Item.Types;
using OctoGhast.Entity;
using OctoGhast.Framework;
using OctoGhast.Framework.Items.Actions;
using OctoGhast.Framework.Mobile;
using static OctoGhast.Translation.Translation;

namespace OctoGhast.Cataclysm.UseActions {
    public class TransformUseActionData {

        /// <summary>
        /// Displayed if the player sees the transformation. {0} replaced with item name.
        /// </summary>
        [LoaderInfo("msg")]
        public string TransformMessage { get; set; }

        /// <summary>
        /// Type of the resulting item
        /// </summary>
        [LoaderInfo("target")]
        public StringID<ItemType> Target { get; set; }

        /// <summary>
        /// If set, transform item to container and place new item inside.
        /// </summary>
        [LoaderInfo("container")]
        public StringID<ItemType> Container { get; set; }

        /// <summary>
        /// If non-negative, set remaining ammo of Target to this, after transforming.
        /// </summary>
        [LoaderInfo("target_charges")]
        public int AmmoQuantity { get; set; }

        /// <summary>
        /// If positive, set transformed item active and start countdown.
        /// </summary>
        [LoaderInfo("countdown")]
        public int Countdown { get; set; }

        /// <summary>
        /// If both this and AmmoQuantity are specified, set Target to this specific ammo.
        /// </summary>
        [LoaderInfo("target_ammo")]
        public StringID<AmmoType> AmmoType { get; set; }

        /// <summary>
        /// Use to set the active property of the transformed Target
        /// </summary>
        [LoaderInfo("active")]
        public bool Active { get; set; }

        /// <summary>
        /// Subtract this from Creature.Moves when transformation is succesful
        /// </summary>
        [LoaderInfo("moves")]
        public int MoveCost { get; set; }

        /// <summary>
        /// Minimum number of fire charges required (if any) for transformation.
        /// </summary>
        [LoaderInfo("need_fire")]
        public int FireRequirement { get; set; }

        /// <summary>
        /// Displayed if item is in player possession with {0} replaced by item name.
        /// </summary>
        [LoaderInfo("need_fire_msg")]
        public string NeedFireMessage { get; set; }

        /// <summary>
        /// Minimum charges, if any, required for transformation
        /// </summary>
        [LoaderInfo("need_charges")]
        public string NeedCharges { get; set; }

        [LoaderInfo("need_charges_msg")]
        public string NeedChargesMessage { get; set; }

        [LoaderInfo("menu_text")]
        public string MenuText { get; set; }
    }

    public class TransformUseAction : ItemUseAction {
        public TransformUseActionData Data { get; set; }

        /// <inheritdoc />
        public TransformUseAction(string type) : base("transform") { }

        
        /// <inheritdoc />
        public override int Invoke(UseActionData actionData, BaseCreature player, BaseItem item, bool turnTick, WorldSpace position) {

            throw new NotImplementedException();
            /*
            if (turnTick)
                return 0;

            bool hasItem = player.HasItem(item);

            if (Data.NeedCharges && item.AmmoRemaining < Data.NeedCharges) {
                if (hasItem) {
                    player.SendMessage(Data.NeedChargesMessage, item.GetName());
                }

                return 0;
            }

            if (Data.FireRequirement > 0 && hasItem) {
                if (player.IsSubmerged()) {
                    player.SendMessage(_($"You can't do that while submerged"));
                    return 0;
                }

                if (!player.AttemptUseCharges(ChargeType.Fire, Data.FireRequirement)) {
                    player.SendMessage(Data.NeedFireMessage, item.GetName());
                    return 0;
                }
            }

            if (player.CanSee(position) && !String.IsNullOrWhiteSpace(Data.TransformMessage)) {
                player.SendMessage(Data.TransformMessage, item.GetName());
            }

            if (hasItem)
                player.Moves = Data.MoveCost;

            BaseItem newItem;
            if (Data.Container == null) {

            }
            else {
                throw new NotImplementedException("Implement Containers properly");
            }
            */
        }
        

        /// <inheritdoc />
        public override void Load(JObject jObj) {
            Data = new TransformUseActionData()
            {
                AmmoType = jObj.ReadProperty(() => Data.AmmoType),
                Target = jObj.ReadProperty(() => Data.Target),
                Container = jObj.ReadProperty(() => Data.Container),
                Active = jObj.ReadProperty(() => Data.Active),
                AmmoQuantity = jObj.ReadProperty(() => Data.AmmoQuantity),
                Countdown = jObj.ReadProperty(() => Data.Countdown),
                MenuText = jObj.ReadProperty(() => Data.MenuText),
                MoveCost = jObj.ReadProperty(() => Data.MoveCost),
                NeedCharges = jObj.ReadProperty(() => Data.NeedCharges),
                NeedChargesMessage = jObj.ReadProperty(() => Data.NeedChargesMessage),
                FireRequirement = jObj.ReadProperty(() => Data.FireRequirement),
                NeedFireMessage = jObj.ReadProperty(() => Data.NeedFireMessage),
                TransformMessage = jObj.ReadProperty(() => Data.TransformMessage),
            };

            if (Data.AmmoType != null && Data.Container != null) {
                throw new Exception("Transform actor specified both ammo type and container type");
            }

            if (Data.MoveCost < 0) {
                throw new Exception("Transform actor specified negative moves");
            }
        }
    }
}