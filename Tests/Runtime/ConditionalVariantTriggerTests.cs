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
    /// PlayMode regression test for the conditional-variant trigger flow: selecting a variant that
    /// declares a <see cref="ConditionalVariantData"/> must automatically switch the target variant
    /// set to the target variant. This locks in the behaviour driven by
    /// <see cref="VariantSetBase.VariantTriggered"/> — the same event flow the incompatibility-rules
    /// feature will build on, so a regression here would silently break configurations.
    ///
    /// Uses GameObjectVariantSet because its applied result (which child GameObject is active) is
    /// directly observable.
    /// </summary>
    public class ConditionalVariantTriggerTests
    {
        private readonly List<Object> _toCleanup = new();

        [TearDown]
        public void TearDown()
        {
            // DestroyImmediate so component OnDisable runs now and unsubscribes from the static event.
            foreach (var obj in _toCleanup)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
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

        // Builds a GameObjectVariantSet backed by one child GameObject per variant.
        private GameObjectVariantSet BuildSet(string setName, VariantSetAsset setAsset, VariantAsset[] variantAssets, out GameObject[] children)
        {
            var host = new GameObject(setName);
            _toCleanup.Add(host);
            var set = host.AddComponent<GameObjectVariantSet>();
            set.SetVariantSetAsset(setAsset);

            children = new GameObject[variantAssets.Length];
            for (int i = 0; i < variantAssets.Length; i++)
            {
                var child = new GameObject($"{setName}_variant{i}");
                child.transform.SetParent(host.transform);
                children[i] = child;

                set.AddVariant(variantAssets[i]);
                set.Variants[i].VariantGameObject = child;
            }
            return set;
        }

        [UnityTest]
        public IEnumerator Selecting_a_variant_triggers_its_conditional_variant_in_another_set()
        {
            var setAAsset = NewSetAsset("Set A");
            var setBAsset = NewSetAsset("Set B");
            var a0 = NewVariantAsset("A0");
            var a1 = NewVariantAsset("A1");
            var b0 = NewVariantAsset("B0");
            var b1 = NewVariantAsset("B1");

            var setA = BuildSet("SetA", setAAsset, new[] { a0, a1 }, out var aChildren);
            var setB = BuildSet("SetB", setBAsset, new[] { b0, b1 }, out var bChildren);

            // Selecting A1 must trigger Set B -> B1.
            setA.Variants[1].conditionalVariants.Add(new ConditionalVariantData
            {
                variantSetAsset = setBAsset,
                variantAsset = b1
            });

            // Let OnEnable subscribe both sets to the static VariantTriggered event.
            yield return null;

            // Known starting state: both sets on variant 0.
            setA.SetVariant(0, false);
            setB.SetVariant(0, false);
            Assert.IsTrue(bChildren[0].activeSelf && !bChildren[1].activeSelf, "Set B should start on B0.");

            // Act: select A1 with conditional triggering enabled (mirrors VariantSelect.SelectVariant).
            VariantSetBase.VariantTriggered?.Invoke(setAAsset, a1, true);

            // Assert: Set A switched to A1, and the conditional auto-switched Set B to B1.
            Assert.IsTrue(aChildren[1].activeSelf && !aChildren[0].activeSelf, "Set A should be on A1.");
            Assert.IsTrue(bChildren[1].activeSelf && !bChildren[0].activeSelf, "Set B should have been triggered to B1.");
        }

        [UnityTest]
        public IEnumerator Not_requesting_conditional_triggering_leaves_the_other_set_untouched()
        {
            var setAAsset = NewSetAsset("Set A");
            var setBAsset = NewSetAsset("Set B");
            var a0 = NewVariantAsset("A0");
            var a1 = NewVariantAsset("A1");
            var b0 = NewVariantAsset("B0");
            var b1 = NewVariantAsset("B1");

            var setA = BuildSet("SetA", setAAsset, new[] { a0, a1 }, out _);
            var setB = BuildSet("SetB", setBAsset, new[] { b0, b1 }, out var bChildren);

            // A1 declares a conditional, but this time we fire without requesting triggering.
            setA.Variants[1].conditionalVariants.Add(new ConditionalVariantData
            {
                variantSetAsset = setBAsset,
                variantAsset = b1
            });

            yield return null;
            setB.SetVariant(0, false);

            // Act: trigger A1 but with conditional triggering disabled.
            VariantSetBase.VariantTriggered?.Invoke(setAAsset, a1, false);

            // Assert: Set B stays on B0 because triggering was not requested.
            Assert.IsTrue(bChildren[0].activeSelf && !bChildren[1].activeSelf,
                "Set B should remain on B0 when conditional triggering is not requested.");
        }
    }
}
