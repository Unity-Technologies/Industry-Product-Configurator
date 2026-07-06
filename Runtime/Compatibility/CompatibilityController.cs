using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
{
    /// <summary>
    /// Applies a <see cref="CompatibilityRuleSet"/> at runtime. It listens to variant selections,
    /// attempts to keep the configuration valid by auto-switching a set whose selection becomes
    /// restricted, and exposes a query API + a <see cref="Changed"/> event so a developer's own UI can
    /// grey out / hide / message restricted or required variants. It does not touch any UI itself.
    ///
    /// If a conflict cannot be resolved (no allowed alternative exists) the configuration is left
    /// invalid and reported via <see cref="IsConfigurationValid"/> (a warning is also logged).
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

        private readonly Dictionary<string, VariantSetBase> _setById = new();      // setUniqueId -> set
        private readonly Dictionary<string, string> _variantToSet = new();          // variantUniqueId -> setUniqueId
        private readonly Dictionary<string, VariantAsset> _variantById = new();     // variantUniqueId -> asset
        private readonly Dictionary<string, List<VariantBase>> _variantsBySetId = new(); // setUniqueId -> its variants (cached; VariantBase getter allocates)
        private readonly List<VariantSetBase> _subscribedSets = new();
        private List<CompatibilityRuleInput> _ruleInputs = new();
        private CompatibilityResult _result = new();
        private bool _applying;

        public CompatibilityRuleSet RuleSet
        {
            get => ruleSet;
            set
            {
                if (ruleSet == value) return; // avoid redundant rebuild/re-evaluation
                ruleSet = value;
                RebuildRules();
                if (isActiveAndEnabled) Reevaluate(CurrentSelection(), null);
            }
        }

        protected virtual void OnEnable()
        {
            Refresh();
            // Variant sets added by an additively-loaded scene should be picked up automatically.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected virtual void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeSets();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Refresh();

        /// <summary>Re-discover variant sets and rules (also called automatically on scene load).</summary>
        public void Refresh()
        {
            BuildMaps();
            RebuildRules();
            Reevaluate(CurrentSelection(), null);
        }

        private void BuildMaps()
        {
            UnsubscribeSets();
            _setById.Clear();
            _variantToSet.Clear();
            _variantById.Clear();
            _variantsBySetId.Clear();

            foreach (var set in FindObjectsByType<VariantSetBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (set.VariantSetAsset == null) continue;
                var setId = set.VariantSetAsset.UniqueIdString;
                _setById[setId] = set;

                var variants = set.VariantBase; // getter allocates a list; fetch once and cache it
                _variantsBySetId[setId] = variants;
                foreach (var variant in variants)
                {
                    if (variant?.variantAsset == null) continue;
                    var vid = variant.variantAsset.UniqueIdString;
                    if (_variantToSet.ContainsKey(vid))
                    {
                        Debug.LogWarning($"[CompatibilityController] Variant '{variant.variantAsset.VariantName}' is used by more than one variant set; " +
                                         "compatibility rules will resolve it to the first set only.", set);
                        continue; // first-write-wins keeps behaviour deterministic
                    }
                    _variantToSet[vid] = setId;
                    _variantById[vid] = variant.variantAsset;
                }

                // VariantChanged fires for BOTH VariantTriggered-driven and SetVariant-driven changes
                // (defaults applied in Start, programmatic SetVariant, our own auto-switch), so it is the
                // single signal that keeps the controller in sync with the actual selection state.
                set.VariantChanged += OnSetVariantChanged;
                _subscribedSets.Add(set);
            }
        }

        private void UnsubscribeSets()
        {
            foreach (var set in _subscribedSets)
                if (set != null) set.VariantChanged -= OnSetVariantChanged;
            _subscribedSets.Clear();
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

        private void OnSetVariantChanged(VariantBase changed)
        {
            if (_applying) return; // ignore our own auto-switch churn
            var justSelected = changed?.variantAsset != null ? changed.variantAsset.UniqueIdString : null;
            Reevaluate(CurrentSelection(), justSelected);
        }

        private void Reevaluate(Dictionary<string, string> selection, string justSelectedVariantId)
        {
            _result = CompatibilityEvaluator.Evaluate(_ruleInputs, selection, _variantToSet);

            if (autoSwitchOnConflict && _result.InvalidSelectedVariantIds.Count > 0)
            {
                _applying = true;
                try
                {
                    var unresolved = new HashSet<string>();
                    int guard = 0;
                    while (guard++ < 32)
                    {
                        string toFix = null;
                        foreach (var invalidVid in _result.InvalidSelectedVariantIds)
                        {
                            if (invalidVid == justSelectedVariantId) continue; // keep the user's new choice
                            if (unresolved.Contains(invalidVid)) continue;      // already known unfixable
                            toFix = invalidVid;
                            break;
                        }
                        if (toFix == null) break;

                        if (_variantToSet.TryGetValue(toFix, out var setId)
                            && _setById.TryGetValue(setId, out var set)
                            && SwitchToAllowed(set))
                        {
                            // Re-evaluate after EACH switch so the next decision uses fresh restriction data.
                            _result = CompatibilityEvaluator.Evaluate(_ruleInputs, CurrentSelection(), _variantToSet);
                        }
                        else
                        {
                            unresolved.Add(toFix); // no allowed alternative for this one; try the others
                        }
                    }
                }
                finally { _applying = false; }

                if (_result.InvalidSelectedVariantIds.Count > 0)
                {
                    Debug.LogWarning("[CompatibilityController] Could not auto-resolve all compatibility conflicts; " +
                                     "the configuration is invalid. Inspect IsConfigurationValid / RestrictedVariants to handle it in UI.", this);
                }
            }

            Changed?.Invoke();
        }

        // Switch a set to a non-restricted variant (prefer its default index), announcing the change
        // through VariantTriggered so cost labels and other listeners stay in sync. Our own re-entry is
        // suppressed by the _applying guard.
        private bool SwitchToAllowed(VariantSetBase set)
        {
            if (set.VariantSetAsset == null) return false;
            var variants = _variantsBySetId.TryGetValue(set.VariantSetAsset.UniqueIdString, out var cached) ? cached : set.VariantBase;
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

            var targetAsset = variants[target]?.variantAsset;
            if (targetAsset == null) return false;

            VariantSetBase.VariantTriggered?.Invoke(set.VariantSetAsset, targetAsset, false);
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
