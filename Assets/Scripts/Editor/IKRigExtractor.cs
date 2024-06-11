using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Enums;
using RigAnimationSystem;
using Structures;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using BodyPart = Enums.BodyPart;
using Object = UnityEngine.Object;

namespace Editor {
    public class IKRigExtractor : EditorWindow {
        private string _configPath = "Assets/Configurations/IKRigSetup.asset";
        private AnimationClip _animationClip;
        private List<Armature> _armatures = new List<Armature>();
        private IKRigInfo _ikRigInfo;
        private Dictionary<Rig, bool> _rigFoldouts = new Dictionary<Rig, bool>();
        private Dictionary<ushort, bool> _foldouts = new Dictionary<ushort, bool>();

        #region ProportionSearcherVar

        private bool _isTip;
        private int _boneIndex;
        private BodyPart _bodyPart;
        private BodyPartSide _bodyPartSide;
        #endregion
        private enum FoldoutsEnum : ushort {
            Proportions,
            Rigs,
            AnimationObjects,
            TransformObjects
        };
        private Dictionary<string, Transform> _animatedObjects = new Dictionary<string, Transform>();
        private List<AnimationClip> _clips = new List<AnimationClip>();
        private string _lastSelectedDirectory = "";
        private string _newClipName = "";
        private GameObject _originalModel;
        private Vector2 _scrollPosition;

        private List<Transform> _originalTransforms = new List<Transform>();
        private List<Transform> _retargetedTransform = new List<Transform>();
        public static void ShowWindow() {
            IKRigExtractor window = GetWindow<IKRigExtractor>("IK Rig Extractor");
            Rig[] selectedRigs = Selection.GetFiltered<Rig>(SelectionMode.Deep);
            Transform[] selectedObject = Selection.GetFiltered<Transform>(SelectionMode.TopLevel);
            if (selectedObject.Length > 0) {
                foreach (Transform t in selectedObject) {
                    if (t.TryGetComponent(out Armature armature))
                        window._armatures.Add(armature);
                    else
                        Debug.Log("No se ha podido cargar ningún Armature");
                }
            } 
            else {
                IKRigInfo ikRigInfo = Armature.GetRigInfo(selectedObject[0], selectedRigs);
                if(window._ikRigInfo.RigParent is null && !window.LoadData() || !window._ikRigInfo.Equals(ikRigInfo)) {
                    window._ikRigInfo = ikRigInfo;
                }
            }
            window.LoadProperties();
        }
        private void OnEnable() {
            LoadProperties();
            if (_ikRigInfo.RigParent is null) {
                LoadData();
            }
        }
        private bool LoadData() {
            Debug.Log("Loading configuration");
            IKRigSetup configuration = Utils.LoadScriptableObject<IKRigSetup>(_configPath);
            if (configuration != null) {
                _animationClip = configuration.AnimationClip;
                _lastSelectedDirectory = configuration.LastSelectedDirectory;
                _clips = configuration.Clips;
                // Convert Foldouts list to dictionary
                _rigFoldouts = new Dictionary<Rig, bool>();
                foreach (var pair in configuration.Foldouts)
                {
                    _rigFoldouts[pair.rig] = pair.foldout;
                }

                // Convert AnimatedObjects list to dictionary
                _animatedObjects = new Dictionary<string, Transform>();
                foreach (var pair in configuration.AnimatedObjects)
                {
                    Transform foundTransform = GameObject.Find(pair.name)?.transform;
                    if (foundTransform != null)
                    {
                        _animatedObjects[pair.name] = foundTransform;
                    }
                }
                _ikRigInfo = Armature.CopyIKRigInfo(configuration.IKRigInfo);
                _originalModel = configuration.SavedModel;
                return true;
            }
            Debug.LogError("Couldnt find IKRigInfo data");
            return false; 
        }
        private void PersistData() {
            Debug.Log("Saving configuration");
            IKRigSetup configuration = Utils.LoadScriptableObject<IKRigSetup>(_configPath);
            if (configuration != null) {
                configuration.AnimationClip = _animationClip;
                configuration.LastSelectedDirectory = _lastSelectedDirectory;
                configuration.Clips = new List<AnimationClip>(_clips);
                // Convert Foldouts dictionary to list
                configuration.Foldouts = new List<DataPairStructures>();
                foreach (var kvp in _rigFoldouts) {
                    configuration.Foldouts.Add(new DataPairStructures { rig = kvp.Key, foldout = kvp.Value });
                }

                // Convert AnimatedObjects dictionary to list
                configuration.AnimatedObjects = new List<StringTransformPair>();
                foreach (var kvp in _animatedObjects) {
                    configuration.AnimatedObjects.Add(new StringTransformPair { name = kvp.Key, transform = kvp.Value });
                }
                configuration.IKRigInfo = Armature.CopyIKRigInfo(_ikRigInfo);
                configuration.SavedModel = _originalModel;
                
                EditorUtility.SetDirty(configuration);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else {
                Debug.LogError("Couldnt find IKRigInfo data");
            }
        }
        private void OnGUI() {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            if (_armatures is not null && _armatures.Count > 0) {
                GUIBoneProportionsComparer();
                EditorGUILayout.Separator();
                GUIIKRigRetargeting();
            }
            else {
                GUIIKRigExtractor();
            }
            GUILayout.EndScrollView();
        }
        private void GUIIKRigRetargeting() {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            float labelPercentage = EditorGUIUtility.currentViewWidth / 2;
            float objectPercentage = EditorGUIUtility.currentViewWidth / 5;
            EditorGUILayout.LabelField("Select IK Animation (Generated with Transfer Motion)", GUILayout.Width(labelPercentage));
            if (_animationClip is not null) {
                EditorGUILayout.ObjectField(_animationClip, typeof(AnimationClip), false, GUILayout.Width(objectPercentage));
            }
            if (GUILayout.Button("Load Animation Clip", GUILayout.Width(objectPercentage))) {
                LoadAnimationClip();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUIShowAnimatedObjects();
            if (_armatures.Count > 1) {
                if (_originalTransforms.Count > 0 && _retargetedTransform.Count > 0) {
                    GUIShowTransforms(_originalTransforms, "Original Transform");
                    GUIShowTransforms(_retargetedTransform, "Retargeted Transforms");
                }
                else {
                    _originalTransforms.Clear();
                    _retargetedTransform.Clear();
                    foreach (string animatedObjectsPath in _animatedObjects.Keys) {
                        Armature originalArmature = _armatures[1];
                        Armature retargetedModelArmature = _armatures[0];
                        Transform originalTransform = originalArmature.transform.Find(animatedObjectsPath);
                        Transform retargetedTransform = retargetedModelArmature.transform.Find(animatedObjectsPath);
                        if (originalTransform is not null) _originalTransforms.Add(originalTransform);
                        if (retargetedTransform is not null) _retargetedTransform.Add(retargetedTransform);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        private void GUIShowAnimatedObjects() {
            if (_animatedObjects?.Count > 0) {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                bool currentState = _foldouts[(ushort)FoldoutsEnum.AnimationObjects];
                bool newState = EditorGUILayout.Foldout(currentState, "Animation Objects");
                if (newState != currentState) {
                    _foldouts[(ushort)FoldoutsEnum.AnimationObjects] = newState;
                }
                EditorGUILayout.EndHorizontal();
                if (_foldouts[(ushort)FoldoutsEnum.AnimationObjects]) { // Si el foldout está expandido, mostrar los Transforms
                    EditorGUILayout.BeginVertical();
                    foreach (var animatedObject in _animatedObjects.Keys) {
                        int lastIndex = animatedObject.LastIndexOf("/", StringComparison.Ordinal);
                        string objectName = lastIndex > 0 ? animatedObject.Substring(lastIndex + 1) : animatedObject == "" ? "ROOT" : animatedObject;
                        GUIContent labelContent = new GUIContent(objectName);
                        GUIStyle labelStyle = GUI.skin.label;
                        Vector2 labelSize = labelStyle.CalcSize(labelContent);
                        EditorGUILayout.LabelField(labelContent, GUILayout.Width(labelSize.x));
                    }
                    GUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
        }
        private void GUIShowTransforms(List<Transform> transforms, string foldoutTitle = "Transform Objects") {
            if (transforms?.Count > 0) {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                bool currentState = _foldouts[(ushort)FoldoutsEnum.TransformObjects];
                bool newState = EditorGUILayout.Foldout(currentState, foldoutTitle);
                if (newState != currentState) {
                    _foldouts[(ushort)FoldoutsEnum.TransformObjects] = newState;
                }
                EditorGUILayout.EndHorizontal();
                if (_foldouts[(ushort)FoldoutsEnum.TransformObjects]) { // Si el foldout está expandido, mostrar los Transforms
                    EditorGUILayout.BeginVertical();
                    foreach (var t in transforms) {
                        EditorGUILayout.ObjectField(t, typeof(Transform), true);
                    }
                    GUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
        }
        private void GUIBoneProportionsComparer() {
            EditorGUILayout.LabelField("Armature Bone proportions Comparator");
            float labelPercentage = 0.5f * EditorGUIUtility.currentViewWidth / 4;
            float objectPercentage = 0.5f * EditorGUIUtility.currentViewWidth / 4;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select BodyPart", GUILayout.Width(labelPercentage));
            _bodyPart = (BodyPart)EditorGUILayout.EnumPopup(_bodyPart, GUILayout.Width(objectPercentage));
            EditorGUILayout.LabelField("Select PartSide", GUILayout.Width(labelPercentage));
            _bodyPartSide = (BodyPartSide)EditorGUILayout.EnumPopup(_bodyPartSide, GUILayout.Width(objectPercentage));
            EditorGUILayout.LabelField("Select Index", GUILayout.Width(labelPercentage));
            _boneIndex = EditorGUILayout.IntField(_boneIndex, GUILayout.Width(objectPercentage));
            EditorGUILayout.LabelField("Is Tip bone?", GUILayout.MaxWidth(labelPercentage));
            _isTip = EditorGUILayout.Toggle(_isTip);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical();
            if (_armatures?.Count > 0) {
                for (int i = 0; i < _armatures.Count; i++) {
                    Armature armature = _armatures[i];
                    EditorGUILayout.LabelField($"Armature :{armature.name}");
                    EditorGUILayout.BeginHorizontal();
                    BoneData boneData = armature.FindBone(_bodyPart, _bodyPartSide, _boneIndex, _isTip);
                    EditorGUILayout.LabelField(boneData.Equals(new BoneData()) ? $"Bone Data not found for {armature.name}" : $"{boneData.boneName} : {boneData.length}");
                    EditorGUILayout.EndHorizontal();
                    if (i < _armatures.Count - 1) EditorGUILayout.Separator();
                }
            }
            EditorGUILayout.EndVertical();
        }
        private void GUIShowAndLinkAnimatedObjects() {
            if (_animatedObjects?.Count > 0) {
                EditorGUILayout.BeginHorizontal();
                bool currentState = _foldouts[(ushort)FoldoutsEnum.AnimationObjects];
                bool newState = EditorGUILayout.Foldout(currentState, "Animation Objects");
                if (newState != currentState) {
                    _foldouts[(ushort)FoldoutsEnum.AnimationObjects] = newState;
                }
                EditorGUILayout.EndHorizontal();
                List<KeyValuePair<string, Transform>> updates = new List<KeyValuePair<string, Transform>>();
                if (_foldouts[(ushort)FoldoutsEnum.AnimationObjects]) { // Si el foldout está expandido, mostrar los Transforms
                    EditorGUILayout.BeginVertical();
                    foreach (var animatedObject in _animatedObjects.Keys) {
                        Object obj = _animatedObjects[animatedObject];
                        EditorGUILayout.BeginHorizontal();
                        int lastIndex = animatedObject.LastIndexOf("/", StringComparison.Ordinal);
                        string objectName = lastIndex > 0 ? animatedObject.Substring(lastIndex + 1) : animatedObject;
                        GUIContent labelContent = new GUIContent(objectName);
                        GUIStyle labelStyle = GUI.skin.label;
                        Vector2 labelSize = labelStyle.CalcSize(labelContent);
                        EditorGUILayout.LabelField(labelContent, GUILayout.Width(labelSize.x));
                        GUILayout.FlexibleSpace();
                        Transform newObj = EditorGUILayout.ObjectField(obj, typeof(Transform), true) as Transform;
                        if ((obj == null && newObj != null) || (obj != null && newObj != null && !newObj.Equals(obj)))
                            updates.Add(new KeyValuePair<string, Transform>(animatedObject, newObj));
                        else if (newObj == null && obj != null)
                            updates.Add(new KeyValuePair<string, Transform>(animatedObject, null));
                        EditorGUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
                foreach (var update in updates) {
                    _animatedObjects[update.Key] = update.Value;
                }
            }
        }
        private void GUIIKRigExtractor() {
            if (_ikRigInfo.originalProportions?.Count > 0) {
                EditorGUILayout.BeginHorizontal();
                bool currentState = _foldouts[(ushort)FoldoutsEnum.Proportions];
                bool newState = EditorGUILayout.Foldout(currentState, "Animation Model Proportions");
                if (newState != currentState) {
                    _foldouts[(ushort)FoldoutsEnum.Proportions] = newState;
                }
                EditorGUILayout.EndHorizontal();

                if (_foldouts[(ushort)FoldoutsEnum.Proportions]) {
                    EditorGUILayout.BeginHorizontal();
                    StringBuilder stringBuilder = new StringBuilder();
                    if (_ikRigInfo.originalProportions is { Count: > 0 }) {
                        EditorGUILayout.BeginVertical("box");
                        foreach (KeyValuePair<string, BoneData> boneProportion in _ikRigInfo.originalProportions) {
                            string boneName = boneProportion.Key.Split("/").Last();
                            boneName = boneName.Split(":").Length > 1 ? boneName.Split(":")[1] : boneName;
                            stringBuilder.AppendFormat("{0} - {1}", boneName, boneProportion.Value.length);
                            EditorGUILayout.LabelField(stringBuilder.ToString());
                            stringBuilder.Clear();
                        }
                        EditorGUILayout.EndVertical();
                    }
                    if (_ikRigInfo.modelProportions is { Count: > 0 }) {
                        EditorGUILayout.BeginVertical("box");
                        foreach (KeyValuePair<string, BoneData> boneProportion in _ikRigInfo.modelProportions) {
                            string boneName = boneProportion.Key.Split("/").Last();
                            boneName = boneName.Split(":").Length > 1 ? boneName.Split(":")[1] : boneName;
                            stringBuilder.AppendFormat("{0} - {1}", boneName, boneProportion.Value.length);
                            EditorGUILayout.LabelField(stringBuilder.ToString());
                            stringBuilder.Clear();
                        }
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            if (_ikRigInfo.Rigs?.Count > 0) {
                EditorGUILayout.BeginHorizontal();
                bool currentState = _foldouts[(ushort)FoldoutsEnum.Rigs];
                bool newState = EditorGUILayout.Foldout(currentState, "Rig Info");
                if (newState != currentState) {
                    _foldouts[(ushort)FoldoutsEnum.Rigs] = newState;
                }
                EditorGUILayout.EndHorizontal();
                if (_foldouts[(ushort)FoldoutsEnum.Rigs]) {
                    EditorGUILayout.BeginVertical("box");
                    foreach (Rig rig in _ikRigInfo.Rigs.Keys) {
                        EditorGUILayout.BeginHorizontal();
                        currentState = _rigFoldouts[rig];
                        newState = EditorGUILayout.Foldout(currentState, rig.name);
                        if (newState != currentState) {
                            _rigFoldouts[rig] = newState;
                        }
                        EditorGUILayout.EndHorizontal();

                        if (_rigFoldouts[rig]) { // Si el foldout está expandido, mostrar los Transforms
                            Transform[] constraintsTransform = _ikRigInfo.Rigs[rig];
                            EditorGUILayout.BeginHorizontal();
                            foreach (Transform transform in constraintsTransform) {
                                string description = "";
                                IRigConstraint rigConstraint = transform.GetComponent<IRigConstraint>();
                                if (rigConstraint != null)
                                    description = rigConstraint.GetType().Name;
                                SerializedObject serializedObject = new SerializedObject(transform.gameObject);
                                if (serializedObject.targetObject == null)
                                    continue;
                                EditorGUILayout.ObjectField(description, transform.gameObject, typeof(GameObject), true);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            GUIShowAndLinkAnimatedObjects();
            EditorGUILayout.BeginVertical();
            foreach (AnimationClip clip in _clips) {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(clip.name)) {
                    _animationClip = clip;
                    Debug.Log($"Cargando clip {_animationClip.name}");
                    LoadAnimatedObjects();
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            if (GUILayout.Button("Load Animation Clip")) {
                LoadAnimationClip();
            }
            GUILayout.BeginHorizontal();
            _newClipName = GUILayout.TextField(_newClipName);
            if (GUILayout.Button("Save Animation Clip")) {
                TransformAndSaveAnimation();
            }
            GUILayout.EndHorizontal();
        }
        private void LoadProperties() {
            if (_ikRigInfo.Rigs?.Keys != null)
                foreach (var rig in _ikRigInfo.Rigs?.Keys) {
                    _rigFoldouts.TryAdd(rig, false); // Inicializar todos los foldouts como cerrados
                }
            for (int i = 0; i < Enum.GetNames(typeof(FoldoutsEnum)).Length; i++) {
                _foldouts.TryAdd((ushort) i, false);
            }
            //_lastSelectedDirectory = Application.dataPath; // Starts in the Assets folder
        }
        private void ExtractFBXAnimations(string path) {
            path = "Assets" + path.Substring(Application.dataPath.Length);
            var assetImporter = AssetImporter.GetAtPath(path) as ModelImporter;
            _clips.Clear();
            if (assetImporter != null) {
                // This reimports the asset from the path.
                assetImporter.importAnimation = true;
                assetImporter.SaveAndReimport();

                Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object obj in objects) {
                    if (obj is AnimationClip clip) 
                        if (!clip.name.Contains("__preview__")) 
                            _clips.Add(clip);
                }
            }
            else {
                Debug.LogError("Failed to load FBX at path: " + path);
            }
        }
        private void LoadAnimationClip() {
            string clipPath = EditorUtility.OpenFilePanel("Select AnimationClip", _lastSelectedDirectory, "anim,fbx");
            if (clipPath.Length == 0)
                return;
            _lastSelectedDirectory = clipPath.Remove(clipPath.LastIndexOf("/", StringComparison.Ordinal));
            string extension = clipPath.Split(".")[1];
            AnimationClip clip;
            _animatedObjects.Clear();
            if (extension.ToLower() == "fbx") {
                ExtractFBXAnimations(clipPath);
            } else {
                string path = "Assets" + clipPath.Substring(Application.dataPath.Length);
                clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null) {
                    _animationClip = clip;
                    LoadAnimatedObjects();
                }
                else {
                    Debug.LogError("Failed to load AnimationClip");
                }
            }
        }
        private void LoadAnimatedObjects() {
            if (!_animationClip){
                Debug.LogError("We need to select an animation before load curves");
                return;
            }
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(_animationClip);
            foreach (EditorCurveBinding curveBinding in curveBindings) {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(_animationClip, curveBinding);
                _animatedObjects.TryAdd(curveBinding.path, null);
            }
        }

        void ApplyCurveToClip(AnimationClip clip, string path, string propertyBase, List<Keyframe> keyframesX, List<Keyframe> keyframesY, List<Keyframe> keyframesZ, List<Keyframe> keyframesW = null) {
            clip.SetCurve(path, typeof(Transform), $"{propertyBase}.x", new AnimationCurve(keyframesX.ToArray()));
            clip.SetCurve(path, typeof(Transform), $"{propertyBase}.y", new AnimationCurve(keyframesY.ToArray()));
            clip.SetCurve(path, typeof(Transform), $"{propertyBase}.z", new AnimationCurve(keyframesZ.ToArray()));
            if (keyframesW != null) {
                clip.SetCurve(path, typeof(Transform), $"{propertyBase}.w", new AnimationCurve(keyframesW.ToArray()));
            }
        }
        private void TransformAndSaveAnimation() {
            Debug.Log($"Saving {_animationClip.name} with name: {_newClipName}");
            AnimationClip newClip = new AnimationClip();
            List<Keyframe> keyframesX = new List<Keyframe>(), keyframesY = new List<Keyframe>(), keyframesZ = new List<Keyframe>();
            List<Keyframe> keyframesRotX = new List<Keyframe>(), keyframesRotY = new List<Keyframe>(), keyframesRotZ = new List<Keyframe>(), keyframesRotW = new List<Keyframe>();

            float startTime = 0f;
            float endTime = _animationClip.length;
            float frameRate = 1 / _animationClip.frameRate;

            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(_animationClip);
            foreach (EditorCurveBinding binding in curveBindings) {
                // Obtener la curva asociada a cada binding
                AnimationCurve curve = AnimationUtility.GetEditorCurve(_animationClip, binding);
                for (float t = startTime; t < endTime + frameRate; t += frameRate) {
                    Vector3 bonePosition = Utils.EvaluateCurveVector3(t, _animationClip, binding.path, binding.propertyName);
                    BoneData originalBone = _armatures[1].IKRig.FindConstrainByPath(binding.path);
                    if (!_armatures[1].IKRig.TryGetRigConstrainList(originalBone, out List<IRigConstraint> rigConstraints))
                        break;
                    Vector3 globalPosition = Utils.CalculatePositionFromProportions(_armatures[1], _armatures[0], bonePosition, rigConstraints[0]);
                    keyframesX.Add(new Keyframe(t, globalPosition.x));
                    keyframesY.Add(new Keyframe(t, globalPosition.y));
                    keyframesZ.Add(new Keyframe(t, globalPosition.z));
                }
            }
            
            //Vector3 localPosition = Utils.EvaluateCurveVector3(time, clip, path, "m_LocalPosition");
            /*
            foreach (var linkedConstrain in newlinkedConstrains) {
                List<string> hierarchyPath = GetHierarchyPath(linkedConstrain.Key);
                
                float startTime = 0f;
                float endTime = _animationClip.length;
                float frameRate = 1 / _animationClip.frameRate;

                for (float t = startTime; t < endTime + frameRate; t += frameRate) {
                    Vector3 globalPosition = Utils.CalculateGlobalPositionAtTime(t, hierarchyPath, _animationClip);

                    Quaternion globalRotation = Utils.CalculateRotationAtTime(t, linkedConstrain.Key, _animationClip);
                }
            }
            */
            string json = JsonUtility.ToJson(newClip);
            var path = $"Assets/IKAnimations/{_newClipName}.ikanim";
            File.WriteAllText(path, json);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            PersistData();
        }
        
        private void TransformAndSaveAnimationClip() {
            Debug.Log($"Saving {_animationClip.name} with name: {_newClipName}");
            List<KeyValuePair<string, Transform>> newlinkedConstrains = new List<KeyValuePair<string, Transform>>();
            foreach (string animatedObject in _animatedObjects.Keys) {
                if (_animatedObjects[animatedObject] != null) {
                    newlinkedConstrains.Add(new KeyValuePair<string, Transform>(animatedObject, _animatedObjects[animatedObject]));
                }
            }
            AnimationClip newClip = new AnimationClip();
            foreach (var linkedConstrain in newlinkedConstrains) {
                List<string> hierarchyPath = Utils.GetHierarchyPath(linkedConstrain.Key);
                List<Keyframe> keyframesX = new List<Keyframe>(), keyframesY = new List<Keyframe>(), keyframesZ = new List<Keyframe>();
                List<Keyframe> keyframesRotX = new List<Keyframe>(), keyframesRotY = new List<Keyframe>(), keyframesRotZ = new List<Keyframe>(), keyframesRotW = new List<Keyframe>();

                float startTime = 0f;
                float endTime = _animationClip.length;
                float frameRate = 1 / _animationClip.frameRate;

                for (float t = startTime; t < endTime + frameRate; t += frameRate) {
                    Vector3 globalPosition = Utils.CalculateGlobalPositionAtTime(t, hierarchyPath, _animationClip);
                    keyframesX.Add(new Keyframe(t, globalPosition.x));
                    keyframesY.Add(new Keyframe(t, globalPosition.y));
                    keyframesZ.Add(new Keyframe(t, globalPosition.z));

                    Quaternion globalRotation = Utils.CalculateRotationAtTime(t, linkedConstrain.Key, _animationClip);
                    keyframesRotX.Add(new Keyframe(t, globalRotation.x));
                    keyframesRotY.Add(new Keyframe(t, globalRotation.y));
                    keyframesRotZ.Add(new Keyframe(t, globalRotation.z));
                    keyframesRotW.Add(new Keyframe(t, globalRotation.w));
                }

                string relativePath = AnimationUtility.CalculateTransformPath(linkedConstrain.Value, _ikRigInfo.RigParent);
                ApplyCurveToClip(newClip, relativePath, "m_LocalPosition", keyframesX, keyframesY, keyframesZ);
                ApplyCurveToClip(newClip, relativePath, "m_LocalRotation", keyframesRotX, keyframesRotY, keyframesRotZ, keyframesRotW);
            }
            AssetDatabase.CreateAsset(newClip, $"Assets/IKAnimations/{_newClipName}.anim");
            AssetDatabase.SaveAssets();
            PersistData();
        }
    }
}