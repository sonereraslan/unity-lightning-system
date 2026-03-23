using System.Collections.Generic;
using UnityEngine;

namespace Lightning
{
    public class LightningStrike
    {
        public List<LightningBranch> Branches { get; }
        public Vector3 Origin { get; }
        public Vector3 Target { get; }

        public LightningStrike(Vector3 origin, Vector3 target, List<LightningBranch> branches)
        {
            Origin = origin;
            Target = target;
            Branches = branches;
        }
    }
}
