using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

namespace VATyakov.Dev
{
    public sealed class VatRotDecodeCheck : MonoBehaviour
    {
        private const int Width = 256;
        private const int Height = 5;

        [SerializeField]
        private Shader _shader;

        private Material _material;
        private Texture2D _texture;
        private RenderTexture _result;
        private int _errors = -1;

        private void Start()
        {
            _texture = TestTexture();
            _material = new Material(_shader) { hideFlags = HideFlags.DontSave };
            _material.SetTexture("_VatRotTex", _texture);
            _result = new RenderTexture(Width, Height, 0, GraphicsFormat.R8G8B8A8_UNorm) { filterMode = FilterMode.Point };
            Graphics.Blit(null, _result, _material);
            _errors = CountErrors(_result);
            Debug.Log($"VAT RGBA8 decode: {(_errors == 0 ? "OK" : _errors + " errors")} on {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}");
        }

        private void OnDestroy()
        {
            Destroy(_material);
            Destroy(_texture);
            if (_result != null)
            {
                _result.Release();
            }

            Destroy(_result);
        }

        private static Texture2D TestTexture()
        {
            var texture = new Texture2D(Width, Height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var texels = new Color32[Width * Height];
            for (var x = 0; x < Width; x++)
            {
                texels[x] = new Color32((byte)x, (byte)(255 - x), (byte)((x * 37 + 11) & 255), (byte)((x * 101 + 7) & 255));
            }

            for (var i = 0; i < 1024; i++)
            {
                texels[Width + i] = Pack(i, (i * 389 + 17) & 1023, 1023 - i, i & 3);
            }

            texture.SetPixelData(texels, 0);
            texture.Apply(false, true);
            return texture;
        }

        private static Color32 Pack(int a, int b, int c, int index)
        {
            return new Color32((byte)(a & 255), (byte)(b & 255), (byte)(c & 255), (byte)(index << 6 | (c >> 8) << 4 | (b >> 8) << 2 | a >> 8));
        }

        private static int CountErrors(RenderTexture result)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = result;
            var read = new Texture2D(Width, Height, TextureFormat.RGBA32, false, true);
            read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            var errors = 0;
            foreach (var pixel in read.GetPixels32())
            {
                if (pixel.r != 0 || pixel.g != 255)
                {
                    errors++;
                }
            }

            Destroy(read);
            return errors;
        }

        private void OnGUI()
        {
            var scale = Mathf.Max(1f, Screen.dpi / 160f);
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * scale) };
            var button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20 * scale) };
            var margin = 12 * scale;
            if (GUI.Button(new Rect(margin, margin, 160 * scale, 56 * scale), "Compare", button))
            {
                SceneManager.LoadScene("compare");
            }

            GUI.Label(new Rect(margin, margin + 64 * scale, Screen.width - 2 * margin, 40 * scale), Status(), style);
            GUI.Label(new Rect(margin, margin + 104 * scale, Screen.width - 2 * margin, 40 * scale),
                $"{SystemInfo.graphicsDeviceType} · {SystemInfo.graphicsDeviceName}", style);
            if (_result != null)
            {
                GUI.DrawTexture(new Rect(margin, margin + 152 * scale, Screen.width - 2 * margin, 200 * scale), _result, ScaleMode.StretchToFill);
            }
        }

        private string Status()
        {
            return _errors < 0 ? "RGBA8: ..." : _errors == 0
                ? $"RGBA8: OK, {Width * Height} texels exact"
                : $"RGBA8: {_errors} of {Width * Height} texels wrong";
        }
    }
}
