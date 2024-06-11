using System.Collections.Generic;
using UnityEngine;

namespace RigAnimationSystem.IKRigBuilderPackage {
    public interface IRigBuilderStrategy {
        public List<GameObject> Execute();
    }
}