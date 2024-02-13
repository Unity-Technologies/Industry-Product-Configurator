using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IndustryCSE.Tool.ProductConfigurator
{
    [Serializable]
    public class TransformOptionDetail : OptionDetailBase
    {
        public Transform optionTransform;
    }

    public class TransformConfiguration : ConfigurationBase
    {
        public List<TransformOptionDetail> OptionDetails => optionDetails;
        public GameObject gameObjectToMove;

        [SerializeField]
        protected List<TransformOptionDetail> optionDetails = new ();

        public string CurrentSelectionGuid => OptionDetails.First(x => x.optionTransform.position==gameObjectToMove.transform.position).configurationOption.UniqueIdString;
    
        public int CurrentSelectionCost => OptionDetails.First(x => x.optionTransform.position==gameObjectToMove.transform.position).configurationOption.additionalCost;

        protected override void OnOptionChanged(OptionDetailBase optionDetailBase)
        {
            if (optionDetailBase is not TransformOptionDetail featureDetails) return;
            gameObjectToMove.transform.position = featureDetails.optionTransform.position;
            gameObjectToMove.transform.rotation = featureDetails.optionTransform.rotation;
        }
    
        public override void SetOption(int value)
        {
            gameObjectToMove.transform.position = optionDetails[value].optionTransform.position;
            gameObjectToMove.transform.rotation = optionDetails[value].optionTransform.rotation;

        }
    }
}

