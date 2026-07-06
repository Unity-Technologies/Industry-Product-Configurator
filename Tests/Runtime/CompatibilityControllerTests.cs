using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using IndustryCSE.Tool.ProductConfigurator.Runtime;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Tests
{
    /// <summary>
    /// PlayMode test for <see cref="CompatibilityController"/>: when a newly-selected variant restricts
    /// a variant currently selected in another set, the controller auto-switches that other set to a
    /// valid variant, keeps the just-made choice, and reports the restriction via its query API.
    /// </summary>
    public class CompatibilityControllerTests
    {
        private readonly List<Object> _toCleanup = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _toCleanup)
                if (obj != null) Object.DestroyImmediate(obj);
            _toCleanup.Clear();
        }

        private VariantAsset NewVariantAsset(string variantName)
        {
            var asset = ScriptableObject.CreateInstance<VariantAsset>();
            asset.NewID();
            asset.SetName(variantName);
            _toCleanup.Add(asset);
            return asset;
        }

        private VariantSetAsset NewSetAsset(string setName)
        {
            var asset = ScriptableObject.CreateInstance<VariantSetAsset>();
            asset.NewID();
            asset.SetName(setName);
            _toCleanup.Add(asset);
            return asset;
        }

        private GameObjectVariantSet BuildSet(string setName, VariantSetAsset setAsset, VariantAsset[] variantAssets, out GameObject[] children)
        {
            var host = new GameObject(setName);
            _toCleanup.Add(host);
            var set = host.AddComponent<GameObjectVariantSet>();
            set.SetVariantSetAsset(setAsset);

            children = new GameObject[variantAssets.Length];
            for (int i = 0; i < variantAssets.Length; i++)
            {
                var child = new GameObject($"{setName}_v{i}");
                child.transform.SetParent(host.transform);
                children[i] = child;
                set.AddVariant(variantAssets[i]);
                set.Variants[i].VariantGameObject = child;
            }
            return set;
        }

        [UnityTest]
        public IEnumerator Selecting_a_variant_auto_switches_a_set_whose_selection_it_restricts()
        {
            var setAAsset = NewSetAsset("Set A");
            var setBAsset = NewSetAsset("Set B");
            var a0 = NewVariantAsset("A0");
            var a1 = NewVariantAsset("A1");
            var b0 = NewVariantAsset("B0");
            var b1 = NewVariantAsset("B1");

            var setA = BuildSet("SetA", setAAsset, new[] { a0, a1 }, out var aChildren);
            var setB = BuildSet("SetB", setBAsset, new[] { b0, b1 }, out var bChildren);

            // Clean starting state: both sets on variant 0.
            setA.SetVariant(0, false);
            setB.SetVariant(0, false);

            // Rule: A1 and B1 are mutually exclusive.
            var ruleSet = ScriptableObject.CreateInstance<CompatibilityRuleSet>();
            _toCleanup.Add(ruleSet);
            ruleSet.AddRule(new CompatibilityRule
            {
                whenSelected = a1,
                ruleType = CompatibilityRuleType.Restricts,
                targets = new List<VariantAsset> { b1 },
                mutual = true
            });

            // Controller is created AFTER the sets so its OnEnable discovers them.
            var controllerGo = new GameObject("Controller");
            _toCleanup.Add(controllerGo);
            var controller = controllerGo.AddComponent<CompatibilityController>();
            controller.RuleSet = ruleSet;

            yield return null;

            // Select B1 — valid so far (A1 not selected).
            VariantSetBase.VariantTriggered?.Invoke(setBAsset, b1, true);
            Assert.IsTrue(bChildren[1].activeSelf, "Set B should be on B1.");

            // Select A1 — conflicts with B1; the controller must auto-switch Set B off B1.
            VariantSetBase.VariantTriggered?.Invoke(setAAsset, a1, true);

            Assert.IsTrue(aChildren[1].activeSelf && !aChildren[0].activeSelf, "Set A should keep the just-selected A1.");
            Assert.IsTrue(bChildren[0].activeSelf && !bChildren[1].activeSelf, "Set B should have auto-switched off the restricted B1.");
            Assert.IsTrue(controller.IsRestricted(b1), "B1 should stay restricted while A1 is selected.");
            Assert.IsFalse(controller.IsAvailable(b1));
            Assert.IsTrue(controller.IsConfigurationValid, "Configuration should be valid after the auto-switch.");
        }

        [UnityTest]
        public IEnumerator Non_mutual_restriction_only_acts_in_the_forward_direction()
        {
            var setAAsset = NewSetAsset("Set A");
            var setBAsset = NewSetAsset("Set B");
            var a0 = NewVariantAsset("A0");
            var a1 = NewVariantAsset("A1");
            var b0 = NewVariantAsset("B0");
            var b1 = NewVariantAsset("B1");

            var setA = BuildSet("SetA", setAAsset, new[] { a0, a1 }, out var aChildren);
            var setB = BuildSet("SetB", setBAsset, new[] { b0, b1 }, out var bChildren);
            setA.SetVariant(0, false);
            setB.SetVariant(0, false);

            // a1 restricts b1, but NOT the other way round.
            var ruleSet = ScriptableObject.CreateInstance<CompatibilityRuleSet>();
            _toCleanup.Add(ruleSet);
            ruleSet.AddRule(new CompatibilityRule
            {
                whenSelected = a1,
                ruleType = CompatibilityRuleType.Restricts,
                targets = new List<VariantAsset> { b1 },
                mutual = false
            });

            var controllerGo = new GameObject("Controller");
            _toCleanup.Add(controllerGo);
            var controller = controllerGo.AddComponent<CompatibilityController>();
            controller.RuleSet = ruleSet;
            yield return null;

            // Selecting the TARGET (b1) while the trigger is not selected must NOT restrict the trigger (non-mutual).
            VariantSetBase.VariantTriggered?.Invoke(setBAsset, b1, true);
            Assert.IsTrue(bChildren[1].activeSelf, "Set B should be on b1.");
            Assert.IsFalse(controller.IsRestricted(a1), "Non-mutual rule must not restrict the trigger when the target is selected.");

            // Selecting the TRIGGER (a1) DOES restrict b1 -> Set B must auto-switch off b1.
            VariantSetBase.VariantTriggered?.Invoke(setAAsset, a1, true);
            Assert.IsTrue(aChildren[1].activeSelf, "Set A should keep the just-selected a1.");
            Assert.IsTrue(bChildren[0].activeSelf && !bChildren[1].activeSelf, "Set B should have auto-switched off the restricted b1.");
            Assert.IsTrue(controller.IsRestricted(b1), "b1 should be restricted while a1 is selected.");
        }

        [UnityTest]
        public IEnumerator Requirement_is_report_only_and_never_auto_switches()
        {
            var setAAsset = NewSetAsset("Set A");
            var setBAsset = NewSetAsset("Set B");
            var a0 = NewVariantAsset("A0");
            var a1 = NewVariantAsset("A1");
            var b0 = NewVariantAsset("B0");
            var b1 = NewVariantAsset("B1");

            var setA = BuildSet("SetA", setAAsset, new[] { a0, a1 }, out var aChildren);
            var setB = BuildSet("SetB", setBAsset, new[] { b0, b1 }, out var bChildren);
            setA.SetVariant(0, false);
            setB.SetVariant(0, false);

            // a1 requires b1.
            var ruleSet = ScriptableObject.CreateInstance<CompatibilityRuleSet>();
            _toCleanup.Add(ruleSet);
            ruleSet.AddRule(new CompatibilityRule
            {
                whenSelected = a1,
                ruleType = CompatibilityRuleType.Requires,
                targets = new List<VariantAsset> { b1 },
                mutual = false
            });

            var controllerGo = new GameObject("Controller");
            _toCleanup.Add(controllerGo);
            var controller = controllerGo.AddComponent<CompatibilityController>();
            controller.RuleSet = ruleSet;
            yield return null;

            // Select a1: b1 becomes required, but requirements are report-only -> Set B must NOT auto-switch to b1.
            VariantSetBase.VariantTriggered?.Invoke(setAAsset, a1, true);
            Assert.IsTrue(bChildren[0].activeSelf && !bChildren[1].activeSelf, "A requirement must NOT auto-switch the other set (report-only).");
            Assert.IsTrue(controller.IsRequired(b1), "b1 should be reported as required.");
            Assert.IsFalse(controller.IsConfigurationValid, "An unmet requirement must make the configuration invalid.");
            Assert.AreEqual(1, controller.UnmetRequirements.Count, "The unmet requirement should be reported.");

            // Satisfying it manually makes the configuration valid.
            VariantSetBase.VariantTriggered?.Invoke(setBAsset, b1, true);
            Assert.IsTrue(controller.IsConfigurationValid, "Configuration should be valid once the requirement is satisfied.");
        }
    }
}
