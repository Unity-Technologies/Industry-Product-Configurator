using System;
using System.Collections.Generic;
using UnityEngine;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
{
    /// <summary>
    /// Applies a <see cref="CompatibilityRuleSet"/> at runtime. It listens to variant selections,
    /// keeps the configuration valid by auto-switching a set whose selection becomes restricted, and
    /// exposes a query API + a <see cref="Changed"/> event so a developer's own UI can grey out /
    /// hide / message restricted or required variants. It does not touch any UI itself.
    ///
    /// Developers who want full control can bypass this and call <see cref="CompatibilityEvaluator"/>
    /// directly.
    /// </summary>
    public class CompatibilityController : MonoBehaviour
    {
        [SerializeField] private CompatibilityRuleSet ruleSet;
        [Tooltip("When a new selection restricts a variant selected in another set, switch that set to a valid variant.")]
        [SerializeField] private bool autoSwitchOnConflict = true;

        /// <summary>Raised after every re-evaluation so UI can refresh its availability display.</summary>
        public event Action Changed;

        private readonly Dictionary<string, VariantSetBase> _setById = new();   // setUniqueId -> set
        private readonly Dictionary<string, string> _variantToSet = new();       // variantUniqueId -> setUniqueId
        private readonly Dictionary<string, VariantAsset> _variantById = new();  // variantUniqueId -> asset
        private List<CompatibilityRuleInput> _ruleInputs = new();
        private CompatibilityResult _result = new();
        private bool _applying;

        public CompatibilityRuleSet RuleSet
        {
            get => ruleSet;
            set
            {
                ruleSet = value;
                RebuildRules();
                if (isActiveAndEnabled) Reevaluate(CurrentSelection(), null);
            }
        }

        protected virtual void OnEnable()
        {
            Refresh();
            VariantSetBase.VariantTriggered += OnVariantTriggered;
        }

        protected virtual void OnDisable()
        {
            VariantSetBase.VariantTriggered -= OnVariantTriggered;
        }

        /// <summary>Re-discover variant sets and rules (call if sets are added/removed at runtime).</summary>
        public void Refresh()
        {
            BuildMaps();
            RebuildRules();
            Reevaluate(CurrentSelection(), null);
        }

        private void BuildMaps()
        {
            _setById.Clear();
            _variantToSet.Clear();
            _variantById.Clear();

            foreach (var set in FindObjectsByType<VariantSetBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (set.VariantSetAsset == null) continue;
                var setId = set.VariantSetAsset.UniqueIdString;
                _setById[setId] = set;
                foreach (var variant in set.VariantBase)
                {
                    if (variant?.variantAsset == null) continue;
                    var vid = variant.variantAsset.UniqueIdString;
                    _variantToSet[vid] = setId;
                    _variantById[vid] = variant.variantAsset;
                }
            }
        }

        private void RebuildRules()
        {
            _ruleInputs = new List<CompatibilityRuleInput>();
            if (ruleSet == null) return;

            foreach (var rule in ruleSet.Rules)
            {
                if (rule?.whenSelected == null || rule.targets == null) continue;
                var targetIds = new List<string>();
                foreach (var t in rule.targets)
                    if (t != null) targetIds.Add(t.UniqueIdString);

                _ruleInputs.Add(new CompatibilityRuleInput
                {
                    WhenVariantId = rule.whenSelected.UniqueIdString,
                    RuleType = rule.ruleType,
                    TargetVariantIds = targetIds,
                    Mutual = rule.mutual
                });
            }
        }

        private Dictionary<string, string> CurrentSelection()
        {
            var selection = new Dictionary<string, string>();
            foreach (var kv in _setById)
            {
                var guid = kv.Value.CurrentSelectionGuid;
                if (!string.IsNullOrEmpty(guid)) selection[kv.Key] = guid;
            }
            return selection;
        }

        private void OnVariantTriggered(VariantSetAsset set, VariantAsset variant, bool triggerConditional)
        {
            if (_applying) return; // ignore our own auto-switch churn

            var selection = CurrentSelection();
            // The just-triggered selection is authoritative regardless of listener ordering
            // (the owning set may not have applied it yet when this handler runs).
            if (set != null && variant != null) selection[set.UniqueIdString] = variant.UniqueIdString;

            Reevaluate(selection, variant != null ? variant.UniqueIdString : null);
        }

        private void Reevaluate(Dictionary<string, string> selection, string justSelectedVariantId)
        {
            _result = CompatibilityEvaluator.Evaluate(_ruleInputs, selection, _variantToSet);

            if (autoSwitchOnConflict && _result.InvalidSelectedVariantIds.Count > 0)
            {
                _applying = true;
                try
                {
                    int guard = 0;
                    while (_result.InvalidSelectedVariantIds.Count > 0 && guard++ < 16)
                    {
                        bool switchedAny = false;
                        foreach (var invalidVid in new List<string>(_result.InvalidSelectedVariantIds))
                        {
                            if (invalidVid == justSelectedVariantId) continue; // keep the user's new choice
                            if (!_variantToSet.TryGetValue(invalidVid, out var setId)) continue;
                            if (_setById.TryGetValue(setId, out var set) && SwitchToAllowed(set)) switchedAny = true;
                        }
                        if (!switchedAny) break;
                        _result = CompatibilityEvaluator.Evaluate(_ruleInputs, CurrentSelection(), _variantToSet);
                    }
                }
                finally { _applying = false; }
            }

            Changed?.Invoke();
        }

        // Switch a set to a non-restricted variant (prefer its default index), applying without cascading.
        private bool SwitchToAllowed(VariantSetBase set)
        {
            var variants = set.VariantBase;
            if (variants == null || variants.Count == 0) return false;

            int current = set.CurrentSelectionIndex;
            int preferred = set.UseDefaultVariantIndex && set.DefaultVariantIndex >= 0 && set.DefaultVariantIndex < variants.Count
                ? set.DefaultVariantIndex
                : -1;

            int target = -1;
            if (preferred >= 0 && preferred != current && IsAllowed(variants, preferred)) target = preferred;
            if (target < 0)
            {
                for (int i = 0; i < variants.Count; i++)
                {
                    if (i == current) continue;
                    if (IsAllowed(variants, i)) { target = i; break; }
                }
            }
            if (target < 0) return false; // no allowed alternative

            set.SetVariant(target, false);
            return true;
        }

        private bool IsAllowed(List<VariantBase> variants, int index)
        {
            var asset = variants[index]?.variantAsset;
            return asset != null && !_result.RestrictedVariantIds.Contains(asset.UniqueIdString);
        }

        // ---------- Query API ----------
        public bool IsRestricted(VariantAsset variant) => variant != null && _result.RestrictedVariantIds.Contains(variant.UniqueIdString);
        public bool IsAvailable(VariantAsset variant) => !IsRestricted(variant);
        public bool IsRequired(VariantAsset variant) => variant != null && _result.RequiredVariantIds.Contains(variant.UniqueIdString);
        public bool IsConfigurationValid => _result.InvalidSelectedVariantIds.Count == 0 && _result.UnmetRequirements.Count == 0;

        public IEnumerable<VariantAsset> RestrictedVariants => Resolve(_result.RestrictedVariantIds);
        public IEnumerable<VariantAsset> RequiredVariants => Resolve(_result.RequiredVariantIds);

        /// <summary>Required variants whose set currently has a different (or no) variant selected.</summary>
        public IReadOnlyList<UnmetRequirement> UnmetRequirements => _result.UnmetRequirements;

        private IEnumerable<VariantAsset> Resolve(HashSet<string> ids)
        {
            foreach (var id in ids)
                if (_variantById.TryGetValue(id, out var asset)) yield return asset;
        }
    }
}
