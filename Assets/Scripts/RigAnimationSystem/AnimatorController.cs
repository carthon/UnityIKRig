using System;
using System.Collections;
using Structures;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace RigAnimationSystem {
    public class AnimatorController : MonoBehaviour {
        public float animationSpeed = 1f;
        [SerializeField]
        private AnimationClip[] clips;
        private IKRigInfo _rigInfo;
        private AnimationClip _playingClip;
        private float _time = 0;
        [SerializeField]
        private bool isPlaying;
        private void Start() {
            Rig[] rigs = GetComponentsInChildren<Rig>();
            _rigInfo = Armature.GetRigInfo(transform, rigs);
            if (clips.Length > 0) {
                _playingClip = clips[0];
            }
        }
        private void Update() {
            if (_playingClip is null)
                return;
            if (isPlaying && _time < _playingClip.length) {
                _time += Time.deltaTime * animationSpeed;
            } else if (_playingClip.isLooping) _time = 0;
            else {
                isPlaying = false;
                return;
            }
            ApplyAnimation(_playingClip, _time);
        }
        /*
        IEnumerator ApplyAnimation(AnimationClip clip, float time, EditorCurveBinding binding)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (binding.propertyName.Contains("m_LocalPosition.")) {
                if(_rigInfo.PathConstrains.TryGetValue(binding.path, out Transform tr))
                    tr.position = Utils.EvaluateCurveVector3(time, clip, binding.path, binding.propertyName);
            }
            if (binding.propertyName.Contains("m_LocalRotation.")) {
                if(_rigInfo.PathConstrains.TryGetValue(binding.path, out Transform tr))
                    tr.rotation = Utils.EvaluateCurveQuaternion(time, clip, binding.path, binding.propertyName);
            }
            yield return curve;
        }
        */
        private void ApplyAnimation(AnimationClip clip, float time) {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)) {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

                if (binding.propertyName.Contains("m_LocalPosition")) {
                    if (_rigInfo.PathConstrains.TryGetValue(binding.path, out Transform tr)) {
                        Vector3 position = tr.localPosition;

                        if (binding.propertyName.EndsWith(".x"))
                            position.x = curve.Evaluate(time);
                        if (binding.propertyName.EndsWith(".y"))
                            position.y = curve.Evaluate(time);
                        if (binding.propertyName.EndsWith(".z"))
                            position.z = curve.Evaluate(time);

                        tr.localPosition = position;
                    }
                }
                else if (binding.propertyName.Contains("m_LocalRotation")) {
                    if (_rigInfo.PathConstrains.TryGetValue(binding.path, out Transform tr)) {
                        Quaternion rotation = tr.localRotation;

                        if (binding.propertyName.EndsWith(".x"))
                            rotation.x = curve.Evaluate(time);
                        if (binding.propertyName.EndsWith(".y"))
                            rotation.y = curve.Evaluate(time);
                        if (binding.propertyName.EndsWith(".z"))
                            rotation.z = curve.Evaluate(time);
                        if (binding.propertyName.EndsWith(".w"))
                            rotation.w = curve.Evaluate(time);

                        tr.localRotation = rotation;
                    }
                }
            }
        }
    }
}