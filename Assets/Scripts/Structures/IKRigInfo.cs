using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Structures {
    public struct IKRigInfo {
        public Transform RigParent;
        public Dictionary<Rig, Transform[]> Rigs;
        public Dictionary<string, Transform> PathConstrains;
        public Dictionary<string, BoneData> originalProportions;
        public Dictionary<string, BoneData> modelProportions;
        public bool Equals(IKRigInfo other) {
            return Equals(RigParent, other.RigParent);
        }
        public override int GetHashCode() {
            return HashCode.Combine(RigParent);
        }
    }
}