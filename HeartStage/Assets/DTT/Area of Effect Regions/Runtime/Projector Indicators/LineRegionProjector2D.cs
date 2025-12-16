using DTT.AreaOfEffectRegions.Shaders;
using DTT.Utils.Extensions;
using UnityEngine;

namespace DTT.AreaOfEffectRegions
{
    [ExecuteAlways]
    public class LineRegionProjector2D : MonoBehaviour
    {
        [Header("Projectors")]
        [SerializeField] private Projector _headProjector;
        [SerializeField] private Projector _bodyProjector;

        [Header("Shape")]
        [SerializeField] private float _length = 1f;
        [SerializeField] private float _width = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float _fillProgress = 1f;
        [SerializeField] private Origin _fillOrigin = Origin.BOTTOM;

        [Header("Colors")]
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private Color _fillColor = Color.white;

        [Header("2D Settings")]
        [SerializeField] private float _zDepth = -0.1f;

        private const float ARROW_OFFSET = 2.9835f;
        public float WIDTH_VISUAL_FIX = 1.7f;

        #region Properties
        public float Length
        {
            get => _length;
            set => _length = Mathf.Max(0f, value);
        }

        public float Width
        {
            get => _width;
            set => _width = Mathf.Max(0f, value);
        }

        public float FillProgress
        {
            get => _fillProgress;
            set => _fillProgress = Mathf.Clamp01(value);
        }

        public float Angle
        {
            get => transform.eulerAngles.z;
            set => transform.localRotation = Quaternion.Euler(0f, 0f, value);
        }
        #endregion

        private static readonly int ColorID = Shader.PropertyToID("_Color");
        private static readonly int FillColorID = Shader.PropertyToID("_FillColor");
        private static readonly int FillProgressID = Shader.PropertyToID("_FillProgress");
        private static readonly int OriginID = Shader.PropertyToID("_Origin");

        private void Start()
        {
            CloneMaterials();
            UpdateProjectors();
        }

        private void OnValidate()
        {
            UpdateProjectors();
        }

        private void CloneMaterials()
        {
            if (_headProjector != null)
                _headProjector.material = new Material(_headProjector.material);

            if (_bodyProjector != null)
                _bodyProjector.material = new Material(_bodyProjector.material);
        }

        public void UpdateProjectors()
        {
            UpdateBody();
            UpdateHead();
        }

        private void UpdateBody()
        {
            if (_bodyProjector == null)
                return;

            float height = _length;
            float width = _width;
            _bodyProjector.orthographicSize = height * 0.5f;
            _bodyProjector.aspectRatio = (width * WIDTH_VISUAL_FIX) / height;
            _bodyProjector.transform.localPosition = new Vector3(0f, height * 0.5f, _zDepth);

            ApplyMaterial(_bodyProjector.material);
        }

        private void UpdateHead()
        {
            if (_headProjector == null)
                return;

            float height = _length;
            float width = _width;
            _headProjector.orthographicSize = height * 0.5f;
            _headProjector.aspectRatio = (width * WIDTH_VISUAL_FIX) / height;
            _headProjector.transform.localPosition = new Vector3(0f, height, _zDepth);

            ApplyMaterial(_headProjector.material);
        }

        private void ApplyMaterial(Material mat)
        {
            if (mat == null) return;

            mat.SetColor(ColorID, _color);
            mat.SetColor(FillColorID, _fillColor);
            mat.SetInt(OriginID, _fillOrigin.ToInt());
            mat.SetFloat(FillProgressID, _fillProgress);
        }
    }
}