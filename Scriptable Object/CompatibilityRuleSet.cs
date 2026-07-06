using System.Collections.Generic;
using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator.ScriptableObjects
{
    public enum CompatibilityRuleType
    {
        /// <summary>While <see cref="CompatibilityRule.whenSelected"/> is selected, the targets cannot be selected.</summary>
        Restricts,
        /// <summary>While <see cref="CompatibilityRule.whenSelected"/> is selected, a target must be selected.</summary>
        Requires
    }

    [System.Serializable]
    public class CompatibilityRule
    {
        [Tooltip("The variant that, when selected, activates this rule.")]
        public VariantAsset whenSelected;

        public CompatibilityRuleType ruleType = CompatibilityRuleType.Restricts;

        [Tooltip("The variants (in other sets) this rule restricts or requires.")]
        public List<VariantAsset> targets = new();

        [Tooltip("Restricts only: also restrict 'When Selected' while a target is selected (mutual exclusion, A <-> B). Ignored for Requires.")]
        public bool mutual = true;
    }

    /// <summary>
    /// A central, editable list of cross-set compatibility rules. Consumed at runtime by
    /// CompatibilityController, which exposes a query API so a developer's own UI can reflect
    /// which variants are restricted/required. This asset holds no UI concerns.
    /// </summary>
    [CreateAssetMenu(fileName = "CompatibilityRuleSet", menuName = "Product Configurator/Compatibility Rule Set")]
    public class CompatibilityRuleSet : ScriptableObject
    {
        [SerializeField]
        private List<CompatibilityRule> rules = new();

        public IReadOnlyList<CompatibilityRule> Rules => rules;

        // --- Edit API (usable at runtime and in the editor) ---
        public void AddRule(CompatibilityRule rule)
        {
            if (rule != null) rules.Add(rule);
        }

        public bool RemoveRule(CompatibilityRule rule) => rules.Remove(rule);

        public void ClearRules() => rules.Clear();
    }
}
