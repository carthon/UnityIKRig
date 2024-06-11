using System.Collections.Generic;
using System.Linq;
using Enums;
using Structures;
using UnityEngine;

namespace RigAnimationSystem.ArmatureBuilderPackage.Strategies {
    public class HumanoidArmatureStrategy : IArmatureStrategy {
        private Dictionary<string, BoneData> _boneData;
        private Transform _startingStransform;
        private Armature _armature;
        public HumanoidArmatureStrategy(Armature armature) {
            _startingStransform = armature.transform;
            _armature = armature;
            _boneData = new Dictionary<string, BoneData>();
        }
        public Dictionary<string, BoneData> Execute() {
            CreateTipBones();
            string[] actualBones = _boneData.Keys.ToArray();
            foreach (string key in actualBones) {
                CreateInnerBones(_boneData[key]);
            }
            return _boneData;
        }
        private void CreateTipBones() {
            BoneData hips = new BoneData() {
                boneRef = ArmatureUtils.FindHips(1, 0, _startingStransform, out int foundIndex),
                bodyPart = BodyPart.HIPS,
                bodyPartSide = BodyPartSide.CENTER,
                indexFromRoot = foundIndex,
                boneName = BodyPart.HIPS.ToString()
            };
            hips.length = Utils.GetBoneLength(hips.boneRef);
            _armature.Hips = hips;
            int childCount = hips.boneRef.childCount;
            for (int i = 0; i < childCount; i++) {
                Transform child = hips.boneRef.GetChild(i);
                if (child.childCount >= 1) {
                    Transform finalChild = ArmatureUtils.GetEndChild(0, child, out int indexFromHips);
                    if (hips.boneRef.position.y - finalChild.position.y < 0) { // Tren superior
                        BoneData bone = new BoneData() {
                            bodyPart = BodyPart.TORSO,
                            boneName = ArmatureUtils.BoneNameFormatter(BodyPart.TORSO, BodyPartSide.CENTER, indexFromHips, true),
                            bodyPartSide = BodyPartSide.CENTER,
                            boneRef = finalChild,
                            indexFromRoot = indexFromHips
                        }; // TORSO
                        bone.length = Utils.GetBoneLength(bone.boneRef);
                        if (!_boneData.TryAdd(bone.boneName, bone))
                            Debug.LogError($"Couldnt add {bone.boneName} because already exists");
                        int indexFromSpine = indexFromHips + 1;
                        for (int j = 0; j < finalChild.childCount; j++) {
                            child = finalChild.GetChild(j);
                            Transform finalTorsoChild = ArmatureUtils.GetEndChild(indexFromSpine, child, out indexFromHips);
                            if (child.position.y - finalTorsoChild.position.y > 0) {
                                BodyPartSide side = (hips.boneRef.position.x - finalTorsoChild.position.x < 0 ? BodyPartSide.RIGHT : BodyPartSide.LEFT);
                                bone = new BoneData() {
                                    bodyPart = BodyPart.ARM,
                                    bodyPartSide = side,
                                    boneName = ArmatureUtils.BoneNameFormatter(BodyPart.ARM, side, indexFromHips, true),
                                    boneRef = finalTorsoChild,
                                    indexFromRoot = indexFromHips
                                }; //ARM
                                bone.length = Utils.GetBoneLength(bone.boneRef);
                                if (!_boneData.TryAdd(bone.boneName, bone))
                                    Debug.LogError($"Couldnt add {bone.boneName} because already exists");
                            }
                            else {
                                bone = new BoneData() {
                                    bodyPart = BodyPart.HEAD,
                                    bodyPartSide = BodyPartSide.CENTER,
                                    boneName = ArmatureUtils.BoneNameFormatter(BodyPart.HEAD, BodyPartSide.CENTER, indexFromHips, true),
                                    boneRef = finalTorsoChild,
                                    indexFromRoot = indexFromHips
                                };
                                bone.length = Utils.GetBoneLength(bone.boneRef);
                                if (!_boneData.TryAdd(bone.boneName, bone))
                                    Debug.LogError($"Couldnt add {bone.boneName} because already exists");
                            } //HEAD
                        }
                    }
                    else if (hips.boneRef.position.y - finalChild.position.y > 0) { //Tren inferior
                        BodyPartSide side = (hips.boneRef.position.x - finalChild.position.x < 0 ? BodyPartSide.RIGHT : BodyPartSide.LEFT);
                        BoneData bone = new BoneData() {
                            bodyPart = BodyPart.LEG,
                            bodyPartSide = side,
                            boneName = ArmatureUtils.BoneNameFormatter(BodyPart.LEG, side, indexFromHips, true),
                            boneRef = finalChild,
                            indexFromRoot = indexFromHips
                        }; // LEG
                        bone.length = Utils.GetBoneLength(bone.boneRef);
                        if (!_boneData.TryAdd(bone.boneName, bone))
                            Debug.LogError($"Couldnt add {bone.boneName} because already exists");
                    }
                }
            }
        }
        private void CreateInnerBones(BoneData boneTip) {
            Transform actualBone = boneTip.boneRef;
            int index = boneTip.indexFromRoot;
            BoneData boneData;
            while (actualBone.parent != null) {
                actualBone = actualBone.parent;
                index--;
                if (actualBone.Equals(_armature.Hips.boneRef) || 
                    actualBone.Equals(_boneData[ArmatureUtils.BoneNameFormatter(BodyPart.TORSO, BodyPartSide.CENTER, 0, true)].boneRef)) {
                    return;
                }
                boneData = new BoneData {
                    bodyPart = boneTip.bodyPart,
                    bodyPartSide = boneTip.bodyPartSide,
                    boneName = ArmatureUtils.BoneNameFormatter(boneTip.bodyPart, boneTip.bodyPartSide, index, false),
                    boneRef = actualBone,
                    indexFromRoot = index,
                    length = Utils.GetBoneLength(actualBone)
                };
                if (!_boneData.TryAdd(boneData.boneName, boneData))
                    Debug.LogError($"Couldnt add {boneData.boneName} because already exists");
            }
        }
    }
}