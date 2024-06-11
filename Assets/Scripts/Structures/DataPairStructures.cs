using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Structures {
    [Serializable]
    public struct DataPairStructures
    {
        public Rig rig;
        public bool foldout;
    }
    [Serializable]
    public struct StringTransformPair
    {
        public string name;
        public Transform transform;
    }
    [Serializable]
    public struct StringBoneDataPair {
        public string name;
        public BoneData bone;
    }
    [Serializable]
    public struct BoneDataConstrainsList {
        public BoneData boneData;
        public List<GameObject> constrainsList;
    }
}