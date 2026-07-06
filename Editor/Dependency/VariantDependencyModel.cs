using System.Collections.Generic;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    /// <summary>
    /// Pure data types describing which Product Configurator ScriptableObjects are used vs unused.
    /// Deliberately free of UnityEngine / UnityEditor dependencies so <see cref="VariantUsageCalculator"/>
    /// can be unit tested in isolation. All entities are identified by their AssetBase.UniqueIdString
    /// (not the Unity asset GUID).
    /// </summary>
    public enum NodeKind
    {
        VariantSet,
        Variant
    }

    /// <summary>A reference from one variant, in a set, to a target set+variant (conditional or combination).</summary>
    public class RelationRef
    {
        public string OwnerVariantId;
        public string TargetSetId;
        public string TargetVariantId;
    }

    /// <summary>
    /// The structural data read from a single VariantSetBase component living in a scene or prefab.
    /// Object references (members, conditionals) and string-GUID references (combos) are both
    /// represented here as plain id strings so usage reasoning treats them uniformly.
    /// </summary>
    public class SetInput
    {
        public string SetId;
        public List<string> MemberVariantIds = new();
        public List<RelationRef> Conditionals = new();
        public List<RelationRef> Combos = new();
    }

    /// <summary>
    /// Object-reference usage discovered for a single scene or prefab via
    /// AssetDatabase.GetDependencies. This never sees the string-GUID combination maps.
    /// </summary>
    public class SourceUsage
    {
        public HashSet<string> ReferencedSetIds = new();
        public HashSet<string> ReferencedVariantIds = new();
    }

    /// <summary>A combination map entry whose target set or variant no longer resolves to a live asset.</summary>
    public class DanglingComboEntry
    {
        public string ComboSetId;
        public string ComboVariantId;
        public string TargetSetId;
        public string TargetVariantId;
        public bool SetMissing;
        public bool VariantMissing;
    }

    /// <summary>A list row (built by the scanner, which has the AssetDatabase display data).</summary>
    public class DepNode
    {
        public string UniqueId;
        public string AssetGuid;
        public string AssetPath;
        public NodeKind Kind;
        public string DisplayName;
        public bool Unused;
    }

    /// <summary>
    /// Result of <see cref="VariantUsageCalculator.Compute"/>: which assets are (un)used, dangling
    /// combination entries, and orphaned icon files.
    /// </summary>
    public class UsageResult
    {
        public HashSet<string> UsedSetIds = new();
        public HashSet<string> UsedVariantIds = new();
        public HashSet<string> UnusedSetIds = new();
        public HashSet<string> UnusedVariantIds = new();
        public List<DanglingComboEntry> DanglingCombos = new();
        public List<string> OrphanIconPaths = new();
    }
}
