using System.Collections.Generic;
using UnityEngine;

namespace RigAnimationSystem.IKRigBuilderPackage {

    public class IKRigBuilder {
        private IRigBuilderStrategy _strategy;
        
        public void SetStrategy(IRigBuilderStrategy strategy) {
            _strategy = strategy;
        }
        public List<GameObject> Build() {
            return _strategy.Execute();
        }
    }
}