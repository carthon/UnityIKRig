using System.Collections.Generic;
using Structures;

namespace RigAnimationSystem.ArmatureBuilderPackage {
    public class ArmatureBuilder {
        private IArmatureStrategy _strategy;

        public void SetStrategy(IArmatureStrategy strategy) {
            _strategy = strategy;
        }
        public Dictionary<string, BoneData> Build() {
            return _strategy.Execute();
        }
    }
}