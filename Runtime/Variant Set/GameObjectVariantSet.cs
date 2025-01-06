using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
{
    [Serializable]
    public class GameObjectVariant : VariantBase
    {
        public GameObject VariantGameObject;
    }

    public class GameObjectVariantSet : VariantSetBase
    {
        public List<GameObjectVariant> Variants => variants;

        [SerializeField]
        protected List<GameObjectVariant> variants = new ();

        public override int CurrentSelectionIndex => Variants.All(x => x.VariantGameObject != null)? Variants.FindIndex(x => x.VariantGameObject.activeSelf) : -1;
        
        public override string CurrentSelectionGuid => Variants[CurrentSelectionIndex].variantAsset.UniqueIdString;
    
        public override int CurrentSelectionCost => Variants[CurrentSelectionIndex].variantAsset.additionalCost;

        public override List<VariantBase> VariantBase => Variants.Cast<VariantBase>().ToList();

        protected override void OnVariantChanged(VariantBase variantBase, bool triggerConditionalVariants)
        {
            if (variantBase is not GameObjectVariant featureDetails) return;
            if(Variants.Contains(featureDetails))
            {
                Variants.ForEach(x => x.VariantGameObject.SetActive(featureDetails.VariantGameObject == x.VariantGameObject));
            }
            base.OnVariantChanged(variantBase, triggerConditionalVariants);
        }
    
        public override void SetVariant(int value, bool triggerConditionalVariants)
        {
            if(value < 0 || value >= Variants.Count) return;
            Variants.ForEach(x => x.VariantGameObject.SetActive(false));
            variants[value].VariantGameObject.SetActive(true);
            base.SetVariant(value, triggerConditionalVariants);
        }

        public override void AddVariant(VariantAsset variantAsset)
        {
            var newVariant = new GameObjectVariant
            {
                variantAsset = variantAsset,
                VariantGameObject = null
            };
            variants.Add(newVariant);
        }

        /// <summary>
        /// Add a new variant with the given name and object
        /// </summary>
        /// <param name="variantName">Variant Name</param>
        /// <param name="variantObject">Assign GameObject</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override void AddVariant<T>(VariantAsset variantAsset, T variantObject)
        {
            if (variantObject == null)
            {
                throw new ArgumentException("variantObject cannot be null");
            }
            
            if (!(variantObject is GameObject))
            {
                throw new ArgumentException("variantObject must be a GameObject");
            }

            AddVariant(variantAsset);
            AssignVariantObject(variantAsset.UniqueIdString, variantObject as GameObject);
        }
        
        public override void AssignVariantObject<T>(string id, T targetGameObject)
        {
            if (targetGameObject == null)
            {
                throw new ArgumentException("targetGameObject cannot be null");
            }
            
            if (!(targetGameObject is GameObject))
            {
                throw new ArgumentException("targetGameObject must be a GameObject");
            }
            
            var targetVariant = VariantBase.Find(x => string.Equals(x.variantAsset.UniqueIdString, id));
            if (targetVariant != null)
            {
                ((GameObjectVariant)targetVariant).VariantGameObject = targetGameObject as GameObject;
            }
        }
    }
}

