using System.Collections.Generic;
using Capsicum;

namespace OctoGhast.Activities {
    public class ButcherActivity : Activity {
        private int _sourceButcherySkill;
        private ButcherableComponent _targetButcherable;
        private CreatureTaxonomyComponent _creatureTaxonomy;
        private ButcherStage _currentButcherStage;
        private Entity _requiredItem;

        public ButcherActivity(int totalTime, Entity source, Entity target) : base(totalTime, source, target) { }

        /// <inheritdoc />
        public override bool Start() {
            if (Target.TryGetComponent<ButcherableComponent>(out var butcherable))
                _targetButcherable = butcherable;
            else // That is not butcherable!
                return false;

            if (Target.TryGetComponent<CreatureTaxonomyComponent>(out var taxonomy))
                _creatureTaxonomy = taxonomy;
            else // You don't know how to dress this creature.
                return false;

            if (Source.TryGetComponent<SkillsComponent>(out var skills))
                _sourceButcherySkill = skills.Skills["BUTCHERY"];
            else // You haven't learned how to butcher animals!
                return false;

            // Base time required is 30 minutes per size class. Every 5 levels grants a 3 minute reduction.
            var timeRequired = ((int) _creatureTaxonomy.Size * (30 * 60)) - ((_sourceButcherySkill / 5)*3);
            AddRequiredTime(timeRequired);

            // Ask the Actor to pick which stage they want to perform
            // var queryResult = base.QueryPlayerActor("What would you do with the creatures carcass?", availableStages);
            // currentStage = queryResult;

            //  - Field dress : Removes any projectiles in the carcass, removes unwanted internal organs. (Generates 'junk' items)
            //  - Skin Pelt : Skin any usable pelt from the carcass
            //  - Harvest : Remove any interesting parts from the carcass (spider venom, deer antlers, etc)
            //  - Butcher Meat : Gather any meat and field clean any usable bones

            // Initially the only available option is to field dress.
            List<ButcherStage> availableStages = new List<ButcherStage> {ButcherStage.FieldDress};

            // Once the creature has been field dressed, the other options are available.
            if (_targetButcherable.Stage.HasFlag(ButcherStage.FieldDress)) {
                availableStages.Remove(ButcherStage.FieldDress);

                if (!_targetButcherable.Stage.HasFlag(ButcherStage.SkinPelt))
                    availableStages.Add(ButcherStage.SkinPelt);

                if (!_targetButcherable.Stage.HasFlag(ButcherStage.HarvestIngredients))
                    availableStages.Add(ButcherStage.HarvestIngredients);

                if (!_targetButcherable.Stage.HasFlag(ButcherStage.ButcherMeat))
                    availableStages.Add(ButcherStage.ButcherMeat);

                availableStages.Add(ButcherStage.Finished);
            }

            ButcherStage currentStage = ButcherStage.SkinPelt;

            // Any butchery requires at least a bladed/sharp item, the ButcherQuality of the item affects the output, not the time.
            // Any improvement in output is reliant on butchery skill, a new hunter using a fancy knife doesn't make much of a difference.
            // Rough progression:
            //  - Sharp rock : Rock with a vaguely sharp edge
            //  - Flint hand axe : Primitive flint tool, fairly sharp edge
            //  - Copper/Bronze knife : Sharpend metal edge
            //  - Iron hunting knife : Designed for hunting
            //  - Steel hunting knife : Sharper, still designed for hunting

            _requiredItem = default(Entity); // TODO: SourceInventory.FindItem(ItemQuality.Bladed)
            _currentButcherStage = currentStage;

            return true;
        }

        /// <inheritdoc />
        public override bool Update(ulong time) {
            // Check that Source (player) can still reach Target (carcass)
            // - Is Target still something that can be butchered?
            // - Does Source still satisfy conditions?

            if (InterruptFlag) {
                // Something interrupted us!
                return false;
            }

            // Are we due to apply any buffs/debuffs to Source for doing this?
            // - This is hard work, so apply some fatigue
            // - Field dressing can be messy, roll for chance of splattering some blood around (maybe on Source?)
            // - Some traits can make actors squeamish around blood or gore
            
            // Every few turns (as a percentage of total time for stage) generate some items from the possible lists
            // - Field Dressing : Retrieve projectiles (broken or intact), generate some 'waste' items (kidneys, offal, etc)
            // - Skin Pelt : (All at end) raw pelts from creature based on size/species
            // - Harvest : Retrieve special ingredients from carcass, wolf eyes, spider venom sacs, etc
            // - Butchet Meat : Generate meat chunks

            // Butchery can be invoked multiple times.
            // When we invoke Finish(), it will set the flag for this stage as complete and allow a different stage to be performed.
            // The user should butcher the carcass again to invoke the next stage.

            return true;
        }

        /// <inheritdoc />
        public override void Finish() {
            // Set the flag for the stage we're currently on.
            _targetButcherable.Stage |= _currentButcherStage;
            base.Finish();
        }
    }
}