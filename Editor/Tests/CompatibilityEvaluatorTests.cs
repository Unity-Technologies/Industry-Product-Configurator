using System.Collections.Generic;
using NUnit.Framework;
using IndustryCSE.Tool.ProductConfigurator.Runtime;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Editor.Tests
{
    /// <summary>
    /// Tests for <see cref="CompatibilityEvaluator"/> — the pure core of the compatibility-rules
    /// feature. These encode the rule semantics: a restriction blocks its targets while active
    /// (mutually by default), a requirement flags its targets and reports unmet ones, and a
    /// currently-selected variant that becomes restricted is reported as an invalid configuration.
    /// </summary>
    public class CompatibilityEvaluatorTests
    {
        private const string SetA = "set-a";
        private const string SetB = "set-b";
        private const string A1 = "a1";
        private const string B1 = "b1";
        private const string B2 = "b2";

        private static readonly Dictionary<string, string> VariantToSet = new()
        {
            { A1, SetA }, { B1, SetB }, { B2, SetB }
        };

        private static CompatibilityRuleInput Rule(string when, CompatibilityRuleType type, bool mutual, params string[] targets) =>
            new()
            {
                WhenVariantId = when,
                RuleType = type,
                TargetVariantIds = targets,
                Mutual = mutual
            };

        private static CompatibilityResult Evaluate(IReadOnlyList<CompatibilityRuleInput> rules, Dictionary<string, string> selection) =>
            CompatibilityEvaluator.Evaluate(rules, selection, VariantToSet);

        [Test]
        public void Restriction_restricts_its_target_while_the_trigger_is_selected()
        {
            var result = Evaluate(
                new[] { Rule(A1, CompatibilityRuleType.Restricts, false, B1) },
                new Dictionary<string, string> { { SetA, A1 } });

            Assert.IsTrue(result.RestrictedVariantIds.Contains(B1), "B1 should be restricted while A1 is selected.");
            Assert.IsFalse(result.RestrictedVariantIds.Contains(A1), "Non-mutual rule must not restrict the trigger.");
        }

        [Test]
        public void Restriction_is_inactive_when_the_trigger_is_not_selected()
        {
            var result = Evaluate(
                new[] { Rule(A1, CompatibilityRuleType.Restricts, false, B1) },
                new Dictionary<string, string> { { SetB, B2 } });

            CollectionAssert.IsEmpty(result.RestrictedVariantIds);
        }

        [Test]
        public void Mutual_restriction_restricts_the_trigger_while_a_target_is_selected()
        {
            // A1 <-> B1 mutually exclusive; only B1 is selected, so A1 must be restricted too.
            var result = Evaluate(
                new[] { Rule(A1, CompatibilityRuleType.Restricts, true, B1) },
                new Dictionary<string, string> { { SetB, B1 } });

            Assert.IsTrue(result.RestrictedVariantIds.Contains(A1), "Mutual rule should restrict the trigger when the target is selected.");
        }

        [Test]
        public void Non_mutual_restriction_does_not_restrict_the_trigger()
        {
            var result = Evaluate(
                new[] { Rule(A1, CompatibilityRuleType.Restricts, false, B1) },
                new Dictionary<string, string> { { SetB, B1 } });

            Assert.IsFalse(result.RestrictedVariantIds.Contains(A1));
        }

        [Test]
        public void Requirement_flags_target_and_reports_unmet_when_a_different_variant_is_selected()
        {
            var result = Evaluate(
                new[] { Rule(A1, CompatibilityRuleType.Requires, false, B1) },
                new Dictionary<string, string> { { SetA, A1 }, { SetB, B2 } });

            Assert.IsTrue(result.RequiredVariantIds.Contains(B1));
            Assert.AreEqual(1, result.UnmetRequirements.Count);
            Assert.AreEqual(SetB, result.UnmetRequirements[0].SetId);
            Assert.AreEqual(B1, result.UnmetRequirements[0].RequiredVariantId);
        }

        [Test]
        public void Requirement_is_met_when_the_required_variant_is_selected()
        {
            var result = Evaluate(
                new[] { Rule(A1, CompatibilityRuleType.Requires, false, B1) },
                new Dictionary<string, string> { { SetA, A1 }, { SetB, B1 } });

            Assert.IsTrue(result.RequiredVariantIds.Contains(B1));
            CollectionAssert.IsEmpty(result.UnmetRequirements);
        }

        [Test]
        public void A_currently_selected_variant_that_becomes_restricted_is_reported_invalid()
        {
            // A1 selected restricts B1, but B1 is also currently selected -> invalid configuration.
            var result = Evaluate(
                new[] { Rule(A1, CompatibilityRuleType.Restricts, false, B1) },
                new Dictionary<string, string> { { SetA, A1 }, { SetB, B1 } });

            Assert.IsTrue(result.InvalidSelectedVariantIds.Contains(B1), "Selected-but-restricted B1 must be invalid.");
            Assert.IsFalse(result.InvalidSelectedVariantIds.Contains(A1));
        }

        [Test]
        public void Irrelevant_rules_produce_an_empty_result()
        {
            var result = Evaluate(
                new[] { Rule("c1", CompatibilityRuleType.Restricts, true, "d1") },
                new Dictionary<string, string> { { SetA, A1 } });

            CollectionAssert.IsEmpty(result.RestrictedVariantIds);
            CollectionAssert.IsEmpty(result.RequiredVariantIds);
            CollectionAssert.IsEmpty(result.InvalidSelectedVariantIds);
            CollectionAssert.IsEmpty(result.UnmetRequirements);
        }
    }
}
