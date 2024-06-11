using System.Collections.Generic;
using Enums;
using RigAnimationSystem.ArmatureBuilderPackage;
using Structures;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace RigAnimationSystem.IKRigBuilderPackage.Strategies {
    public class HumanoidRigStrategy : IRigBuilderStrategy {
        private RigBuilder _rigBuilder;
        private Armature _armature;
        List<GameObject> _createdGameObjects = new List<GameObject>();
        public HumanoidRigStrategy(RigBuilder rigBuilder, Armature armature) {
            _rigBuilder = rigBuilder;
            _armature = armature;
        }
        public List<GameObject> Execute() {
            //TODO: Necesito que cojan la posición global de los huesos para que no se queden en 0,0,0
            GameObject rigLayer = new GameObject("FullBodyIKRig");
            ResetAndParentGameObject(rigLayer.transform, _rigBuilder.transform);
            Rig rig = rigLayer.AddComponent<Rig>();

            //Setting Hips Up with Transform override
            GameObject hips = new GameObject($"{BodyPart.HIPS.ToString()}");
            GameObject hipsParentConstrain = new GameObject($"{BodyPart.HIPS.ToString()}_parentConstrain");
            GameObject hipsTarget = new GameObject($"{BodyPart.HIPS.ToString()}_target");
            ResetAndParentGameObject(hips.transform, _armature.Hips.boneRef);
            ResetAndParentGameObject(hipsParentConstrain.transform, _armature.FindBone(BodyPart.TORSO, BodyPartSide.CENTER, 0, true).boneRef);
            ResetAndParentGameObject(hipsTarget.transform, hips.transform);
            hips.transform.SetParent(rigLayer.transform);
            hipsParentConstrain.transform.SetParent(hips.transform);
            hipsTarget.transform.SetParent(hips.transform);
            MultiParentConstraint multiParentConstraint = hips.AddComponent<MultiParentConstraint>();
            WeightedTransform parentConstraintHips = new WeightedTransform(hipsParentConstrain.transform, 1f);
            ConfigureMultiParentConstrain(
                _armature.Hips.boneRef,
                new List<WeightedTransform> { parentConstraintHips },
                multiParentConstraint
            );
            if (!_armature.IKRig.TryAddRigConstrain(_armature.Hips, multiParentConstraint))
                Debug.LogError($"No se pudo añadir al IKRig {_armature.Hips.boneName}");
            MultiReferentialConstraint multiReferentialConstraint = hips.AddComponent<MultiReferentialConstraint>();
            ConfigureMultiReferentialConstrain(
                1,
                new List<Transform> { hipsParentConstrain.transform, hipsTarget.transform },
                multiReferentialConstraint
            );
            if (!_armature.IKRig.TryAddRigConstrain(_armature.Hips, multiReferentialConstraint))
                Debug.LogError($"No se pudo añadir al IKRig {_armature.Hips.boneName}");
            _rigBuilder.layers.Add(new RigLayer(rig));

            //Setting Torso Up Twist Chain
            GameObject torso = new GameObject($"{BodyPart.TORSO.ToString()}");
            ResetAndParentGameObject(torso.transform, _armature.FindBone(BodyPart.TORSO, BodyPartSide.CENTER, 0, true).boneRef);
            torso.transform.SetParent(rigLayer.transform);
            TwistChainConstraint torsoChainIK = torso.AddComponent<TwistChainConstraint>();
            BoneData torsoTip = _armature.FindBone(BodyPart.TORSO, BodyPartSide.CENTER, 0, true);
            BoneData firstSpineBone = ArmatureUtils.FindFirstBoneOfType(_armature, torsoTip); //Calculamos en ambos brazos por si acaso el rig no es simétrico
            ConfigureTwistChain(
                torsoTip,
                firstSpineBone,
                new[] {
                    new Keyframe(0f, 0f),
                    new Keyframe(1, 1f)
                },
                torsoChainIK
            );
            if (!_armature.IKRig.TryAddRigConstrain(torsoTip, torsoChainIK))
                Debug.LogError($"No se pudo añadir al IKRig {torsoTip.boneName}");
            if (!_armature.IKRig.TryAddRigConstrain(firstSpineBone, torsoChainIK))
                Debug.LogError($"No se pudo añadir al IKRig {firstSpineBone.boneName}");
            //Setting Neck with TwistChain, Local vectors must be normalized in order to work properly

            //Setting Legs with TwoBoneIK
            GameObject leftLeg = new GameObject($"{BodyPart.LEG.ToString()}{BodyPartSide.LEFT.ToString()}");
            ResetAndParentGameObject(leftLeg.transform, rigLayer.transform);
            TwoBoneIKConstraint leftLegTwoBoneIK = leftLeg.AddComponent<TwoBoneIKConstraint>();
            ConfigureTwoBoneIK(
                _armature.FindBone(BodyPart.LEG, BodyPartSide.RIGHT, 2, false),
                _armature.FindBone(BodyPart.LEG, BodyPartSide.RIGHT, 1, false),
                _armature.FindBone(BodyPart.LEG, BodyPartSide.RIGHT, 0, false),
                leftLegTwoBoneIK
            );
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.LEG, BodyPartSide.RIGHT, 2, false), leftLegTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {leftLeg.name}");
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.LEG, BodyPartSide.RIGHT, 1, false), leftLegTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {leftLeg.name}");
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.LEG, BodyPartSide.RIGHT, 0, false), leftLegTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {leftLeg.name}");

            GameObject rightLeg = new GameObject($"{BodyPart.LEG.ToString()}{BodyPartSide.RIGHT.ToString()}");
            ResetAndParentGameObject(rightLeg.transform, rigLayer.transform);
            TwoBoneIKConstraint rightLegTwoBoneIK = rightLeg.AddComponent<TwoBoneIKConstraint>();
            ConfigureTwoBoneIK(
                _armature.FindBone(BodyPart.LEG, BodyPartSide.LEFT, 2, false),
                _armature.FindBone(BodyPart.LEG, BodyPartSide.LEFT, 1, false),
                _armature.FindBone(BodyPart.LEG, BodyPartSide.LEFT, 0, false),
                rightLegTwoBoneIK
            );
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.LEG, BodyPartSide.LEFT, 2, false), rightLegTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {rightLeg.name}");
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.LEG, BodyPartSide.LEFT, 1, false), rightLegTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {rightLeg.name}");
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.LEG, BodyPartSide.LEFT, 0, false), rightLegTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {rightLeg.name}");

            BoneData leftArmTip = _armature.FindBone(BodyPart.ARM, BodyPartSide.LEFT, 0, true);
            BoneData firstLeftArmBone = ArmatureUtils.FindFirstBoneOfType(_armature, leftArmTip); //Calculamos en ambos brazos por si acaso el rig no es simétrico
            bool leftHandIsTip = _armature.FindBone(BodyPart.ARM, BodyPartSide.LEFT, 0, true).boneRef.childCount >= 2;
            BoneData rightArmTip = _armature.FindBone(BodyPart.ARM, BodyPartSide.RIGHT, 0, true);
            BoneData firstRightArmBone = ArmatureUtils.FindFirstBoneOfType(_armature, rightArmTip);
            bool rightHandIsTip = _armature.FindBone(BodyPart.ARM, BodyPartSide.LEFT, 0, true).boneRef.childCount >= 2;

            //Setting Shoulders with Transform override?
            GameObject leftShoulder = new GameObject($"{BodyPart.SHOULDER.ToString()}{BodyPartSide.LEFT.ToString()}");
            ResetAndParentGameObject(leftShoulder.transform, rigLayer.transform);
            GameObject rightShoulder = new GameObject($"{BodyPart.SHOULDER.ToString()}{BodyPartSide.RIGHT.ToString()}");
            ResetAndParentGameObject(rightShoulder.transform, rigLayer.transform);

            //Setting Arms with TwoBoneIK
            GameObject leftArm = new GameObject($"{BodyPart.ARM.ToString()}{BodyPartSide.LEFT.ToString()}");
            ResetAndParentGameObject(leftArm.transform, rigLayer.transform);
            TwoBoneIKConstraint leftArmTwoBoneIK = leftArm.AddComponent<TwoBoneIKConstraint>();
            ConfigureTwoBoneIK(
                _armature.FindBone(BodyPart.ARM, BodyPartSide.LEFT, firstLeftArmBone.indexFromRoot + 3, leftHandIsTip),
                _armature.FindBone(BodyPart.ARM, BodyPartSide.LEFT, firstLeftArmBone.indexFromRoot + 2, false),
                _armature.FindBone(BodyPart.ARM, BodyPartSide.LEFT, firstLeftArmBone.indexFromRoot + 1, false),
                leftArmTwoBoneIK
            );
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.ARM, BodyPartSide.LEFT, firstLeftArmBone.indexFromRoot + 3, leftHandIsTip), leftArmTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {leftArm.name}");
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.ARM, BodyPartSide.LEFT, firstLeftArmBone.indexFromRoot + 2, false), leftArmTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {leftArm.name}");
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.ARM, BodyPartSide.LEFT, firstLeftArmBone.indexFromRoot + 1, false), leftArmTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {leftArm.name}");

            GameObject rightArm = new GameObject($"{BodyPart.ARM.ToString()}{BodyPartSide.RIGHT.ToString()}");
            ResetAndParentGameObject(rightArm.transform, rigLayer.transform);
            TwoBoneIKConstraint rightArmTwoBoneIK = rightArm.AddComponent<TwoBoneIKConstraint>();
            ConfigureTwoBoneIK(
                _armature.FindBone(BodyPart.ARM, BodyPartSide.RIGHT, firstRightArmBone.indexFromRoot + 3, rightHandIsTip),
                _armature.FindBone(BodyPart.ARM, BodyPartSide.RIGHT, firstRightArmBone.indexFromRoot + 2, false),
                _armature.FindBone(BodyPart.ARM, BodyPartSide.RIGHT, firstRightArmBone.indexFromRoot + 1, false),
                rightArmTwoBoneIK
            );
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.ARM, BodyPartSide.RIGHT, firstRightArmBone.indexFromRoot + 3, rightHandIsTip), rightArmTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {leftArm.name}");
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.ARM, BodyPartSide.RIGHT, firstRightArmBone.indexFromRoot + 2, false), rightArmTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {leftArm.name}");
            if (!_armature.IKRig.TryAddRigConstrain(_armature.FindBone(BodyPart.ARM, BodyPartSide.RIGHT, firstRightArmBone.indexFromRoot + 1, false), rightArmTwoBoneIK))
                Debug.LogError($"No se pudo añadir al IKRig {leftArm.name}");

            _createdGameObjects.AddRange(
                new[] {
                    rigLayer, hips, hipsTarget, hipsParentConstrain, torso,
                    leftLeg, rightLeg, leftShoulder, rightShoulder, leftArm, rightArm
                }
            );
            return _createdGameObjects;
        }
        private void ConfigureMultiReferentialConstrain(int driverIndex, List<Transform> sourceObjects, MultiReferentialConstraint multiReferentialConstraint) {
            multiReferentialConstraint.data = new MultiReferentialConstraintData() {
                sourceObjects = new List<Transform>(sourceObjects),
                driver = driverIndex
            };
        }
        private void ConfigureMultiParentConstrain(Transform constrainedObject, List<WeightedTransform> sourceObjects, MultiParentConstraint multiParentConstraint) {
            WeightedTransformArray weightedTransformArray = new WeightedTransformArray();
            foreach (WeightedTransform weightedTransform in sourceObjects) {
                weightedTransformArray.Add(weightedTransform);
            }

            multiParentConstraint.data = new MultiParentConstraintData() {
                constrainedObject = constrainedObject,
                sourceObjects = weightedTransformArray,
                maintainRotationOffset = true,
                maintainPositionOffset = true,
                constrainedPositionXAxis = true,
                constrainedPositionYAxis = true,
                constrainedPositionZAxis = true,
                constrainedRotationXAxis = true,
                constrainedRotationYAxis = true,
                constrainedRotationZAxis = true
            };
        }
        private void ConfigureTwistChain(BoneData tip, BoneData root, Keyframe[] curveKeyframes, TwistChainConstraint twistChainConstraint) {
            GameObject tipTarget = new GameObject($"{tip.bodyPart}{tip.bodyPartSide}_target");
            ResetAndParentGameObject(tipTarget.transform, tip.boneRef);
            tipTarget.transform.SetParent(twistChainConstraint.transform);
            GameObject rootTarget = new GameObject($"{root.bodyPart}{root.bodyPartSide}_root");
            ResetAndParentGameObject(rootTarget.transform, root.boneRef);
            rootTarget.transform.SetParent(twistChainConstraint.transform);
            twistChainConstraint.data = new TwistChainConstraintData() {
                root = root.boneRef,
                tip = tip.boneRef,
                curve = new AnimationCurve(curveKeyframes),
                rootTarget = rootTarget.transform,
                tipTarget = tipTarget.transform
            };
            twistChainConstraint.weight = 1f;
            _createdGameObjects.AddRange(new[] { rootTarget, tipTarget });
        }
        private void ConfigureIKChain(BoneData tip, BoneData root, ChainIKConstraint chainIkConstrain) {
            GameObject tipTarget = new GameObject($"{tip.bodyPart}{tip.bodyPartSide}_target");
            ResetAndParentGameObject(tipTarget.transform, tip.boneRef);
            tipTarget.transform.SetParent(chainIkConstrain.transform);
            GameObject rootTarget = new GameObject($"{root.bodyPart}{root.bodyPartSide}_root");
            ResetAndParentGameObject(rootTarget.transform, root.boneRef);
            rootTarget.transform.SetParent(chainIkConstrain.transform);
            chainIkConstrain.data = new ChainIKConstraintData {
                root = root.boneRef,
                tip = tip.boneRef,
                target = tipTarget.transform,
                chainRotationWeight = 0.5f,
                tipRotationWeight = 0.35f,
                maxIterations = 15,
                tolerance = 0.0001f,
                maintainTargetPositionOffset = true,
                maintainTargetRotationOffset = true,

            };
            chainIkConstrain.weight = 1f;
            _createdGameObjects.AddRange(new[] { rootTarget, tipTarget });
        }
        private void ConfigureTwoBoneIK(BoneData tip, BoneData mid, BoneData root, TwoBoneIKConstraint twoBoneIKTransform, bool noHint = false) {
            GameObject target = new GameObject($"{tip.bodyPart}{tip.bodyPartSide}_target");
            ResetAndParentGameObject(target.transform, tip.boneRef);
            target.transform.SetParent(twoBoneIKTransform.transform);
            GameObject hint = new GameObject($"{mid.bodyPart}{mid.bodyPartSide}_hint");
            ResetAndParentGameObject(hint.transform, mid.boneRef);
            hint.transform.SetParent(twoBoneIKTransform.transform);
            TwoBoneIKConstraintData data = new TwoBoneIKConstraintData() {
                tip = tip.boneRef,
                mid = mid.boneRef,
                root = root.boneRef,
                target = target.transform,
                hint = noHint ? null : hint.transform,
                hintWeight = 1f,
                targetPositionWeight = 1f,
                targetRotationWeight = 1f,
                maintainTargetPositionOffset = true,
                maintainTargetRotationOffset = true
            };
            twoBoneIKTransform.weight = 1f;
            twoBoneIKTransform.data = data;
            _createdGameObjects.AddRange(new[] { target, hint });
        }
        private void ResetAndParentGameObject(Transform child, Transform parent) {
            child.SetParent(parent);
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
        }
    }
}