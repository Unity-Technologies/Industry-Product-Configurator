using System.Collections.Generic;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    /// <summary>
    /// Pure reasoning core for the cleanup tool. Given the structural data of every variant set
    /// (<see cref="SetInput"/>), the full set of assets in the project, the object-reference usage
    /// of every scene/prefab (<see cref="SourceUsage"/>) and the icon file map, it decides what is
    /// safe to delete.
    ///
    /// Crucially, a reference counts as usage whether it is a Unity object reference OR a plain
    /// string GUID stored in a combination map. This is the correctness the AssetDatabase-only
    /// approach in the legacy Dependencies Cleaner lacked: a variant reachable only through a
    /// CombinationList Value would otherwise be reported unused and deleted, silently breaking
    /// the combination at runtime.
    ///
    /// No UnityEngine / UnityEditor dependency — see VariantUsageCalculatorTests.
    /// </summary>
    public static class VariantUsageCalculator
    {
        public static UsageResult Compute(
            IReadOnlyList<SetInput> sets,
            IReadOnlyCollection<string> allSetIds,
            IReadOnlyCollection<string> allVariantIds,
            IReadOnlyList<SourceUsage> sources,
            IReadOnlyDictionary<string, string> iconFileToVariantId)
        {
            var result = new UsageResult();

            var setIds = new HashSet<string>(allSetIds ?? System.Array.Empty<string>());
            var variantIds = new HashSet<string>(allVariantIds ?? System.Array.Empty<string>());

            // 1. Usage from the structural data of each live variant set component. The mere presence
            //    of a SetInput means a real component in a scene/prefab references these assets (by
            //    object ref for members/conditionals, by string for combos) — so all are "used".
            if (sets != null)
            {
                foreach (var set in sets)
                {
                    if (set == null) continue;

                    MarkSetUsed(result, set.SetId);

                    foreach (var memberId in set.MemberVariantIds)
                    {
                        MarkVariantUsed(result, memberId);
                    }

                    foreach (var conditional in set.Conditionals)
                    {
                        if (conditional == null) continue;
                        MarkSetUsed(result, conditional.TargetSetId);
                        MarkVariantUsed(result, conditional.TargetVariantId);
                    }

                    foreach (var combo in set.Combos)
                    {
                        if (combo == null) continue;
                        MarkSetUsed(result, combo.TargetSetId);
                        MarkVariantUsed(result, combo.TargetVariantId);

                        // A combo maps by string GUID; if the target no longer resolves to a live
                        // asset the entry is dangling and must be surfaced, not deleted silently.
                        bool setMissing = !string.IsNullOrEmpty(combo.TargetSetId) && !setIds.Contains(combo.TargetSetId);
                        bool variantMissing = !string.IsNullOrEmpty(combo.TargetVariantId) && !variantIds.Contains(combo.TargetVariantId);
                        if (setMissing || variantMissing)
                        {
                            result.DanglingCombos.Add(new DanglingComboEntry
                            {
                                ComboSetId = set.SetId,
                                ComboVariantId = combo.OwnerVariantId,
                                TargetSetId = combo.TargetSetId,
                                TargetVariantId = combo.TargetVariantId,
                                SetMissing = setMissing,
                                VariantMissing = variantMissing
                            });
                        }
                    }
                }
            }

            // 2. Broader object-reference usage from scene/prefab dependency scans.
            if (sources != null)
            {
                foreach (var source in sources)
                {
                    if (source == null) continue;
                    foreach (var id in source.ReferencedSetIds) MarkSetUsed(result, id);
                    foreach (var id in source.ReferencedVariantIds) MarkVariantUsed(result, id);
                }
            }

            // 3. Unused = everything in the project minus what is used.
            foreach (var id in setIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (!result.UsedSetIds.Contains(id)) result.UnusedSetIds.Add(id);
            }

            foreach (var id in variantIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (!result.UsedVariantIds.Contains(id)) result.UnusedVariantIds.Add(id);
            }

            // 4. Orphan icons: a PNG named <variantId>.png whose variant no longer exists.
            if (iconFileToVariantId != null)
            {
                foreach (var pair in iconFileToVariantId)
                {
                    if (string.IsNullOrEmpty(pair.Value) || !variantIds.Contains(pair.Value))
                    {
                        result.OrphanIconPaths.Add(pair.Key);
                    }
                }
            }

            return result;
        }

        private static void MarkSetUsed(UsageResult result, string id)
        {
            if (!string.IsNullOrEmpty(id)) result.UsedSetIds.Add(id);
        }

        private static void MarkVariantUsed(UsageResult result, string id)
        {
            if (!string.IsNullOrEmpty(id)) result.UsedVariantIds.Add(id);
        }
    }
}
