using System;
using UnityEngine;

namespace Structures {
    [Serializable]
    public struct KeyFrameData {
        public BoneData boneData;
        public float time;
        public Vector3 position;
        public Quaternion quaternion;
    }
}