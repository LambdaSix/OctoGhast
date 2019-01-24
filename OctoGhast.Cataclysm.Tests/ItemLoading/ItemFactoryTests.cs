using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OctoGhast.Cataclysm.LegacyLoader;

namespace OctoGhast.Cataclysm.Tests.ItemLoading {
    [TestFixture]
    public class ItemFactoryTests {
        [Test]
        public void LoadTemplates() {
            var sw = new Stopwatch();
            sw.Start();
            var itemFactory = new ItemTemplateFactory();
            itemFactory.LoadFrom("C:\\Users\\somervn\\Documents\\Code\\OctoGhast\\OctoGhast.Cataclysm\\data\\json");

            sw.Stop();
            Console.WriteLine($"Loaded {itemFactory.BaseTemplates.Count} templates in {sw.Elapsed.TotalSeconds} seconds");

            sw.Restart();
            itemFactory.LoadAbstracts();
            sw.Stop();
            Console.WriteLine($"Loaded {itemFactory.Abstracts.Count} abstracts in {sw.Elapsed.TotalSeconds} seconds");

            sw.Restart();
            itemFactory.LoadItemTemplates();
            sw.Stop();
            Console.WriteLine(
                $"Loaded {itemFactory.ItemTemplates.Count} item templates in {sw.Elapsed.TotalSeconds} seconds");
        }

        [Test]
        public void Foo() {
            var t = typeof(ItemType);
            foreach (var field in t.GetFields()) {
                Console.WriteLine(field);
            }
        }

        [Test]
        public void Inheritance() {
            var itemFactory = new ItemTemplateFactory();
            itemFactory.LoadFrom("C:\\Users\\somervn\\Documents\\Code\\OctoGhast\\OctoGhast.Cataclysm\\data\\json");

            var groups = itemFactory.BaseTemplates.GroupBy(s => s.Key.Type);
            foreach (var group in groups) {
                Console.WriteLine(group.Key);
            }

            itemFactory.LoadAbstracts();
            itemFactory.LoadItemTemplates();

            /*
            foreach (var item in itemFactory.Abstracts) {
                Console.WriteLine("Loaded: " + item.Type + "::" + item.Abstract);
            }
            */
        }
    }
}