using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
{
    [Serializable]
    public class TransformVariant : VariantBase
    {
        public Transform VariantTransform;
        public bool InstantChange = true;
        public float LerpTime = 1f;
    }

    public class TransformVariantSet : VariantSetBase
    {
        public List<TransformVariant> Variants => variants;
        public GameObject gameObjectToMove;

        [SerializeField]
        protected List<TransformVariant> variants = new ();
        
        public override int CurrentSelectionIndex => gameObjectToMove != null && Variants.All(x => x.VariantTransform != null) ? Variants.FindIndex(x => x.VariantTransform.position==gameObjectToMove.transform.position && x.VariantTransform.rotation == gameObjectToMove.transform.rotation) : -1;

        public override string CurrentSelectionGuid => Variants[CurrentSelectionIndex].variantAsset.UniqueIdString;
    
        public override int CurrentSelectionCost => Variants[CurrentSelectionIndex].variantAsset.additionalCost;

        public override List<VariantBase> VariantBase => Variants.Cast<VariantBase>().ToList();
        
        Coroutine lerpCoroutine;

        protected override void OnVariantChanged(VariantBase variantBase, bool triggerConditionalVariants)
        {
            if (variantBase is not TransformVariant featureDetails) return;
            
            TransformVariant(featureDetails);
            base.OnVariantChanged(variantBase, triggerConditionalVariants);
        }
    
        public override void SetVariant(int value, bool triggerConditionalVariants)
        {
            if(value < 0 || value >= Variants.Count) return;

            TransformVariant variant = Variants[value];

            TransformVariant(variant);
            
            base.SetVariant(value, triggerConditionalVariants);
        }

        private void TransformVariant(TransformVariant targetTransform)
        {
            if (!Application.isPlaying || targetTransform.InstantChange)
            {
                gameObjectToMove.transform.SetPositionAndRotation(targetTransform.VariantTransform.position, targetTransform.VariantTransform.rotation);
                gameObjectToMove.transform.localScale = targetTransform.VariantTransform.localScale;
            }
            else
            {
                if(gameObjectToMove.transform.position != targetTransform.VariantTransform.position || gameObjectToMove.transform.rotation != targetTransform.VariantTransform.rotation || gameObjectToMove.transform.localScale != targetTransform.VariantTransform.localScale)
                {
                    if (lerpCoroutine != null)
                    {
                        StopCoroutine(lerpCoroutine);
                    }
                    lerpCoroutine = StartCoroutine(LerpTransform(targetTransform.VariantTransform.position, targetTransform.VariantTransform.rotation, targetTransform.VariantTransform.localScale, targetTransform.LerpTime));
                }
            }
        }

        private IEnumerator LerpTransform(Vector3 variantTransformPosition, Quaternion variantTransformRotation, Vector3 variantTransformLocalScale, float lerpTime)
        {
            float timeElapsed = 0;
            Vector3 startPosition = gameObjectToMove.transform.position;
            Quaternion startRotation = gameObjectToMove.transform.rotation;
            Vector3 startScale = gameObjectToMove.transform.localScale;
            while (timeElapsed < lerpTime)
            {
                gameObjectToMove.transform.position = Vector3.Lerp(startPosition, variantTransformPosition, timeElapsed / lerpTime);
                gameObjectToMove.transform.rotation = Quaternion.Lerp(startRotation, variantTransformRotation, timeElapsed / lerpTime);
                gameObjectToMove.transform.localScale = Vector3.Lerp(startScale, variantTransformLocalScale, timeElapsed / lerpTime);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            gameObjectToMove.transform.position = variantTransformPosition;
            gameObjectToMove.transform.rotation = variantTransformRotation;
            gameObjectToMove.transform.localScale = variantTransformLocalScale;
        }

        public override void AddVariant(VariantAsset variantAsset)
        {
            var newVariant = new TransformVariant
            {
                variantAsset = variantAsset,
                VariantTransform = null
            };
            variants.Add(newVariant);
        }

        /// <summary>
        /// Add a new variant with a Transform object
        /// </summary>
        /// <param name="variantName">Variant Name</param>
        /// <param name="variantObject">Assign Transform</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override void AddVariant<T>(VariantAsset variantAsset, T variantObject)
        {
            if (variantObject == null)
            {
                throw new ArgumentException("variantObject cannot be null");
            }
            
            if (!(variantObject is Transform))
            {
                throw new ArgumentException("variantObject must be a Transform");
            }
            
            AddVariant(variantAsset);
            AssignVariantObject(variantAsset.UniqueIdString, variantObject as Transform);
        }
        
        public override void AssignVariantObject<T>(string id, T targetTransform)
        {
            if (targetTransform == null)
            {
                throw new ArgumentException("targetTransform cannot be null");
            }
            
            if (!(targetTransform is Transform))
            {
                throw new ArgumentException("targetTransform must be a Transform");
            }
            
            var targetVariant = VariantBase.Find(x => string.Equals(x.variantAsset.UniqueIdString, id));
            if (targetVariant != null)
            {
                ((TransformVariant)targetVariant).VariantTransform = targetTransform as Transform;
            }
        }
    }
}

