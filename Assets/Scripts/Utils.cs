using System.Collections.Generic;
using System.Linq;
using RigAnimationSystem;
using RigAnimationSystem.ArmatureBuilderPackage;
using Structures;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public abstract class Utils {
    public static T LoadScriptableObject<T>(string assetPath) where T : ScriptableObject{
        ScriptableObject loadedObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (loadedObject != null) {
            Debug.Log($"{typeof(T).Name} loaded successfully.");
            if (loadedObject.GetType() == typeof(T)) {
                return loadedObject as T;
            }
        }
        else {
            Debug.Log($"Failed to load {typeof(T).Name}. Creating a new one");
            T ikRigSetupData = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(ikRigSetupData, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return ikRigSetupData;
        }
        return null;
    }
    public static Vector3 EvaluateCurveVector3(float time, AnimationClip clip, string path, string property)
    {
        float x = EvaluateCurve(time, clip, path, property + ".x");
        float y = EvaluateCurve(time, clip, path, property + ".y");
        float z = EvaluateCurve(time, clip, path, property + ".z");
        return new Vector3(x, y, z);
    }
    public static Quaternion EvaluateCurveQuaternion(float time, AnimationClip clip, string path, string property)
    {
        float x = EvaluateCurve(time, clip, path, property + ".x");
        float y = EvaluateCurve(time, clip, path, property + ".y");
        float z = EvaluateCurve(time, clip, path, property + ".z");
        float w = EvaluateCurve(time, clip, path, property + ".w");
        return new Quaternion(x, y, z, w);
    }
    public static float EvaluateCurve(float time, AnimationClip clip, string path, string propertyName)
    {
        var binding = new EditorCurveBinding { type = typeof(Transform), path = path, propertyName = propertyName };
        var curve = AnimationUtility.GetEditorCurve(clip, binding);
        return curve?.Evaluate(time) ?? 0f;
    }
    public static Vector3 CalculatePositionFromProportions(Armature armatureOriginal, Armature armatureRetargeted,Vector3 position, IRigConstraint constrain) {
        switch (constrain) {
            case TwoBoneIKConstraint casted:
                List<string> pathToRoot = GetHierarchyPath(AnimationUtility.CalculateTransformPath(casted.data.root, armatureOriginal.Hips.boneRef));
                float totalBoneProportion = 1f;
                for (int i = 0; i < pathToRoot.Count; i++) {
                    BoneData originalBone = armatureOriginal.FindBoneByRefPath(pathToRoot[i]);
                    BoneData retargetedBone = armatureRetargeted.FindBone(
                        originalBone.bodyPart, originalBone.bodyPartSide, originalBone.indexFromRoot, ArmatureUtils.IsBoneTip(originalBone.boneName));
                    float proportion = retargetedBone.length / originalBone.length;
                    totalBoneProportion *= proportion;
                }
                return position * totalBoneProportion;
                break;
            default:
            break;
        }
        return Vector3.zero;
    }
    public static List<string> GetHierarchyPath(string fullPath) {
        List<string> parts = new List<string>();
        var splits = fullPath.Split('/').ToList();
        //splits.RemoveAt(0);
        string currentPath = "";
        foreach (string split in splits)
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? split : currentPath + "/" + split;
            parts.Add(currentPath);
        }
        return parts;
    }
    public static Vector3 CalculateGlobalPositionAtTime(float time, List<string> hierarchyPath, AnimationClip clip)
    {
        Vector3 globalPosition = Vector3.zero;
        Quaternion globalRotation = Quaternion.identity;

        foreach (string path in hierarchyPath) {
            Vector3 localPosition = Utils.EvaluateCurveVector3(time, clip, path, "m_LocalPosition");
            Quaternion localRotation = Utils.EvaluateCurveQuaternion(time, clip, path, "m_LocalRotation");

            // Aplicar la rotación y luego la traslación
            globalPosition = globalRotation * localPosition + globalPosition;
            globalRotation *= localRotation;
        }

        return globalPosition;
    }

    public static Quaternion CalculateRotationAtTime(float time, string path, AnimationClip clip) {
        Quaternion localRotation = Utils.EvaluateCurveQuaternion(time, clip, path, "m_LocalRotation");
        return localRotation;
    }
    public static float GetBoneLength(Transform transform) {
        float length = 0f;
        if (transform.childCount > 0) {
            length = Vector3.Distance(transform.GetChild(0).position, transform.position);
        }
        return length;
    }
}