using System.Collections.Generic;
using UnityEngine;

namespace Lightning
{
    public class LightningBranch
    {
        public List<Vector3> Points { get; }
        public float Width { get; }
        public int Depth { get; }  // 0 = main bolt, 1+ = sub-branch

        public LightningBranch(List<Vector3> points, float width, int depth)
        {
            Points = points;
            Width = width;
            Depth = depth;
        }
    }
}
