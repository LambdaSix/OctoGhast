namespace OctoGhast.Cataclysm.Loaders.Requirement {
    public static class RequirementTypeNamespace {
        public static string Name = "requirement";
    }

    public class RequirementType : TemplateType {
        public override string NamespaceName { get; } = RequirementTypeNamespace.Name;
    }
}