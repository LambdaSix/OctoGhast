using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OctoGhast.Spatial;

namespace OctoGhast.Core.Tests
{
    [TestFixture]
    public class WorldSpatialIndexTests
    {
        private sealed class TestCellMapper : ISpatialCellMapper
        {
            private readonly decimal cellSize;

            public TestCellMapper(decimal cellSize)
            {
                if (cellSize <= 0) throw new ArgumentOutOfRangeException("cellSize");
                this.cellSize = cellSize;
            }

            public SpatialCell GetCell(WorldPosition position)
            {
                return new SpatialCell(
                    decimal.ToInt64(decimal.Floor(position.X / cellSize)),
                    decimal.ToInt64(decimal.Floor(position.Y / cellSize)),
                    decimal.ToInt64(decimal.Floor(position.Z / cellSize)));
            }
        }

        private sealed class FailingCellMapper : ISpatialCellMapper
        {
            public SpatialCell GetCell(WorldPosition position)
            {
                if (position.X > 0m) throw new InvalidOperationException("Injected mapping failure.");
                return new SpatialCell(decimal.ToInt64(position.X), decimal.ToInt64(position.Y), decimal.ToInt64(position.Z));
            }
        }

        private static WorldSpatialIndex<string> CreateIndex()
        {
            return new WorldSpatialIndex<string>(new TestCellMapper(2m), StringComparer.Ordinal);
        }

        [Test]
        public void PositionAndCellAreSeparateDeterministicValues()
        {
            var fractionalPosition = new WorldPosition(-0.25m, 4.5m, 0m);
            var mapper = new TestCellMapper(2m);

            Assert.AreEqual(new SpatialCell(-1, 2, 0), mapper.GetCell(fractionalPosition));
            Assert.AreNotEqual(fractionalPosition, new WorldPosition(-1m, 4.5m, 0m));
            Assert.AreNotEqual(mapper.GetCell(fractionalPosition), mapper.GetCell(new WorldPosition(0m, 4.5m, 0m)));
        }

        [Test]
        public void SpawnRejectsDuplicateIdentityAndAllowsSeveralEntitiesPerCell()
        {
            var index = CreateIndex();
            var position = new WorldPosition(1m, 1m, 0m);

            Assert.IsTrue(index.TrySpawn("entity-b", position));
            Assert.IsTrue(index.TrySpawn("entity-a", position));
            Assert.IsFalse(index.TrySpawn("entity-a", new WorldPosition(40m, 0m, 0m)));

            CollectionAssert.AreEqual(new[] { "entity-a", "entity-b" }, index.GetEntitiesAt(new SpatialCell(0, 0, 0)));
            Assert.AreEqual(2, index.Count);
            Assert.IsTrue(index.TryGetPosition("entity-a", out var actual));
            Assert.AreEqual(position, actual);
        }

        [Test]
        public void MoveIsConditionalAndUpdatesPositionAndCellTogether()
        {
            var index = CreateIndex();
            var source = new WorldPosition(0.5m, 1m, 0m);
            var destinationWithinCell = new WorldPosition(1.5m, 1m, 0m);
            var destination = new WorldPosition(4m, 1m, 0m);
            index.TrySpawn("actor", source);

            Assert.IsFalse(index.TryMove("actor", new WorldPosition(9m, 9m, 0m), destination));
            Assert.IsTrue(index.TryMove("actor", source, destinationWithinCell));
            Assert.IsTrue(index.TryGetPosition("actor", out var current));
            Assert.AreEqual(destinationWithinCell, current);
            CollectionAssert.AreEqual(new[] { "actor" }, index.GetEntitiesAt(new SpatialCell(0, 0, 0)));

            Assert.IsTrue(index.TryMove("actor", destinationWithinCell, destination));
            CollectionAssert.IsEmpty(index.GetEntitiesAt(new SpatialCell(0, 0, 0)));
            CollectionAssert.AreEqual(new[] { "actor" }, index.GetEntitiesAt(new SpatialCell(2, 0, 0)));
        }

        [Test]
        public void DespawnRequiresExpectedPositionAndRemovesEmptyBuckets()
        {
            var index = CreateIndex();
            var position = new WorldPosition(4m, 2m, -2m);
            var cell = new SpatialCell(2, 1, -1);
            index.TrySpawn("actor", position);

            Assert.IsFalse(index.TryDespawn("actor", new WorldPosition(0m, 0m, 0m)));
            Assert.IsTrue(index.TryGetPosition("actor", out var unchanged));
            Assert.AreEqual(position, unchanged);
            Assert.IsTrue(index.TryDespawn("actor", position));
            Assert.IsFalse(index.TryGetPosition("actor", out _));
            Assert.IsFalse(index.TryDespawn("actor", position));
            Assert.AreEqual(0, index.Count);
            CollectionAssert.IsEmpty(index.GetEntitiesAt(cell));
        }

        [Test]
        public void MappingFailureLeavesThePublishedSourcePositionAndMembershipIntact()
        {
            var index = new WorldSpatialIndex<string>(new FailingCellMapper(), StringComparer.Ordinal);
            var source = new WorldPosition(0m, 1m, 0m);
            index.TrySpawn("actor", source);

            Assert.Throws<InvalidOperationException>(() => index.TryMove("actor", source, new WorldPosition(1m, 1m, 0m)));
            Assert.IsTrue(index.TryGetPosition("actor", out var current));
            Assert.AreEqual(source, current);
            CollectionAssert.AreEqual(new[] { "actor" }, index.GetEntitiesAt(new SpatialCell(0, 1, 0)));
        }

        [Test]
        public void ConcurrentMovesFromSameExpectedPositionHaveOneWinner()
        {
            var index = new WorldSpatialIndex<string>(new TestCellMapper(1m), StringComparer.Ordinal);
            var initial = new WorldPosition(0m, 0m, 0m);
            index.TrySpawn("actor", initial);
            var winners = 0;

            Parallel.For(0, 32, i =>
            {
                if (index.TryMove("actor", initial, new WorldPosition(i + 1m, 0m, 0m)))
                    Interlocked.Increment(ref winners);
            });

            Assert.AreEqual(1, winners);
            Assert.AreEqual(1, index.Count);
            Assert.IsTrue(index.TryGetPosition("actor", out var current));
            Assert.AreNotEqual(initial, current);
            CollectionAssert.AreEqual(new[] { "actor" }, index.GetEntitiesAt(new TestCellMapper(1m).GetCell(current)));
        }

        [Test]
        public void WorldOwnedIndexCanRepresentSeparatedActiveRegionsWithoutPlayerDuplication()
        {
            // Region activation and interest leases are separate concerns. The one world-owned
            // index keeps distant entities addressable and does not duplicate overlap membership.
            var index = new WorldSpatialIndex<string>(new TestCellMapper(1m), StringComparer.Ordinal);
            index.TrySpawn("player-a-actor", new WorldPosition(-10000m, 0m, 0m));
            index.TrySpawn("player-b-actor", new WorldPosition(10000m, 0m, 0m));
            index.TrySpawn("shared-world-actor", new WorldPosition(0m, 0m, 0m));

            Assert.AreEqual(3, index.Count);
            Assert.AreEqual(3, index.Snapshot().Count);
            Assert.IsTrue(index.TryGetPosition("shared-world-actor", out var sharedPosition));
            Assert.AreEqual(new WorldPosition(0m, 0m, 0m), sharedPosition);
            CollectionAssert.AreEqual(new[] { "shared-world-actor" }, index.GetEntitiesInCells(new[]
            {
                new SpatialCell(0, 0, 0),
                new SpatialCell(0, 0, 0) // overlapping active-region requirements do not duplicate results
            }));
        }

        [Test]
        public void RejectsMissingMapperComparerAndNullEntityIds()
        {
            Assert.Throws<ArgumentNullException>(() => new WorldSpatialIndex<string>(null, StringComparer.Ordinal));
            Assert.Throws<ArgumentNullException>(() => new WorldSpatialIndex<string>(new TestCellMapper(1m), null));
            var index = CreateIndex();
            Assert.Throws<ArgumentNullException>(() => index.TrySpawn(null, new WorldPosition(0m, 0m, 0m)));
        }
    }
}
