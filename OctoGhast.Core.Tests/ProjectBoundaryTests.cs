using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace OctoGhast.Core.Tests {
    [TestFixture]
    public class ProjectBoundaryTests {
        private static readonly IDictionary<string, string[]> ExpectedProjectReferences =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
                ["OctoGhast"] = new[] {
                    "Capsicum", "InfiniMap", "RenderLike", "OctoGhast.DataStructures", "OctoGhast.MapGeneration",
                    "OctoGhast.Spatial", "OctoGhast.UserInterface"
                },
                ["OctoGhast.Cataclysm"] = new[] { "InfiniMap", "OctoGhast.Spatial", "OctoGhast" },
                ["OctoGhast.Server"] = new[] { "OctoGhast" },
                ["OctoGhast.UserInterface"] = new[] { "RenderLike", "OctoGhast.DataStructures", "OctoGhast.Spatial" },
                ["OctoGhast.DataStructures"] = new[] { "InfiniMap", "OctoGhast.Spatial" },
                ["OctoGhast.MapGeneration"] = new[] { "RenderLike", "OctoGhast.DataStructures", "OctoGhast.Spatial" },
                ["OctoGhast.Spatial"] = new string[0],
                ["OctoGhast.Core.Tests"] = new[] { "Capsicum", "OctoGhast", "OctoGhast.Spatial" },
                ["OctoGhast.Cataclysm.Tests"] = new[] { "InfiniMap", "OctoGhast.Cataclysm", "OctoGhast" },
                ["OctoGhast.Shell.Win32"] = new[] { "OctoGhast.Spatial", "OctoGhast.UserInterface", "OctoGhast" }
            };

        private static readonly ISet<string> KnownLegacyHostProfileLeaks = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "CoreMaterials.cs",
            Path.Combine("Framework", "Material.cs"),
            Path.Combine("Framework", "Data", "Loading", "BaseTemplateType.cs"),
            Path.Combine("Framework", "Data", "Loading", "TemplateFactoryBase.cs"),
            "UnitQuantity.cs"
        };

        [Test]
        public void Scoped_projects_have_only_reviewed_direct_project_references() {
            var root = FindRepositoryRoot();

            foreach (var expected in ExpectedProjectReferences) {
                var projectPath = Path.Combine(root, expected.Key, expected.Key + ".csproj");
                Assert.That(File.Exists(projectPath), Is.True, "Missing project: " + projectPath);

                var project = XDocument.Load(projectPath);
                var actual = project.Descendants()
                    .Where(element => element.Name.LocalName == "ProjectReference")
                    .Select(element => (string) element.Attribute("Include"))
                    .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar)))
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var reviewed = expected.Value.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

                Assert.That(actual, Is.EqualTo(reviewed),
                    expected.Key + " changed its project references. Update docs/architecture/project-boundaries.md " +
                    "and this reviewed allowlist together after checking the dependency direction.");
            }
        }

        [Test]
        public void Presentation_and_server_edges_do_not_gain_profile_or_reverse_dependencies() {
            var root = FindRepositoryRoot();
            AssertReferenceSet(root, "OctoGhast.UserInterface", "RenderLike", "OctoGhast.DataStructures", "OctoGhast.Spatial");
            AssertReferenceSet(root, "OctoGhast.Server", "OctoGhast");

            var uiReferences = ReadReferences(root, "OctoGhast.UserInterface");
            Assert.That(uiReferences, Does.Not.Contain("OctoGhast.Cataclysm"));
            Assert.That(uiReferences, Does.Not.Contain("OctoGhast.Server"));
            Assert.That(uiReferences, Does.Not.Contain("OctoGhast"),
                "The client presentation layer must not gain a direct dependency on the legacy authoritative/game host.");

            var serverReferences = ReadReferences(root, "OctoGhast.Server");
            Assert.That(serverReferences, Does.Not.Contain("OctoGhast.UserInterface"));
            Assert.That(serverReferences, Does.Not.Contain("CataSharp.Client"));
        }

        [Test]
        public void Legacy_host_profile_namespace_leakage_does_not_expand() {
            var root = Path.Combine(FindRepositoryRoot(), "OctoGhast");
            var leakedFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("OctoGhast.Cataclysm"))
                .Select(path => path.Substring(root.Length + 1))
                .ToArray();

            CollectionAssert.AreEquivalent(KnownLegacyHostProfileLeaks, leakedFiles,
                "The named OctoGhast project is still a legacy hybrid, but no new Core/host source may acquire " +
                "Cataclysm namespace coupling. Remove a known leak by migrating its type to the profile, then update " +
                "docs/architecture/project-boundaries.md and this list together.");
        }

        [Test]
        public void Extracted_core_project_must_not_reference_profile_or_presentation_projects() {
            var root = FindRepositoryRoot();
            var corePath = Path.Combine(root, "OctoGhast.Core", "OctoGhast.Core.csproj");
            if (!File.Exists(corePath)) {
                Assert.Pass("No separately extracted OctoGhast.Core project exists on this legacy baseline.");
            }

            var forbidden = new[] { "OctoGhast.Cataclysm", "OctoGhast.UserInterface", "OctoGhast.Server", "OctoGhast.Shell.Win32" };
            var actual = ReadReferences(root, "OctoGhast.Core");
            foreach (var projectName in forbidden) {
                Assert.That(actual, Does.Not.Contain(projectName), "Generic Core must not reference " + projectName + ".");
            }
        }

        private static void AssertReferenceSet(string root, string projectName, params string[] expected) {
            CollectionAssert.AreEquivalent(expected, ReadReferences(root, projectName), projectName + " project references");
        }

        private static string[] ReadReferences(string root, string projectName) {
            var path = Path.Combine(root, projectName, projectName + ".csproj");
            var project = XDocument.Load(path);
            return project.Descendants()
                .Where(element => element.Name.LocalName == "ProjectReference")
                .Select(element => (string) element.Attribute("Include"))
                .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar)))
                .ToArray();
        }

        private static string FindRepositoryRoot() {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "OctoGhast.sln"))) {
                directory = directory.Parent;
            }

            if (directory == null) {
                throw new DirectoryNotFoundException("Could not find OctoGhast.sln above the test directory.");
            }

            return directory.FullName;
        }
    }
}
