using System.Collections.Generic;
using UnityEngine;

namespace Structures {
    [CreateAssetMenu(fileName = "IKRigSetup", menuName = "Configurations/IKRigSetup")]
    public class IKRigSetup : ScriptableObject {
        private AnimationClip _animationClip;
        private IKRigInfo _ikRigInfo;
        private List<DataPairStructures> _foldouts = new List<DataPairStructures>();
        private List<StringTransformPair> _animatedObjects = new List<StringTransformPair>();
        private List<AnimationClip> _clips = new List<AnimationClip>();
        private GameObject _savedModel;
        public List<DataPairStructures> Foldouts
        {
            get => _foldouts;
            set => _foldouts = value;
        }
        public List<StringTransformPair> AnimatedObjects
        {
            get => _animatedObjects;
            set => _animatedObjects = value;
        }
        public GameObject SavedModel
        {
            get => _savedModel;
            set => _savedModel = value;
        }
        private string _lastSelectedDirectory = "";
        private string _newClipName = "";
        public string NewClipName
        {
            get => _newClipName;
            set => _newClipName = value;
        }
        public string LastSelectedDirectory
        {
            get => _lastSelectedDirectory;
            set => _lastSelectedDirectory = value;
        }
        public List<AnimationClip> Clips
        {
            get => _clips;
            set => _clips = value;
        }
        public IKRigInfo IKRigInfo
        {
            get => _ikRigInfo;
            set => _ikRigInfo = value;
        }
        public AnimationClip AnimationClip
        {
            get => _animationClip;
            set => _animationClip = value;
        }
    }
}