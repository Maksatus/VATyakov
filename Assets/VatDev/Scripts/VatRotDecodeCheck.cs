using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

namespace VATyakov.Dev
{
    /// <summary>
    /// Device check of 1.3 (scene RotDecode): fills an RGBA8 texture with known bytes and smallest-three fields,
    /// decodes it on the GPU with VatCore.hlsl (shader VatRotDecodeTest) and counts the wrong texels.
    /// The packing here follows §1.9 on its own, so it also cross-checks the baker's encoder.
    /// </summary>
    public sealed class VatRotDecodeCheck : MonoBehaviour
    {
        const int Width = 256;
        const int Height = 5;

        [SerializeField] Shader _shader;

        Material _material;
        Texture2D _texture;
        RenderTexture _result;
        int _errors = -1;

        void Start()
        {
            _texture = TestTexture();
            _material = new Material(_shader) { hideFlags = HideFlags.DontSave };
            _material.SetTexture("_VatRotTex", _texture);
            _result = new RenderTexture(Width, Height, 0, GraphicsFormat.R8G8B8A8_UNorm) { filterMode = FilterMode.Point };
            Graphics.Blit(null, _result, _material);
            _errors = CountErrors(_result);
            Debug.Log($"VAT RGBA8 decode: {(_errors == 0 ? "OK" : _errors + " errors")} on {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}");
        }

        void OnDestroy()
        {
            Destroy(_material);
            Destroy(_texture);
            if (_result != null)
                _result.Release();
            Destroy(_result);
        }

        // §1.8: GraphicsFormat constructor, linear, Point, Clamp, no mips — like the baker.
        static Texture2D TestTexture()
        {
            var texture = new Texture2D(Width, Height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var texels = new Color32[Width * Height];
            for (int x = 0; x < Width; x++)
                texels[x] = new Color32((byte)x, (byte)(255 - x), (byte)((x * 37 + 11) & 255), (byte)((x * 101 + 7) & 255));
            for (int i = 0; i < 1024; i++)
                texels[Width + i] = Pack(i, (i * 389 + 17) & 1023, 1023 - i, i & 3);
            texture.SetPixelData(texels, 0);
            texture.Apply(false, true);
            return texture;
        }

        // §1.9: R = n_a & 255, G = n_b & 255, B = n_c & 255, A = idx<<6 | (n_c>>8)<<4 | (n_b>>8)<<2 | (n_a>>8).
        static Color32 Pack(int a, int b, int c, int index) =>
            new Color32((byte)(a & 255), (byte)(b & 255), (byte)(c & 255), (byte)(index << 6 | (c >> 8) << 4 | (b >> 8) << 2 | a >> 8));

        static int CountErrors(RenderTexture result)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = result;
            var read = new Texture2D(Width, Height, TextureFormat.RGBA32, false, true);
            read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            int errors = 0;
            foreach (var pixel in read.GetPixels32())
                if (pixel.r != 0 || pixel.g != 255)
                    errors++;
            Destroy(read);
            return errors;
        }

        void OnGUI()
        {
            float scale = Mathf.Max(1f, Screen.dpi / 160f);
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * scale) };
            var button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20 * scale) };
            float margin = 12 * scale;
            if (GUI.Button(new Rect(margin, margin, 160 * scale, 56 * scale), "Compare", button))
                SceneManager.LoadScene("Compare");
            GUI.Label(new Rect(margin, margin + 64 * scale, Screen.width - 2 * margin, 40 * scale), Status(), style);
            GUI.Label(new Rect(margin, margin + 104 * scale, Screen.width - 2 * margin, 40 * scale),
                $"{SystemInfo.graphicsDeviceType} · {SystemInfo.graphicsDeviceName}", style);
            if (_result != null)
                GUI.DrawTexture(new Rect(margin, margin + 152 * scale, Screen.width - 2 * margin, 200 * scale), _result, ScaleMode.StretchToFill);
        }

        string Status() => _errors < 0 ? "RGBA8: ..." : _errors == 0
            ? $"RGBA8: OK, {Width * Height} texels exact"
            : $"RGBA8: {_errors} of {Width * Height} texels wrong";
    }
}
