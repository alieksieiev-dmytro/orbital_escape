using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OrbitalEscape.UI
{
    [RequireComponent(typeof(RawImage))]
    public sealed class PauseBlurBackground : MonoBehaviour
    {
        private const string BlurShaderName = "Hidden/OrbitalEscape/UI/PauseSnapshotBlur";
        private static readonly int DirectionId = Shader.PropertyToID("_Direction");

        [SerializeField, Min(1)] private int _downsampleFactor = 4;
        [SerializeField, Min(1)] private int _blurIterations = 3;
        [SerializeField, Min(0.1f)] private float _blurRadius = 2f;

        private RawImage _rawImage;
        private Material _blurMaterial;
        private Texture2D _screenTexture;
        private RenderTexture _source;
        private RenderTexture _ping;
        private RenderTexture _pong;
        private int _screenWidth;
        private int _screenHeight;

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
        }

        private void OnDisable()
        {
            Release();
        }

        private void OnDestroy()
        {
            Release();
        }

        public IEnumerator CaptureAsync()
        {
            yield return new WaitForEndOfFrame();

            if (!EnsureReady())
            {
                yield break;
            }

            int width = Screen.width;
            int height = Screen.height;

            if (width <= 0 || height <= 0)
            {
                yield break;
            }

            EnsureBuffers(width, height);

            _screenTexture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            _screenTexture.Apply(false, false);

            Graphics.Blit(_screenTexture, _source);

            RenderTexture current = _source;

            for (int i = 0; i < _blurIterations; i++)
            {
                _blurMaterial.SetVector(DirectionId, new Vector4(_blurRadius, 0f, 0f, 0f));
                Graphics.Blit(current, _ping, _blurMaterial);

                _blurMaterial.SetVector(DirectionId, new Vector4(0f, _blurRadius, 0f, 0f));
                Graphics.Blit(_ping, _pong, _blurMaterial);

                current = _pong;
            }

            _rawImage.texture = current;
        }

        public void Release()
        {
            ReleaseTextures();
            DestroyRuntimeObject(_blurMaterial);
            _blurMaterial = null;
        }

        private void ReleaseTextures()
        {
            if (_rawImage != null)
            {
                _rawImage.texture = null;
            }

            ReleaseRenderTexture(ref _source);
            ReleaseRenderTexture(ref _ping);
            ReleaseRenderTexture(ref _pong);
            DestroyRuntimeObject(_screenTexture);
            _screenTexture = null;
            _screenWidth = 0;
            _screenHeight = 0;
        }

        private bool EnsureReady()
        {
            if (_rawImage == null)
            {
                _rawImage = GetComponent<RawImage>();
            }

            if (_blurMaterial != null)
            {
                return true;
            }

            Shader blurShader = Shader.Find(BlurShaderName);

            if (blurShader == null)
            {
                Debug.LogWarning($"Shader '{BlurShaderName}' was not found. Pause background blur will be skipped.", this);
                return false;
            }

            _blurMaterial = new Material(blurShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            return true;
        }

        private void EnsureBuffers(int width, int height)
        {
            if (_screenTexture != null && _screenWidth == width && _screenHeight == height)
            {
                return;
            }

            ReleaseTextures();

            _screenWidth = width;
            _screenHeight = height;

            _screenTexture = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            int blurWidth = Mathf.Max(1, width / _downsampleFactor);
            int blurHeight = Mathf.Max(1, height / _downsampleFactor);

            _source = CreateRenderTexture(blurWidth, blurHeight);
            _ping = CreateRenderTexture(blurWidth, blurHeight);
            _pong = CreateRenderTexture(blurWidth, blurHeight);
        }

        private RenderTexture CreateRenderTexture(int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            renderTexture.Create();
            return renderTexture;
        }

        private void ReleaseRenderTexture(ref RenderTexture renderTexture)
        {
            if (renderTexture == null)
            {
                return;
            }

            renderTexture.Release();
            DestroyRuntimeObject(renderTexture);
            renderTexture = null;
        }

        private void DestroyRuntimeObject(Object runtimeObject)
        {
            if (runtimeObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeObject);
                return;
            }

            DestroyImmediate(runtimeObject);
        }
    }
}
