using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OctoGhast.Entity;

namespace OctoGhast {
    public class DamageTypes : IEnumerable<DamageType> {
        public static readonly IEnumerable<DamageType> Types =
            new[]
            {
                DamageType.Debug, DamageType.Biological, DamageType.Bash, DamageType.Cut, DamageType.Acid,
                DamageType.Stab, DamageType.Heat, DamageType.Cold, DamageType.Electric
            };

        public IEnumerator<DamageType> GetEnumerator() {
            return Types.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Types of damage internally.
    /// </summary>
    public enum DamageType {
        /// <summary>
        /// Typeless damage, ignores all resists
        /// </summary>
        // TODO: Better name for this?
        Debug,

        /// <summary>
        /// Internal, damage from smoke or poison
        /// </summary>
        Biological,

        /// <summary>
        /// Bashing damage
        /// </summary>
        Bash,

        /// <summary>
        /// Cutting damage, includes ranged attacks (bullets, arrows)
        /// </summary>
        Cut,

        /// <summary>
        /// Corrosive damage
        /// </summary>
        Acid,

        /// <summary>
        /// Stabbing/piercing damage
        /// </summary>
        Stab,

        /// <summary>
        /// Thermal damage
        /// </summary>
        Heat,

        /// <summary>
        /// Heatdrain, cyrogrenades
        /// </summary>
        Cold,

        /// <summary>
        /// Electrical discharge
        /// </summary>
        Electric,
    }

    public class DamageUnit : IEquatable<DamageUnit> {
        public DamageType Type { get; }
        public float Amount { get; internal set; }
        public float ResistancePenetration { get; internal set; }
        public float ResistanceMultiplier { get; internal set; }
        public float DamageMultiplier { get; internal set; }

        public DamageUnit(DamageType type, float amount,
            float resistancePenetration = 0.0f, float resistanceMultiplier = 0.0f,
            float damageMultiplier = 1.0f) {
            Type = type;
            Amount = amount;
            ResistancePenetration = resistancePenetration;
            ResistanceMultiplier = resistanceMultiplier;
            DamageMultiplier = damageMultiplier;
        }

        public override bool Equals(object obj) {
            return Equals(obj as DamageUnit);
        }

        public bool Equals(DamageUnit other) {
            return other != null &&
                   Type == other.Type &&
                   Amount == other.Amount &&
                   ResistancePenetration == other.ResistancePenetration &&
                   ResistanceMultiplier == other.ResistanceMultiplier &&
                   DamageMultiplier == other.DamageMultiplier;
        }

        private static Dictionary<string, DamageType> _damageMap;
        public static DamageType FromString(string value) {
            _damageMap = _damageMap ?? RetrieveDamageMap();

            if (_damageMap.TryGetValue(value.ToLowerInvariant(), out var enumMember))
                return enumMember;
            throw new Exception($"Unknown damage type {value}");
        }

        private static Dictionary<string, DamageType> RetrieveDamageMap() {
            var map = new Dictionary<string, DamageType>();
            foreach (var entry in Enum.GetNames(typeof(DamageType))) {
                if (Enum.TryParse(entry,out DamageType enumValue)) {
                    map.Add(entry.ToLowerInvariant(), enumValue);
                }
            }

            return map;
        }

        public static bool operator ==(DamageUnit unit1, DamageUnit unit2) {
            return EqualityComparer<DamageUnit>.Default.Equals(unit1, unit2);
        }

        public static bool operator !=(DamageUnit unit1, DamageUnit unit2) {
            return !(unit1 == unit2);
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            unchecked {
                var hashCode = (int) Type;
                hashCode = (hashCode * 397) ^ Amount.GetHashCode();
                hashCode = (hashCode * 397) ^ ResistancePenetration.GetHashCode();
                hashCode = (hashCode * 397) ^ ResistanceMultiplier.GetHashCode();
                hashCode = (hashCode * 397) ^ DamageMultiplier.GetHashCode();
                return hashCode;
            }
        }
    }

    public class DamageInfo : IEnumerable<DamageUnit> {
        public List<DamageUnit> DamageUnits { get; set; }

        public void AddDamage(DamageType type, float amount, float resistPenetration = 0.0f, float resistMultiplier = 1.0f, float multiplier = 1.0f) {
            DamageUnits.Add(new DamageUnit(type, amount, resistPenetration, resistMultiplier, multiplier));
        }

        public void Add(DamageInfo info) {
            DamageUnits.AddRange(info.DamageUnits);
        }

        public void Add(DamageUnit src) {
            float Lerp(float min, float max, float t) {
                return (1.0f - t) * min + t * max;
            }

            var matchingDamage = DamageUnits.Where(s => s.Type == src.Type);
            foreach (var damage in matchingDamage) {
                float mult = src.DamageMultiplier / damage.DamageMultiplier;
                damage.Amount += src.Amount * mult;
                damage.ResistancePenetration += src.ResistancePenetration * mult;
                // Linearly interpole armor multplier based on damage proportion contributed
                float t = src.DamageMultiplier / (src.DamageMultiplier + damage.DamageMultiplier);
                damage.ResistanceMultiplier = Lerp(damage.ResistanceMultiplier, src.DamageMultiplier, t);
            }
        }

        public static DamageInfo Physical(float bash, float cut, float stab, float armorPenetration)
        {
            var damage = new DamageInfo();
            damage.AddDamage(DamageType.Bash, bash, armorPenetration);
            damage.AddDamage(DamageType.Cut, cut, armorPenetration);
            damage.AddDamage(DamageType.Stab, stab, armorPenetration);
            return damage;
        }

        public void Multiply(float multiplier, bool preArmor) {
            if (multiplier < 0.0)
                DamageUnits.Clear();

            if (preArmor) {
                foreach (var i in DamageUnits) {
                    i.Amount *= multiplier;
                }
            }
            else {
                foreach (var i in DamageUnits) {
                    i.DamageMultiplier *= multiplier;
                }
            }
        }

        public float DamageForType(DamageType type) {
            return DamageUnits.Where(s => s.Type == type).Sum(s => s.Amount * s.DamageMultiplier);
        }

        public float TotalDamage() {
            return DamageUnits.Sum(s => s.Amount * s.DamageMultiplier);
        }

        /// <inheritdoc />
        public IEnumerator<DamageUnit> GetEnumerator() {
            return DamageUnits.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable) DamageUnits).GetEnumerator();
        }
    }

    public class DealtDamage {
        public Dictionary<DamageType,int> DealtDamages { get; }
        public BodyPart BodyPartHit { get; set; }

        public DealtDamage() {
            DealtDamages = new Dictionary<DamageType, int>();
        }

        public void SetDamage(DamageType type, int amount) {
            DealtDamages[type] = amount;
        }

        public int DamageForType(DamageType type) {
            return DealtDamages[type];
        }

        public int TotalDamage() {
            return DealtDamages.Sum(s => s.Value);
        }
    }

    public class Resistances : IEquatable<Resistances>
    {
        public Dictionary<DamageType, int> ResistValues { get; }

        public Resistances() {
            ResistValues = new Dictionary<DamageType, int>();
        }

        /*
        public Resistances(Item item, bool toSelf) : this() {
            // Armors protect, but all items can resist
            if (toSelf || item.IsArmor) {
                foreach (var type in DamageTypes.Types) {
                    var armor = item.AsArmor();
                    SetResist(type, armor.DamageResist(type, toSelf));
                }
            }
        }
        */

        public Resistances(IMobile mob) : this() {
            
        }

        public void SetResist(DamageType type, float amount) {
            
        }

        public float ResistForType(DamageType type) {
            throw new NotImplementedException();
        }

        public float GetEffectiveResist(DamageUnit unit) {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Resistances);
        }

        public bool Equals(Resistances other)
        {
            return other != null &&
                   EqualityComparer<Dictionary<DamageType, int>>.Default.Equals(ResistValues, other.ResistValues);
        }

        public static bool operator ==(Resistances resistances1, Resistances resistances2)
        {
            return EqualityComparer<Resistances>.Default.Equals(resistances1, resistances2);
        }

        public static bool operator !=(Resistances resistances1, Resistances resistances2)
        {
            return !(resistances1 == resistances2);
        }

        public static Resistances operator +(Resistances self, Resistances other) {
            throw new NotImplementedException(); 
        }
    }
}