using System.Collections.Generic;
using UnityEngine;

namespace Lightning
{
    public class LightningRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Material _boltMaterial;

        [Header("Settings")]
        [SerializeField] private int _poolSize = 32;
        [SerializeField] private float _mainBoltWidth = 0.08f;
        [SerializeField] private float _branchWidthMultiplier = 0.5f;

        private readonly List<LineRenderer> _pool = new();
        private int _activeCount;
        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject($"LightningLine_{i}");
                go.transform.SetParent(transform);
                var lr = go.AddComponent<LineRenderer>();
                lr.material = _boltMaterial;
                lr.useWorldSpace = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.gameObject.SetActive(false);
                _pool.Add(lr);
            }
            _propertyBlock = new MaterialPropertyBlock();
        }

        public void RenderStrike(LightningStrike strike)
        {
            ClearAll();

            for (int i = 0; i < strike.Branches.Count && i < _poolSize; i++)
            {
                var branch = strike.Branches[i];
                var lr = _pool[i];

                float width = branch.Depth == 0
                    ? _mainBoltWidth
                    : _mainBoltWidth * _branchWidthMultiplier / branch.Depth;

                lr.positionCount = branch.Points.Count;
                lr.SetPositions(branch.Points.ToArray());
                lr.startWidth = width;
                lr.endWidth = width * 0.5f;
                lr.gameObject.SetActive(true);
                _activeCount++;
            }
        }

        public void SetBranchAlpha(int branchIndex, float alpha)
        {
            if (branchIndex < 0 || branchIndex >= _activeCount) return;
            var lr = _pool[branchIndex];
            _propertyBlock.SetColor("_BaseColor", new Color(8f, 8f, 8f, alpha));
            lr.SetPropertyBlock(_propertyBlock);
        }

        public void ClearAll()
        {
            foreach (var lr in _pool)
                lr.gameObject.SetActive(false);
            _activeCount = 0;
        }

        public int ActiveBranchCount => _activeCount;
    }
}
