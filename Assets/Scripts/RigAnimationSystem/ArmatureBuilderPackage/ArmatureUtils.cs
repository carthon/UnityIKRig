using System.Collections.Generic;
using System.Text.RegularExpressions;
using Enums;
using Structures;
using UnityEngine;

namespace RigAnimationSystem.ArmatureBuilderPackage {
    public static class ArmatureUtils {
        public static string BoneNameFormatter(BodyPart bodyPart, BodyPartSide bodyPartSide, int index, bool isTip) {
            return $"{bodyPart.ToString()}{(!isTip ? index.ToString() : "")}{bodyPartSide}";
        }
        public static bool IsBoneTip(string boneName) => new Regex(@"\d+").IsMatch(boneName);
        public static void PrintChilds(int padding, Transform startingTransform) {
            int i = 0;
            int childCount = startingTransform.childCount;
            Debug.Log(" ".PadLeft(padding) + startingTransform.name + ":" + childCount);
            for (int j = 0; j < childCount; j++) {
                Transform child = startingTransform.GetChild(j);
                if (child.childCount is 1 or 3)
                    PrintChilds(padding + i, child);
            }
        }
        public static Transform GetEndChild(int index, Transform startChild, out int finalChildIndex) {
            int childCount = startChild.childCount;
            finalChildIndex = index;
            if (childCount > 0) {
                Transform child = startChild.GetChild(0);
                if (child.childCount is 0 or >= 2) {
                    finalChildIndex++;
                    return child;
                }
                index++;
                //Debug.Log($"Getting child {child.name} and index {index}");
                return GetEndChild(index, child, out finalChildIndex);
            }
            return startChild;
        }
        public static BoneData FindFirstBoneOfType(Armature armature, BoneData tip) {
            int i = tip.indexFromRoot - 1;
            BoneData lastBone = new BoneData();
            bool found = false;
            
            while (!found) {
                BoneData bone = armature.FindBone(tip.bodyPart, tip.bodyPartSide, i, false);
                found = bone.Equals(new BoneData());
                if (!found)
                    lastBone = bone;
                i--;
            }
            return lastBone;
        }
        public static Transform FindHips(int depth, int index, Transform modelTransform, out int foundIndex) {
            int childCount = modelTransform.childCount;
            foundIndex = index;
            for (int i = 0; i < childCount; i++) {
                Transform child = modelTransform.GetChild(i);
                if (child.childCount is 1 or 3) {
                    if (depth > 0 && child.childCount is 3) {
                        return child;
                    }
                    foundIndex++;
                    return FindHips(depth + 1, foundIndex, child, out foundIndex);
                }
                foundIndex++;
            }
            return modelTransform;
        }
    }
}