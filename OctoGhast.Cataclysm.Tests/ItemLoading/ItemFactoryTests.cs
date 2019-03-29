using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Cataclysm.Loaders.Item;

namespace OctoGhast.Cataclysm.Tests.ItemLoading {
    [TestFixture]
    public class ItemFactoryTests {
        [Test]
        public void LoadTemplates() {
            var itemFactory = new ItemTemplateFactory();

            var t = new TimeSpan();
            var sw = new Stopwatch();
            sw.Start();
            itemFactory.LoadFrom(TestContext.CurrentContext.TestDirectory + "\\data\\core");
            itemFactory.LoadFrom(TestContext.CurrentContext.TestDirectory + "\\data\\json");

            sw.Stop();
            t = t.Add(sw.Elapsed);
            Console.WriteLine($"Loaded {itemFactory.BaseTemplates.Count} templates in {sw.Elapsed.TotalSeconds} seconds");

            sw.Restart();
            itemFactory.LoadAbstracts();
            sw.Stop();
            t = t.Add(sw.Elapsed);
            Console.WriteLine($"Loaded {itemFactory.Abstracts.Count} abstracts in {sw.Elapsed.TotalSeconds} seconds");

            sw.Restart();
            itemFactory.LoadItemTemplates();
            sw.Stop();
            t = t.Add(sw.Elapsed);
            Console.WriteLine(
                $"Loaded {itemFactory.ItemTemplates.Count} ItemTypes in {sw.Elapsed.TotalSeconds} seconds");

            foreach (var group in itemFactory.ItemGroups)
            {
                Console.WriteLine($"\t{group.Key} - {group.Value.Count()} items");
            }

            Console.WriteLine($"Total Time: {t:g}");
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
            itemFactory.LoadFrom(TestContext.CurrentContext.TestDirectory + "\\data\\core");
            itemFactory.LoadFrom(TestContext.CurrentContext.TestDirectory + "\\data\\json");

            
            foreach (var group in itemFactory.BaseTemplateGroups) {
                Console.WriteLine($"{group.Key} - {group.Value.Count()} templates");
            }

            itemFactory.LoadAbstracts();
            itemFactory.LoadItemTemplates();

            foreach (var group in itemFactory.ItemGroups) {
                Console.WriteLine($"{group.Key} - {group.Value.Count()} items");
            }

            /*
            foreach (var item in itemFactory.Abstracts) {
                Console.WriteLine("Loaded: " + item.Type + "::" + item.Abstract);
            }
            */
      }
    }
}