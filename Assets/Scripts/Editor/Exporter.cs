using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RigAnimationSystem;
using Structures;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Editor {
    public class ExporterUtils : MonoBehaviour
    {
        [MenuItem("Export/Export Selected to OBJ")]
        static void ExportSelectedToObj()
        {
            StringBuilder objStringBuilder = new StringBuilder();
            objStringBuilder.AppendLine("# Exported from Unity");

            // Transform data
            Transform[] transforms = Selection.GetTransforms(SelectionMode.Deep | SelectionMode.ExcludePrefab);
            int vertexOffset = 1;  // OBJ format starts counting from 1

            foreach (Transform t in transforms)
            {
                Debug.Log($"Extracting data from {t.name}");
                MeshFilter mf = t.GetComponent<MeshFilter>();
                if (mf)
                {
                    Mesh m = mf.sharedMesh;
                    if (m)
                    {
                        objStringBuilder.AppendLine($"o {t.gameObject.name}");

                        // Vertices
                        foreach (Vector3 v in m.vertices)
                        {
                            Vector3 worldV = t.TransformPoint(v);
                            objStringBuilder.AppendLine($"v {worldV.x} {worldV.y} {worldV.z}");
                        }

                        // Normals
                        foreach (Vector3 n in m.normals)
                        {
                            Vector3 worldN = t.TransformDirection(n);
                            objStringBuilder.AppendLine($"vn {worldN.x} {worldN.y} {worldN.z}");
                        }

                        // UVs
                        foreach (Vector3 uv in m.uv)
                        {
                            objStringBuilder.AppendLine($"vt {uv.x} {uv.y}");
                        }

                        // Faces
                        for (int i = 0; i < m.triangles.Length; i += 3)
                        {
                            int[] triangle = new int[]
                            {
                                m.triangles[i] + vertexOffset,
                                m.triangles[i + 1] + vertexOffset,
                                m.triangles[i + 2] + vertexOffset
                            };
                            objStringBuilder.AppendLine($"f {triangle[0]}/{triangle[0]}/{triangle[0]} {triangle[1]}/{triangle[1]}/{triangle[1]} {triangle[2]}/{triangle[2]}/{triangle[2]}");
                        }

                        vertexOffset += m.vertexCount;
                    }
                }
            }

            // Writing to file
            string filePath = EditorUtility.SaveFilePanel("Save OBJ File", "", "ExportedModel.obj", "obj");
            if (!string.IsNullOrEmpty(filePath))
            {
                File.WriteAllText(filePath, objStringBuilder.ToString());
            }
        }

        [MenuItem("Export/IKRig Tool")]
        public static void SelectAndProcessAnimation()
        {
            IKRigExtractor.ShowWindow();
        }
        //[MenuItem("Export/Create IK Target Animation")]
        public static void CreateAnimation() {
            GameObject target = Selection.activeGameObject;
            if (target == null)
            {
                Debug.LogError("No GameObject selected. Please select your IK Target.");
                return;
            }

            AnimationClip clip = new AnimationClip();
            clip.legacy = false;
            Vector3 position = target.transform.position;

            // Animate the target along the y-axis
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0, position.y);
            keys[1] = new Keyframe(1, position.y + 2.0f);  // Move 2 units upwards in 1 second
            AnimationCurve curve = new AnimationCurve(keys);

            string path = AnimationUtility.CalculateTransformPath(target.transform, target.transform.root);
            clip.SetCurve(path, typeof(Transform), "localPosition.y", curve);

            AssetDatabase.CreateAsset(clip, "Assets/IKAnimations/TargetMove.anim");
            AssetDatabase.SaveAssets();
        }
    }
}