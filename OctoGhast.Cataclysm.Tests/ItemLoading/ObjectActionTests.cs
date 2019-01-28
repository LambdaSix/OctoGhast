using System;
using System.Collections.Generic;
using System.Linq;
using InfiniMap;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Framework;
using OctoGhast.Framework.Items.Actions;
using OctoGhast.Framework.Mobile;

namespace OctoGhast.Cataclysm.Tests.ItemLoading {
    [TestFixture]
    public class ObjectActionTests {
        [Test]
        public void CanLoadAction() {
            var itemString = @"{
                ""type"": ""GENERIC"",
                ""id"": ""concrete"",
                ""mass"": ""5"",
                ""use_action"": ""dummy_action"",
                ""volume"": ""-11""
            }";

            var itype = new ItemType();
            var item = new ItemTypeLoader().Load(JObject.Parse(itemString), itype);

            Assert.That(item.UseActions, Is.Not.Null);
            Assert.That(item.UseActions.Count(), Is.AtLeast(1));

            Assert.That(item.HasUse, Is.True);
            Assert.That(item.CanUse("dummy_action"), Is.True);

            // GetUse is for supporting multiple use_actions
            Assert.AreEqual(item.GetUse("dummy_action"), "dummy_action");
        }

        [Test]
        public void CanLoadActions()
        {
            var itemString = @"{
                ""type"": ""GENERIC"",
                ""id"": ""concrete"",
                ""mass"": ""5"",
                ""use_action"": [ ""dummy_action"", ""fake_action"" ],
                ""volume"": ""-11""
            }";

            var itype = new ItemType();
            var item = new ItemTypeLoader().Load(JObject.Parse(itemString), itype);

            Assert.That(item.UseActions, Is.Not.Null);
            Assert.That(item.UseActions.Count(), Is.AtLeast(2));

            Assert.That(item.HasUse, Is.True);
            Assert.That(item.CanUse("dummy_action"), Is.True);
            Assert.That(item.CanUse("fake_action"), Is.True);

            // GetUse is for supporting multiple use_actions
            Assert.AreEqual(item.GetUse("dummy_action"), "dummy_action");
            Assert.AreEqual(item.GetUse("fake_action"), "fake_action");
        }

        private class FakePlayer : Player {
            public ICollection<string> MessageQueue { get; set; } = new List<string>();

            public FakePlayer(int serial) : base(serial) {
                
            }

            /// <inheritdoc />
            public override void SendMessage(string msg) {
                MessageQueue.Add(msg);
            }
        }

        [ItemUse("dummy_action")]
        public class DummyAction : ItemUse<TemplateType> {
            /// <inheritdoc />
            public override int Use(BaseCreature player, RLObject<TemplateType> item, bool turnTick, WorldSpace position) {
                var outStr = $"Dummy action called on {item.GetName()}";
                Console.WriteLine(outStr);
                player.SendMessage(outStr);
                return 1;
            }
        }

        [Test]
        public void ActionsFromUseRegistry() {
            // Also sneakily tests the auto-detection of new ItemUse handlers via DummyAction :)

            var itemString = @"{
                ""type"": ""GENERIC"",
                ""id"": ""concrete"",
                ""mass"": ""5"",
                ""name"": ""concrete chunk"",
                ""use_action"": [ ""NULL"", ""dummy_action"" ],
                ""volume"": ""-11""
            }";            

            var itype = new ItemTypeLoader().Load(JObject.Parse(itemString));
            var item = new RLObject<ItemType>(itype, 1);
            var useRegistry = new ItemUseRegistry();
            var fakePlayer = new FakePlayer(1);
            

            var usage = useRegistry.Retrieve(item.TemplateData.UseActions.First().Name);
            var res = usage(fakePlayer, item, false, new WorldSpace(0, 0, 0));

            Assert.That(res, Is.EqualTo(0));
            Assert.That(fakePlayer.MessageQueue.Count, Is.AtLeast(1));
            Assert.That(fakePlayer.MessageQueue.First(),
                Is.EqualTo($"You can't do anything interesting with your {item.GetName()}"));

            var usage2 = useRegistry.Retrieve(item.TemplateData.UseActions.ElementAt(1).Name);
            var res2 = usage2(fakePlayer, item, false, new WorldSpace(0, 0, 0));

            Assert.That(res2, Is.EqualTo(1));
            Assert.That(fakePlayer.MessageQueue.Count, Is.AtLeast(2));
            Assert.That(fakePlayer.MessageQueue.ElementAt(1),
                Is.EqualTo($"Dummy action called on {item.GetName()}"));
        }
    }
}