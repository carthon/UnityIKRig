using System.Collections.Generic;
using System.Linq;
using Enums;
using RigAnimationSystem.ArmatureBuilderPackage;
using RigAnimationSystem.ArmatureBuilderPackage.Strategies;
using RigAnimationSystem.IKRigBuilderPackage;
using RigAnimationSystem.IKRigBuilderPackage.Strategies;
using Structures;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using BodyPart = Enums.BodyPart;

namespace RigAnimationSystem {
    [RequireComponent(typeof(RigBuilder))]
    public class Armature : MonoBehaviour {
        [SerializeField]
        private List<StringBoneDataPair> BoneData;
        private Dictionary<string, BoneData> _boneData;
        private IKComponent[] _ikComponents;
        [SerializeField]
        private IKRigStrategy _ikRigStrategy;
        [SerializeField]
        private BoneData _hips;
        public BoneData Hips
        {
            get => _hips;
            set => _hips = value;
        }
        [SerializeField]
        private IKRig _ikRig;
        public IKRig IKRig
        {
            get => _ikRig;
            set => _ikRig = value;
        }
        private RigBuilder _rigBuilder;
        [SerializeField]
        private List<GameObject> _createdGameObjects = new List<GameObject>();

        public void CreateArmature() {
            Debug.Log("Creating Armature for model " + transform.name);
            _boneData = new Dictionary<string, BoneData>();
            _ikRig ??= new IKRig(this);
            BoneData = new List<StringBoneDataPair>();
            ArmatureBuilder armatureBuilder = new ArmatureBuilder();
            switch (_ikRigStrategy) {
                case IKRigStrategy.HUMANOID:
                    armatureBuilder.SetStrategy(new HumanoidArmatureStrategy(this));
                    break;
                default:
                    Debug.LogError("No building strategy selected");
                    break;
            }
            _boneData = armatureBuilder.Build();
            UpdateBoneEditorList();
        }
        public void CreateIKRig() {
            if (_boneData?.Count > 0) {
                _ikRig ??= new IKRig(this);
                if (TryGetComponent(out _rigBuilder)) {
                    Debug.Log("Creating IKRig constrains");
                    IKRigBuilder ikRigBuilder = new IKRigBuilder();
                    switch (_ikRigStrategy) {
                        case IKRigStrategy.HUMANOID:
                            ikRigBuilder.SetStrategy(new HumanoidRigStrategy(_rigBuilder, this));
                            break;
                        default:
                            Debug.LogError("No building strategy selected");
                            break;
                    }
                    _createdGameObjects = ikRigBuilder.Build();
                }
                else {
                    Debug.LogError("No RigBuilder attached to model");
                }
            }
            _ikRig.UpdateList();
        }
        public void ResetIKRig() {
            if (_rigBuilder is not null) _rigBuilder.layers.Clear();
            foreach (GameObject createdGameObject in _createdGameObjects) {
                DestroyImmediate(createdGameObject);
            }
            _ikRig.Clear();
            _createdGameObjects.Clear();
        }
        public BoneData FindBone(BodyPart bodyPart, BodyPartSide bodyPartSide, int boneIndex, bool isTip) {
            if (_boneData is not null && _boneData.TryGetValue(ArmatureUtils.BoneNameFormatter(bodyPart, bodyPartSide, boneIndex, isTip), out BoneData boneData))
                return boneData;
            return new BoneData();
        }
        public BoneData FindBoneByRefPath(string transformPath) => 
            _boneData.First(keyValue => transformPath.Equals(AnimationUtility.CalculateTransformPath(keyValue.Value.boneRef, _hips.boneRef))).Value;
        private void UpdateBoneEditorList() {
            BoneData.Clear();
            foreach (string key in _boneData.Keys) {
                BoneData.Add(new StringBoneDataPair{ bone = _boneData[key], name = key });
            }
        }
        private void UpdateBoneDictionary() {
            if (_boneData is null && BoneData is not null) {
                _boneData = new Dictionary<string, BoneData>();
                Debug.Log($"Reloaded {name} bone dictionary");
                foreach (var key in BoneData) {
                    _boneData.Add(key.name, key.bone);
                }
            }
        }
        public void OnValidate() {
            UpdateBoneDictionary();
        }
        public static IKRigInfo GetRigInfo(Transform selectedObject, Rig[] selectedRigs) {
            IKRigInfo ikRigInfo;
            ikRigInfo.Rigs = new Dictionary<Rig, Transform[]>();
            ikRigInfo.PathConstrains = new Dictionary<string, Transform>();
            ikRigInfo.originalProportions = new Dictionary<string, BoneData>();
            ikRigInfo.modelProportions = new Dictionary<string, BoneData>();
            Transform[] rigTransforms = selectedObject.GetComponentsInChildren<Transform>();
            if (rigTransforms.Length == 0) {
                ikRigInfo.RigParent = null;
                Debug.LogError("No se ha encontrado ningun Rig Parent válido");
                return ikRigInfo;
            }
            ikRigInfo.RigParent = selectedObject.transform;
            foreach (Transform transform in rigTransforms) {
                ikRigInfo.PathConstrains.Add(AnimationUtility.CalculateTransformPath(transform, ikRigInfo.RigParent), transform);
            }
            foreach (Rig rig in selectedRigs) {
                List<Transform> childTransforms = rig.GetComponentsInChildren<Transform>().ToList();
                ikRigInfo.Rigs.Add(rig, childTransforms.ToArray());
            }
            return ikRigInfo;
        }
        public static IKRigInfo CopyIKRigInfo(IKRigInfo ikRigInfoOriginal) {
            IKRigInfo ikRigInfo = new IKRigInfo {
                RigParent = ikRigInfoOriginal.RigParent,
                Rigs = ikRigInfoOriginal.Rigs is not null ? new Dictionary<Rig, Transform[]>(ikRigInfoOriginal.Rigs) : new Dictionary<Rig, Transform[]>(),
                PathConstrains = ikRigInfoOriginal.PathConstrains is not null ? new Dictionary<string, Transform>(ikRigInfoOriginal.PathConstrains) : new Dictionary<string, Transform>(),
                originalProportions = ikRigInfoOriginal.originalProportions is not null ? new Dictionary<string, BoneData>(ikRigInfoOriginal.originalProportions) : new Dictionary<string, BoneData>(),
                modelProportions = ikRigInfoOriginal.modelProportions is not null ? new Dictionary<string, BoneData>(ikRigInfoOriginal.modelProportions) : new Dictionary<string, BoneData>()
            };
            return ikRigInfo;
        }
    }
}