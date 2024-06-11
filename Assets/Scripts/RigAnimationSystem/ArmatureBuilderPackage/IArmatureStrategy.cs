using System.Collections.Generic;
using Structures;

namespace RigAnimationSystem.ArmatureBuilderPackage {
    public interface IArmatureStrategy {
        public Dictionary<string, BoneData> Execute();
    }
}