using System;
using OctoGhast.Spatial;
using OctoGhast.SystemManager;

namespace OctoGhast.Framework.Mobile {
    public class Mobile<T> : RLObject<T>, IScheduleable where T : TemplateType
    {
        public Mobile(T data, int? serial = null) : base(data, serial) { }

        public virtual string Name() => "";

        /// <inheritdoc />
        public int Time { get; }

        public virtual void ProcessTurn() { }

        public virtual void Die(Mobile<T> killer) { }
    }

    public class PlayerData : TemplateType { }
    public class CreatureData : TemplateType { }

    /// <summary>
    /// A Player character instance.
    /// </summary>
    public class Player : BaseCreature
    {
        public PlayerData PlayerData { get; set; }

        /// <inheritdoc />
        public Player(PlayerData data, int? serial = null) : base(null, serial)
        {
            PlayerData = data;
        }

        public Player(int? serial = null) : base(null, serial)
        {
            PlayerData = new PlayerData();
        }
    }

    /// <summary>
    /// An instance of a creature, runs off TemplateData from <see cref="RLObject{T}"/>
    /// </summary>
    public class BaseCreature : Mobile<CreatureData>
    {
        /// <inheritdoc />
        public BaseCreature(CreatureData data, int? serial = null) : base(data, serial) { }

        public BaseCreature() : base(new CreatureData())
        {

        }

        public virtual void SendMessage(string msg) { }
    }

    /// <summary>
    /// An instance of an NPC, inherits from Creature for the TemplateData driven behaviour.
    /// </summary>
    public class BaseNpc : BaseCreature
    {
        /// <inheritdoc />
        public BaseNpc(CreatureData data, int? serial = null) : base(data, serial) { }
    }
}