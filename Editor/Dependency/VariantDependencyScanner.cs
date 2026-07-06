using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IndustryCSE.Tool.ProductConfigurator.Runtime;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using IndustryCSE.Tool.ProductConfigurator.Settings.Editor;
using Object = UnityEngine.Object;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    /// <summary>
    /// Unity-side shell that gathers the project data <see cref="VariantUsageCalculator"/> needs and
    /// returns the finished result. Object-reference usage is collected from every scene and prefab
    /// via AssetDatabase.GetDependencies (the same safety net the legacy cleaner used); the richer
    /// structure (set membership, conditionals, and the string-GUID combination maps) is deep-read
    /// from prefabs and currently-open scenes. Closed scenes are never opened by this tool (Unity
    /// forbids opening scenes in read-only packages); to include a closed scene's combination maps,
    /// open that scene and scan again — its object-reference usage is always counted regardless.
    /// </summary>
    public static class VariantDependencyScanner
    {
        public class ScanResult
        {
            public List<DepNode> Nodes = new();
            public UsageResult Usage = new();
            /// <summary>Variant asset GUID -> its assigned icon texture, so the UI needs no per-row asset loads.</summary>
            public Dictionary<string, Texture2D> IconByGuid = new();
        }

        public static ScanResult Scan()
        {
            var nodes = new List<DepNode>();
            var iconByGuid = new Dictionary<string, Texture2D>();

            // path -> uniqueId, so GetDependencies results (which are asset paths) can be resolved.
            var setPathToId = new Dictionary<string, string>();
            var variantPathToId = new Dictionary<string, string>();

            var allSetIds = new HashSet<string>();
            var allVariantIds = new HashSet<string>();

            // --- 1. Enumerate every VariantSetAsset / VariantAsset in the project. ---
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(VariantSetAsset)}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<VariantSetAsset>(path);
                if (asset == null || string.IsNullOrEmpty(asset.UniqueIdString)) continue;
                WarnOnDuplicateId(allSetIds, asset.UniqueIdString, path);
                setPathToId[path] = asset.UniqueIdString;
                nodes.Add(new DepNode
                {
                    UniqueId = asset.UniqueIdString,
                    AssetGuid = guid,
                    AssetPath = path,
                    Kind = NodeKind.VariantSet,
                    DisplayName = string.IsNullOrEmpty(asset.VariantSetName) ? asset.name : asset.VariantSetName
                });
            }

            var iconFolders = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(VariantAsset)}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<VariantAsset>(path);
                if (asset == null || string.IsNullOrEmpty(asset.UniqueIdString)) continue;
                WarnOnDuplicateId(allVariantIds, asset.UniqueIdString, path);
                variantPathToId[path] = asset.UniqueIdString;
                nodes.Add(new DepNode
                {
                    UniqueId = asset.UniqueIdString,
                    AssetGuid = guid,
                    AssetPath = path,
                    Kind = NodeKind.Variant,
                    DisplayName = string.IsNullOrEmpty(asset.VariantName) ? asset.name : asset.VariantName
                });

                if (asset.icon != null)
                {
                    iconByGuid[guid] = asset.icon;

                    // Remember folders that actually hold variant icons, so orphan-icon detection works
                    // wherever icons live (e.g. a sample's own Icons folder), not only the configured path.
                    var iconDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(asset.icon));
                    if (!string.IsNullOrEmpty(iconDir))
                    {
                        iconDir = iconDir.Replace('\\', '/');
                        // Skip the Assets root: scanning it would treat every GUID-named PNG in the
                        // whole project as an orphan-icon candidate.
                        if (iconDir != "Assets") iconFolders.Add(iconDir);
                    }
                }
            }

            // --- 2. Object-reference usage from all scenes + prefabs (shallow, path-based). ---
            var sources = new List<SourceUsage>();
            var allScenePaths = AssetDatabase.FindAssets("t:Scene")
                .Select(AssetDatabase.GUIDToAssetPath).ToList();
            var allPrefabPaths = AssetDatabase.FindAssets("t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath).ToList();

            foreach (var path in allScenePaths.Concat(allPrefabPaths))
            {
                sources.Add(BuildSourceUsage(path, setPathToId, variantPathToId));
            }

            // --- 3. Deep structural read of prefabs + open scenes (members, conditionals, combos). ---
            var setInputs = new List<SetInput>();

            foreach (var path in allPrefabPaths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                foreach (var component in go.GetComponentsInChildren<VariantSetBase>(true))
                {
                    setInputs.Add(BuildSetInput(component));
                }
            }

            foreach (var component in Object.FindObjectsByType<VariantSetBase>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                setInputs.Add(BuildSetInput(component));
            }

            // --- 4. Orphan icon candidates: <variantId>.png files in the configured folder or any
            //        folder that currently holds variant icons. ---
            var iconMap = BuildIconMap(iconFolders);

            // --- 5. Reason about it all (pure). ---
            var usage = VariantUsageCalculator.Compute(setInputs, allSetIds, allVariantIds, sources, iconMap);

            foreach (var node in nodes)
            {
                node.Unused = node.Kind == NodeKind.VariantSet
                    ? usage.UnusedSetIds.Contains(node.UniqueId)
                    : usage.UnusedVariantIds.Contains(node.UniqueId);
            }

            return new ScanResult { Nodes = nodes, Usage = usage, IconByGuid = iconByGuid };
        }

        private static void WarnOnDuplicateId(HashSet<string> seen, string uniqueId, string path)
        {
            if (!seen.Add(uniqueId))
            {
                Debug.LogWarning($"[Product Configurator] Duplicate uniqueIdString '{uniqueId}' at '{path}'. " +
                                 "Two assets share an id (e.g. from duplicating a .asset); cleanup results for that id may be inaccurate.");
            }
        }

        private static SourceUsage BuildSourceUsage(string sourcePath,
            IReadOnlyDictionary<string, string> setPathToId,
            IReadOnlyDictionary<string, string> variantPathToId)
        {
            var usage = new SourceUsage();
            foreach (var dependency in AssetDatabase.GetDependencies(sourcePath, true))
            {
                if (setPathToId.TryGetValue(dependency, out var setId)) usage.ReferencedSetIds.Add(setId);
                if (variantPathToId.TryGetValue(dependency, out var variantId)) usage.ReferencedVariantIds.Add(variantId);
            }
            return usage;
        }

        private static SetInput BuildSetInput(VariantSetBase component)
        {
            var setInput = new SetInput { SetId = Uid(component.VariantSetAsset) };

            foreach (var variant in component.VariantBase)
            {
                if (variant == null) continue;
                var ownerId = Uid(variant.variantAsset);
                if (!string.IsNullOrEmpty(ownerId)) setInput.MemberVariantIds.Add(ownerId);

                foreach (var conditional in variant.conditionalVariants)
                {
                    setInput.Conditionals.Add(new RelationRef
                    {
                        OwnerVariantId = ownerId,
                        TargetSetId = Uid(conditional.variantSetAsset),
                        TargetVariantId = Uid(conditional.variantAsset)
                    });
                }
            }

            // Combination maps store the target set/variant as UniqueIdString strings directly.
            if (component is CombinationVariantSet combination)
            {
                foreach (var comboVariant in combination.Variants)
                {
                    if (comboVariant?.CombinationList?.KeyValuePairs == null) continue;
                    var ownerId = Uid(comboVariant.variantAsset);
                    foreach (var pair in comboVariant.CombinationList.KeyValuePairs)
                    {
                        setInput.Combos.Add(new RelationRef
                        {
                            OwnerVariantId = ownerId,
                            TargetSetId = pair.Key,
                            TargetVariantId = pair.Value
                        });
                    }
                }
            }

            return setInput;
        }

        private static Dictionary<string, string> BuildIconMap(HashSet<string> variantIconFolders)
        {
            var iconMap = new Dictionary<string, string>();

            var folders = new HashSet<string>(variantIconFolders ?? new HashSet<string>());
            var configured = PackageSettingsController.Settings.VariantIconPath.Replace('\\', '/').TrimEnd('/');
            if (!string.IsNullOrEmpty(configured)) folders.Add(configured);

            foreach (var folder in folders)
            {
                if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;

                    // Icons follow the "<variantUniqueId>.png" convention. Only GUID-named PNGs are
                    // treated as candidates (ordinary textures are never touched), and the stem is
                    // normalized to the canonical form so a case/format difference can't cause a
                    // false orphan against the lowercase-dashed uniqueIdString.
                    var stem = Path.GetFileNameWithoutExtension(path);
                    if (!System.Guid.TryParse(stem, out var parsed)) continue;
                    iconMap[path] = parsed.ToString();
                }
            }
            return iconMap;
        }

        private static string Uid(AssetBase asset) => asset != null ? asset.UniqueIdString : string.Empty;
    }
}
