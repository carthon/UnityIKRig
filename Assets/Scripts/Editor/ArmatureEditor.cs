using RigAnimationSystem;
using UnityEditor;
using UnityEngine;

namespace Editor {
    [CustomEditor(typeof(Armature))]
    public class ArmatureEditor : UnityEditor.Editor {
        private Armature _armature;
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            _armature = (Armature) target;
            if (GUILayout.Button("Generate Armature")) {
                _armature.CreateArmature();
            }
            if (GUILayout.Button("Create IKRig")) {
                _armature.CreateIKRig();
            }
            if (GUILayout.Button("Reset IKRig")) {
                _armature.ResetIKRig();
            }
        }
    }
}