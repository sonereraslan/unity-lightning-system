using System.Collections.Generic;
using UnityEngine;

namespace Lightning
{
    public class LightningGenerator
    {
        private readonly int _segmentCount;
        private readonly float _maxDeviation;
        private readonly float _branchProbability;
        private readonly int _maxBranchDepth;
        private readonly System.Random _random;
        private readonly float _noiseOffsetX;
        private readonly float _noiseOffsetY;

        public LightningGenerator(
            int segmentCount = 60,
            float maxDeviation = 0.4f,
            float branchProbability = 0.25f,
            int maxBranchDepth = 2,
            int seed = -1)
        {
            _segmentCount = segmentCount;
            _maxDeviation = maxDeviation;
            _branchProbability = Mathf.Clamp01(branchProbability);
            _maxBranchDepth = maxBranchDepth;
            _random = seed >= 0 ? new System.Random(seed) : new System.Random();
            _noiseOffsetX = (float)(_random.NextDouble() * 1000.0);
            _noiseOffsetY = (float)(_random.NextDouble() * 1000.0);
        }

        public LightningStrike Generate(Vector3 origin, Vector3 target)
        {
            var branches = new List<LightningBranch>();
            var mainPoints = GeneratePath(origin, target, _segmentCount);
            branches.Add(new LightningBranch(mainPoints, 1.0f, 0));
            GenerateBranches(mainPoints, branches, depth: 1);
            return new LightningStrike(origin, target, branches);
        }

        private List<Vector3> GeneratePath(Vector3 from, Vector3 to, int segments)
        {
            var points = new List<Vector3>(segments + 1);
            points.Add(from);

            float segmentLength = Vector3.Distance(from, to) / segments;

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 straightPoint = Vector3.Lerp(from, to, t);

                float noiseX = SampleNoise(_noiseOffsetX + i * 0.3f, _noiseOffsetY) * _maxDeviation;
                float noiseZ = SampleNoise(_noiseOffsetY, _noiseOffsetX + i * 0.3f) * _maxDeviation;

                Vector3 offset = new Vector3(noiseX, 0f, noiseZ) * segmentLength;
                points.Add(straightPoint + offset);
            }

            points[points.Count - 1] = to;
            return points;
        }

        private void GenerateBranches(List<Vector3> parentPoints, List<LightningBranch> branches, int depth)
        {
            if (depth > _maxBranchDepth) return;

            for (int i = 1; i < parentPoints.Count - 1; i++)
            {
                if ((float)_random.NextDouble() > _branchProbability) continue;

                Vector3 branchOrigin = parentPoints[i];
                Vector3 parentDir = (parentPoints[i] - parentPoints[i - 1]).normalized;

                Vector3 deviation = new Vector3(
                    (float)(_random.NextDouble() * 2 - 1),
                    (float)(_random.NextDouble() * 2 - 1),
                    (float)(_random.NextDouble() * 2 - 1)
                ).normalized;
                Vector3 branchDir = Vector3.Lerp(parentDir, deviation, 0.5f).normalized;

                float branchLength = Vector3.Distance(parentPoints[i], parentPoints[parentPoints.Count - 1]) * 0.5f;
                Vector3 branchTarget = branchOrigin + branchDir * branchLength;

                int branchSegments = Mathf.Max(5, _segmentCount / (depth + 1));
                var branchPoints = GeneratePath(branchOrigin, branchTarget, branchSegments);

                float width = 1.0f / (depth + 1);
                branches.Add(new LightningBranch(branchPoints, width, depth));

                GenerateBranches(branchPoints, branches, depth + 1);
            }
        }

        private float SampleNoise(float x, float y)
        {
            return (Mathf.PerlinNoise(x, y) - 0.5f) * 2f;
        }
    }
}
