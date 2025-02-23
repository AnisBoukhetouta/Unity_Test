using System;
using UnityEngine;

namespace LocalRigs
{
    public class PositionConstrained : LocalRig
    {
        [Range(0, 1)] public float weight;
        public SetPositionType setPositionType;
        public Vector3 axesWeight = Vector3.one;


        public enum GetPositionType
        {
            Local,
            World,
        }
        public enum SetPositionType
        {
            Copy,
            Add,
            Subtract,
        }

        [Serializable]
        public struct PositionConstrinedObject
        {
            public Transform transformObject;
            public GetPositionType positionType;
            public Vector3 originalPosition;
            public Vector3 lastPostition;
        }

        [Space(20f)]
        public PositionConstrinedObject fromTransform;
        public PositionConstrinedObject toTransform;

        private void Start()
        {
            fromTransform.originalPosition = fromTransform.positionType == GetPositionType.Local ? fromTransform.transformObject.localPosition : fromTransform.transformObject.position;
            fromTransform.lastPostition = fromTransform.originalPosition;

            toTransform.originalPosition = toTransform.positionType == GetPositionType.Local ? toTransform.transformObject.localPosition : toTransform.transformObject.position;
            toTransform.lastPostition = toTransform.originalPosition;
        }

        public override void Execute()
        {
            if (weight == 0) return;
            
            var fromPositionValue = fromTransform.positionType == GetPositionType.Local ? fromTransform.transformObject.localPosition : fromTransform.transformObject.position;
            var toPositionValue = fromTransform.lastPostition;

            Vector3 finalPositionValue = Vector3.zero;

            switch (setPositionType)
            {
                case SetPositionType.Copy:
                    finalPositionValue = fromPositionValue;
                    break;
                case SetPositionType.Add:
                    finalPositionValue = toPositionValue + fromPositionValue;
                    break;
                case SetPositionType.Subtract:
                    finalPositionValue = toPositionValue - fromPositionValue;
                    break;
                default:
                    finalPositionValue = Vector3.zero;
                    break;
            }

            var xPosition = Mathf.Lerp(toPositionValue.x, finalPositionValue.x, axesWeight.x);

            var yPosition = Mathf.Lerp(toPositionValue.y, finalPositionValue.y, axesWeight.y);

            var zPosition = Mathf.Lerp(toPositionValue.z, finalPositionValue.z, axesWeight.z);

            finalPositionValue = Vector3.Lerp(toPositionValue, new Vector3(xPosition, yPosition, zPosition), weight);

            fromTransform.lastPostition = finalPositionValue;

            if (toTransform.positionType == GetPositionType.Local) toTransform.transformObject.localPosition = finalPositionValue;
            else toTransform.transformObject.position = finalPositionValue;
        }
    }
}
