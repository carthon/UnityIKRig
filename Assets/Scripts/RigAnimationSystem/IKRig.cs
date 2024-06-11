using System;
using System.Collections.Generic;
using System.Linq;
using Structures;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace RigAnimationSystem {
    [Serializable]
    public class IKRig {
        private Dictionary<BoneData, List<IRigConstraint>> _rigConstraints;
        public List<BoneDataConstrainsList> _rigConstraintsList;
        private Armature _armature;

        public IKRig(Armature armature, Dictionary<BoneData, List<IRigConstraint>> rigConstraints) {
            _rigConstraints = new Dictionary<BoneData, List<IRigConstraint>>(rigConstraints);
            _armature = armature;
        }
        public IKRig(Armature armature) {
            _rigConstraints = new Dictionary<BoneData, List<IRigConstraint>>();
            _rigConstraintsList = new List<BoneDataConstrainsList>();
            _armature = armature;
        }
        public BoneData FindConstrainByPath(string path) => _rigConstraintsList.First(
                list => list.constrainsList.Exists(
                    gameObject => path.Equals(AnimationUtility.CalculateTransformPath(gameObject.transform, _armature.Hips.boneRef)))).boneData;
        public bool TryAddRigConstrain(BoneData bone, IRigConstraint rigConstraint) {
            bool canAddBone = _rigConstraints.TryAdd(bone, new List<IRigConstraint>() { rigConstraint });
            bool canAddConstrain = false;
            if (!canAddBone) {
                canAddConstrain = _rigConstraints.TryGetValue(bone, out List<IRigConstraint> rigConstraints);
                if (canAddConstrain) {
                    rigConstraints.Add(rigConstraint);
                }
            }
            return canAddBone || canAddConstrain;
        }
        public void UpdateList() {
            _rigConstraintsList.Clear();
            foreach (BoneData key in _rigConstraints.Keys) {
                List<GameObject> gameObjects = new List<GameObject>();
                foreach (IRigConstraint constrain in _rigConstraints[key]) {
                    gameObjects.Add(constrain.component.gameObject);
                }
                _rigConstraintsList.Add(new BoneDataConstrainsList{ boneData = key, constrainsList = new List<GameObject>(gameObjects)});
            }
        }
        public bool TryGetRigConstrainList(BoneData bone, out List<IRigConstraint> value) => _rigConstraints.TryGetValue(bone, out value);
        public void Clear() {
            _rigConstraints.Clear();
            _rigConstraintsList.Clear();
        }
    }
}