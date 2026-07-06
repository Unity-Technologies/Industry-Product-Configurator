using System.Collections.Generic;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
{
    /// <summary>Id-based rule input for <see cref="CompatibilityEvaluator"/> (resolved from a CompatibilityRule).</summary>
    public struct CompatibilityRuleInput
    {
        public string WhenVariantId;
        public CompatibilityRuleType RuleType;
        public IReadOnlyList<string> TargetVariantIds;
        public bool Mutual;
    }

    public struct UnmetRequirement
    {
        public string SetId;
        public string RequiredVariantId;
    }

    public class CompatibilityResult
    {
        /// <summary>Variants that cannot currently be selected given the current selection.</summary>
        public HashSet<string> RestrictedVariantIds = new();
        /// <summary>Variants a rule says must be selected given the current selection.</summary>
        public HashSet<string> RequiredVariantIds = new();
        /// <summary>Required variants whose set currently has a different (or no) variant selected.</summary>
        public List<UnmetRequirement> UnmetRequirements = new();
        /// <summary>Currently-selected variants that are now restricted (an invalid configuration).</summary>
        public HashSet<string> InvalidSelectedVariantIds = new();
    }

    /// <summary>
    /// Pure evaluation of compatibility rules against a current selection. No UnityEngine dependency,
    /// so it is unit-testable in isolation (mirrors VariantUsageCalculator). Everything is keyed by
    /// AssetBase.UniqueIdString.
    /// </summary>
    public static class CompatibilityEvaluator
    {
        /// <param name="rules">The active rules.</param>
        /// <param name="currentSelectionBySet">setId -> currently selected variantId.</param>
        /// <param name="variantToSet">variantId -> the set it belongs to (used to detect unmet requirements).</param>
        public static CompatibilityResult Evaluate(
            IReadOnlyList<CompatibilityRuleInput> rules,
            IReadOnlyDictionary<string, string> currentSelectionBySet,
            IReadOnlyDictionary<string, string> variantToSet)
        {
            var result = new CompatibilityResult();
            if (rules == null) return result;

            var selectedVariantIds = new HashSet<string>();
            if (currentSelectionBySet != null)
            {
                foreach (var kv in currentSelectionBySet)
                    if (!string.IsNullOrEmpty(kv.Value)) selectedVariantIds.Add(kv.Value);
            }

            foreach (var rule in rules)
            {
                if (string.IsNullOrEmpty(rule.WhenVariantId) || rule.TargetVariantIds == null) continue;
                bool triggerSelected = selectedVariantIds.Contains(rule.WhenVariantId);

                if (rule.RuleType == CompatibilityRuleType.Restricts)
                {
                    if (triggerSelected)
                    {
                        foreach (var t in rule.TargetVariantIds)
                            if (!string.IsNullOrEmpty(t)) result.RestrictedVariantIds.Add(t);
                    }

                    // Mutual exclusion: while any target is selected, the trigger is restricted too.
                    if (rule.Mutual)
                    {
                        foreach (var t in rule.TargetVariantIds)
                        {
                            if (!string.IsNullOrEmpty(t) && selectedVariantIds.Contains(t))
                            {
                                result.RestrictedVariantIds.Add(rule.WhenVariantId);
                                break;
                            }
                        }
                    }
                }
                else // Requires
                {
                    if (!triggerSelected) continue;
                    foreach (var t in rule.TargetVariantIds)
                    {
                        if (string.IsNullOrEmpty(t)) continue;
                        result.RequiredVariantIds.Add(t);

                        // Unmet if the required variant's set currently shows a different variant (or none).
                        if (variantToSet != null && variantToSet.TryGetValue(t, out var setId))
                        {
                            var current = currentSelectionBySet != null && currentSelectionBySet.TryGetValue(setId, out var c) ? c : null;
                            if (!string.Equals(current, t))
                                result.UnmetRequirements.Add(new UnmetRequirement { SetId = setId, RequiredVariantId = t });
                        }
                    }
                }
            }

            // Any currently-selected variant that ended up restricted makes the configuration invalid.
            foreach (var vid in selectedVariantIds)
                if (result.RestrictedVariantIds.Contains(vid))
                    result.InvalidSelectedVariantIds.Add(vid);

            return result;
        }
    }
}
