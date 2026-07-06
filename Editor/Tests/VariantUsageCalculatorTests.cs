using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using IndustryCSE.Tool.ProductConfigurator.Editor;

namespace IndustryCSE.Tool.ProductConfigurator.Editor.Tests
{
    /// <summary>
    /// Tests for <see cref="VariantUsageCalculator"/> — the correctness core behind the cleanup
    /// tools. These encode WHY deletion must be safe (Rule 9): the calculator must never report an
    /// asset as unused while any configuration still references it, including references that live
    /// only in the string-GUID combination maps that AssetDatabase.GetDependencies cannot see.
    /// The calculator is pure, so every case is built from in-memory data — no scenes or assets.
    /// </summary>
    public class VariantUsageCalculatorTests
    {
        private const string SetA = "set-a";
        private const string SetB = "set-b";
        private const string ComboSet = "set-combo";
        private const string VarA1 = "var-a1";
        private const string VarA2 = "var-a2";
        private const string VarB1 = "var-b1";
        private const string ComboOption = "var-combo1";

        private static SetInput Set(string id) => new() { SetId = id };

        private static UsageResult Compute(
            IReadOnlyList<SetInput> sets,
            IEnumerable<string> allSetIds,
            IEnumerable<string> allVariantIds,
            IReadOnlyList<SourceUsage> sources = null,
            IReadOnlyDictionary<string, string> icons = null)
        {
            return VariantUsageCalculator.Compute(
                sets,
                allSetIds.ToList(),
                allVariantIds.ToList(),
                sources ?? new List<SourceUsage>(),
                icons ?? new Dictionary<string, string>());
        }

        [Test]
        public void Variant_that_is_a_member_of_a_live_set_is_used()
        {
            // A variant belonging to a set is referenced by that set's component and must survive cleanup.
            var setA = Set(SetA);
            setA.MemberVariantIds.Add(VarA1);

            var result = Compute(new[] { setA }, new[] { SetA }, new[] { VarA1 });

            Assert.IsTrue(result.UsedVariantIds.Contains(VarA1));
            Assert.IsFalse(result.UnusedVariantIds.Contains(VarA1));
        }

        [Test]
        public void Variant_referenced_only_by_a_combination_map_is_used()
        {
            // THE BUG GUARD. VarB1 is not a member of any set here and is referenced solely through
            // the combination map (a plain string GUID). AssetDatabase.GetDependencies is blind to
            // this, so the legacy cleaner would delete VarB1 and silently break the combination.
            // The calculator must count the string reference as usage.
            var combo = Set(ComboSet);
            combo.MemberVariantIds.Add(ComboOption);
            combo.Combos.Add(new RelationRef
            {
                OwnerVariantId = ComboOption,
                TargetSetId = SetB,
                TargetVariantId = VarB1
            });

            var result = Compute(
                new[] { combo },
                new[] { ComboSet, SetB },
                new[] { ComboOption, VarB1 });

            Assert.IsTrue(result.UsedVariantIds.Contains(VarB1), "Combination-referenced variant must be treated as used.");
            Assert.IsFalse(result.UnusedVariantIds.Contains(VarB1));
        }

        [Test]
        public void Variant_referenced_by_nothing_is_unused()
        {
            // A loose VariantAsset in no set, no conditional, no combination is genuinely deletable.
            var setA = Set(SetA);
            setA.MemberVariantIds.Add(VarA1);

            var result = Compute(
                new[] { setA },
                new[] { SetA },
                new[] { VarA1, VarA2 }); // VarA2 exists in project but is referenced nowhere

            Assert.IsTrue(result.UnusedVariantIds.Contains(VarA2));
            Assert.IsFalse(result.UsedVariantIds.Contains(VarA2));
        }

        [Test]
        public void Conditional_target_variant_is_used()
        {
            // Selecting VarA1 triggers VarB1 via a conditional. VarB1 must count as used.
            var setA = Set(SetA);
            setA.MemberVariantIds.Add(VarA1);
            setA.Conditionals.Add(new RelationRef
            {
                OwnerVariantId = VarA1,
                TargetSetId = SetB,
                TargetVariantId = VarB1
            });

            var result = Compute(
                new[] { setA },
                new[] { SetA, SetB },
                new[] { VarA1, VarB1 });

            Assert.IsTrue(result.UsedVariantIds.Contains(VarB1));
            Assert.IsTrue(result.UsedSetIds.Contains(SetB));
        }

        [Test]
        public void Combination_pointing_to_a_missing_variant_is_reported_dangling()
        {
            // The combo Value GUID no longer resolves to any VariantAsset in the project.
            // It must be surfaced as dangling so the user can fix it — never silently ignored.
            var combo = Set(ComboSet);
            combo.MemberVariantIds.Add(ComboOption);
            combo.Combos.Add(new RelationRef
            {
                OwnerVariantId = ComboOption,
                TargetSetId = SetB,
                TargetVariantId = "deleted-variant"
            });

            var result = Compute(
                new[] { combo },
                new[] { ComboSet, SetB },
                new[] { ComboOption }); // "deleted-variant" is absent

            Assert.AreEqual(1, result.DanglingCombos.Count);
            Assert.IsTrue(result.DanglingCombos[0].VariantMissing);
            Assert.IsFalse(result.DanglingCombos[0].SetMissing);
            Assert.AreEqual("deleted-variant", result.DanglingCombos[0].TargetVariantId);
        }

        [Test]
        public void Combination_pointing_to_a_missing_set_is_reported_dangling()
        {
            var combo = Set(ComboSet);
            combo.MemberVariantIds.Add(ComboOption);
            combo.Combos.Add(new RelationRef
            {
                OwnerVariantId = ComboOption,
                TargetSetId = "deleted-set",
                TargetVariantId = VarB1
            });

            var result = Compute(
                new[] { combo },
                new[] { ComboSet },        // "deleted-set" is absent
                new[] { ComboOption, VarB1 });

            Assert.AreEqual(1, result.DanglingCombos.Count);
            Assert.IsTrue(result.DanglingCombos[0].SetMissing);
        }

        [Test]
        public void Orphan_icon_is_detected_when_its_variant_is_absent()
        {
            // Icons are named <variantId>.png. A PNG whose variant was deleted is an orphan file.
            var icons = new Dictionary<string, string>
            {
                { "Assets/Product Configurator/Icons/deleted-variant.png", "deleted-variant" }
            };

            var result = Compute(
                new List<SetInput>(),
                new[] { SetA },
                new[] { VarA1 },
                icons: icons);

            Assert.Contains("Assets/Product Configurator/Icons/deleted-variant.png", result.OrphanIconPaths);
        }

        [Test]
        public void Icon_is_not_orphan_when_its_variant_exists()
        {
            var icons = new Dictionary<string, string>
            {
                { "Assets/Product Configurator/Icons/var-a1.png", VarA1 }
            };

            var result = Compute(
                new List<SetInput>(),
                new[] { SetA },
                new[] { VarA1 },
                icons: icons);

            CollectionAssert.IsEmpty(result.OrphanIconPaths);
        }

        [Test]
        public void Object_reference_usage_from_a_source_marks_assets_used()
        {
            // Safety net for closed scenes scanned shallowly: an object-ref dependency alone must
            // keep an asset out of the unused list, even without any SetInput structural data.
            var source = new SourceUsage();
            source.ReferencedSetIds.Add(SetA);
            source.ReferencedVariantIds.Add(VarA1);

            var result = Compute(
                new List<SetInput>(),
                new[] { SetA },
                new[] { VarA1 },
                sources: new[] { source });

            Assert.IsTrue(result.UsedSetIds.Contains(SetA));
            Assert.IsTrue(result.UsedVariantIds.Contains(VarA1));
            CollectionAssert.IsEmpty(result.UnusedVariantIds);
        }

        [Test]
        public void Empty_ids_do_not_produce_usage()
        {
            // A component with an unassigned variantSetAsset / null variantAsset yields empty ids.
            // These must be ignored rather than counting as phantom usage.
            var setA = Set(SetA);
            setA.MemberVariantIds.Add(string.Empty);
            setA.Conditionals.Add(new RelationRef { OwnerVariantId = string.Empty, TargetSetId = string.Empty, TargetVariantId = string.Empty });

            var result = Compute(new[] { setA }, new[] { SetA }, new[] { VarA1 });

            Assert.IsFalse(result.UsedVariantIds.Contains(string.Empty));
            Assert.IsFalse(result.UsedSetIds.Contains(string.Empty));
        }
    }
}
