using System;
using Enums;
using UnityEngine;

namespace Structures {
    [Serializable]
    public struct BoneData
    {
        public string boneName;
        public BodyPart bodyPart;
        public BodyPartSide bodyPartSide;
        public Transform boneRef;
        public int indexFromRoot;
        public float length;
    }
}