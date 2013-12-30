namespace OctoGhast.Entity.Behaviours
{
    public interface IEntityBehaviour
    {
        
    }

    public interface IUpdatingEntityBehaviour : IEntityBehaviour
    {
        void Update();
    }

    public interface IDamagableEntityBehaviour : IEntityBehaviour
    {
        void Damage(int damage);
    }

    public interface IAttackingEntityBehaviour : IEntityBehaviour
    {
        void Attack(IMobile target);
    }

    public class UpdatingEntityBehaviour : IUpdatingEntityBehaviour
    {
        private IMobile Owner { get; set; }

        public UpdatingEntityBehaviour(IMobile owner) {
            Owner = owner;
        }

        public void Update() {
            return;
        }
    }

    public class DamagableEntityBehaviour : IDamagableEntityBehaviour
    {
        private IMobile Owner { get; set; }

        public DamagableEntityBehaviour(IMobile owner) {
            Owner = owner;
        }

        public void Damage(int damage) {
            return;
        }
    }

    public class AttackingEntityBehaviour : IAttackingEntityBehaviour
    {
        private IMobile Owner { get; set; }

        public AttackingEntityBehaviour(IMobile owner) {
            Owner = owner;
        }

        public void Attack(IMobile target) {
            return;
        }
    }
}