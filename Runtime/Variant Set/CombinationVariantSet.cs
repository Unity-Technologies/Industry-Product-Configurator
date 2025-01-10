using System;
using System.Collections;
using System.Collections.Generic;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using UnityEngine;
using System.Linq;
using UnityEngine.Serialization;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
{
    
    [Serializable]
    public class SerializableKeyValuePair
    {
        public string Key;
        public string Value;
    }
    
    [Serializable]
    public class SerializableDictionary
    {
        public List<SerializableKeyValuePair> KeyValuePairs = new List<SerializableKeyValuePair>();
    }
    
    [Serializable]
    public class CombinationVariant : VariantBase
    {
        public SerializableDictionary CombinationList = new();
    }
    
    public class CombinationVariantSet : VariantSetBase
    {
        [SerializeField, FormerlySerializedAs("variantSets")]
        public List<VariantSetBase> VariantSets = new();
    
        public List<CombinationVariant> Variants => variants;
    
        [SerializeField]
        protected List<CombinationVariant> variants = new();
        
        public override List<VariantBase> VariantBase => Variants.Cast<VariantBase>().ToList();

        public override int CurrentSelectionIndex
        {
            get
            {
                if(VariantSets == null || VariantSets.Count == 0 || VariantSets.All(x => x == null)) return -1;
                
                if(VariantSets.All(item => item.CurrentSelectionIndex == VariantSets[0].CurrentSelectionIndex))
                {
                    return VariantSets[0].CurrentSelectionIndex;
                }
                else
                {
                    return -1;
                }
            }
        }

        public override string CurrentSelectionGuid => CurrentSelectionIndex == -1 ? string.Empty : Variants[CurrentSelectionIndex].variantAsset.UniqueIdString;
        
        public override int CurrentSelectionCost => CurrentSelectionIndex == -1 ? 0 : Variants[CurrentSelectionIndex].variantAsset.additionalCost;

        protected override void OnVariantChanged(VariantBase variantBase, bool triggerConditionalVariants)
        {
            if(!Variants.Contains(variantBase)) return;
            foreach (var variantSet in VariantSets)
            {
                var combinationVariant = (variantBase as CombinationVariant);
                if (!combinationVariant.CombinationList.KeyValuePairs.Any(x =>
                        string.Equals(x.Key, variantSet.VariantSetAsset.UniqueIdString)))
                {
                    Debug.Log("Variant Set not found in Combination List");
                    continue;
                }
                {
                    var variantID = combinationVariant.CombinationList.KeyValuePairs.Find(x =>
                        string.Equals(x.Key, variantSet.VariantSetAsset.UniqueIdString)).Value;
                    var index = variantSet.VariantBase.FindIndex(x => string.Equals(x.variantAsset.UniqueIdString, variantID));
                    variantSet.SetVariant(index, triggerConditionalVariants);
                }
            }
            base.OnVariantChanged(variantBase, triggerConditionalVariants);
        }

        public override void SetVariant(int value, bool triggerConditionalVariants)
        {
            if(value < 0 || value >= Variants.Count) return;
            foreach (var variantSet in VariantSets)
            {
                var combinationVariant = (VariantBase[value] as CombinationVariant);
                if (!combinationVariant.CombinationList.KeyValuePairs.Any(x =>
                        string.Equals(x.Key, variantSet.VariantSetAsset.UniqueIdString)))
                {
                    Debug.Log("Variant Set not found in Combination List");
                    continue;
                }
                {
                    var variantID = combinationVariant.CombinationList.KeyValuePairs.Find(x =>
                        string.Equals(x.Key, variantSet.VariantSetAsset.UniqueIdString)).Value;
                    var index = variantSet.VariantBase.FindIndex(x => string.Equals(x.variantAsset.UniqueIdString, variantID));
                    variantSet.SetVariant(index, triggerConditionalVariants);
                }
            }
            base.SetVariant(value, triggerConditionalVariants);
        }

        public override void AddVariant(VariantAsset variantAsset)
        {
            var newVariant = new CombinationVariant
            {
                variantAsset = variantAsset
            };
            variants.Add(newVariant);
        }

        public override void AddVariant<T>(VariantAsset variantAsset, T variantObject)
        {
            AddVariant(variantAsset);
        }
        
        public override void AssignVariantObject<T>(string variantGuid, T variantObject) {}
    }
}