using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    //[InitializeOnLoad]
    public static class SceneTracker
    {
        /*
        private static void CheckForVariantSet()
        {
            var variantSets = Object.FindObjectsByType<VariantSetBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var variantSet in variantSets)
            {
                if(variantSet.VariantSetAsset == null) continue;
                if(variantSet.VariantBase == null || variantSet.VariantBase.Count == 0) continue;
                foreach (var variantBase in variantSet.VariantBase)
                {
                    if(variantBase.conditionalVariants == null || variantBase.conditionalVariants.Count == 0) continue;
                    List<ConditionalVariantData> toRemove = new List<ConditionalVariantData>();
                    foreach (var baseConditionalVariant in variantBase.conditionalVariants)
                    {
                        if(variantSets.Any(x => x.VariantSetAsset == baseConditionalVariant.variantSetAsset)) continue;
                        toRemove.Add(baseConditionalVariant);
                    }
                    foreach (var conditionalVariantData in toRemove)
                    {
                        Undo.RecordObject(variantSet, "Remove Conditional Variant Data");
                        variantBase.conditionalVariants.Remove(conditionalVariantData);
                    }
                }
            }
        }*/
        
        public static void CheckVariantSet(VariantSetBase variantSet)
        {
            var variantSets = Object.FindObjectsByType<VariantSetBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where
                (x => x != variantSet);
            foreach (var variantSetBase in variantSets)
            {
                if(variantSet.VariantSetAsset == null) continue;
                if(variantSetBase.VariantBase == null || variantSetBase.VariantBase.Count == 0) continue;
                foreach (var variantBase in variantSetBase.VariantBase)
                {
                    if(variantBase.conditionalVariants == null || variantBase.conditionalVariants.Count == 0) continue;
                    List<ConditionalVariantData> toRemove = new List<ConditionalVariantData>();
                    foreach (var baseConditionalVariant in variantBase.conditionalVariants)
                    {
                        if(baseConditionalVariant.variantSetAsset != variantSet.VariantSetAsset) continue;
                        if(variantSet.VariantBase.Any(x => x.variantAsset == baseConditionalVariant.variantAsset)) continue;
                        toRemove.Add(baseConditionalVariant);
                    }
                    foreach (var conditionalVariantData in toRemove)
                    {
                        Undo.RecordObject(variantSetBase, "Remove Conditional Variant Data");
                        variantBase.conditionalVariants.Remove(conditionalVariantData);
                    }
                }
            }
        }
    }
}
