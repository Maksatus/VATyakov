using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

namespace VATyakov.Dev
{
    public sealed class VatRotationDecodeCheck : MonoBehaviour
    {
        private const int Width = 256;
        private const int Height = 5;
        private const int ByteMask = 255;
        private const int FieldMax = 1023;
        private const int FieldCount = FieldMax + 1;
        private const int IndexMask = 3;
        private const int FieldLowBits = 8;
        private const int HighBitsBShift = 2;
        private const int HighBitsCShift = 4;
        private const int IndexShift = 6;
        private const int SecondByteFactor = 37;
        private const int SecondByteOffset = 11;
        private const int ThirdByteFactor = 101;
        private const int ThirdByteOffset = 7;
        private const int FieldFactor = 389;
        private const int FieldOffset = 17;
        private const byte PassGreen = 255;
        private const string CompareScene = "compare";
        private const int LabelFontSize = 22;
        private const float ButtonWidth = 160f;
        private const float LineHeight = 40f;
        private const float StatusTop = 64f;
        private const float DeviceTop = 104f;
        private const float ResultTop = 152f;
        private const float ResultHeight = 200f;

        [SerializeField]
        private Shader _shader;

        private Material _material;
        private Texture2D _texture;
        private RenderTexture _result;
        private int _errors;
        private bool _hasResult;

        private void Start()
        {
            _texture = TestTexture();
            _material = new Material(_shader) { hideFlags = HideFlags.DontSave };
            _material.SetTexture(VatShaderIds.RotationTexture, _texture);
            _result = new RenderTexture(Width, Height, 0, GraphicsFormat.R8G8B8A8_UNorm) { filterMode = FilterMode.Point };
            Graphics.Blit(null, _result, _material);
            _errors = CountErrors(_result);
            _hasResult = true;
            Debug.Log($"VAT {Status()} on {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}");
        }

        private void OnGUI()
        {
            var scale = VatDevGui.Scale();
            var style = VatDevGui.LabelStyle(LabelFontSize, TextAnchor.UpperLeft, scale);
            var button = VatDevGui.ButtonStyle(scale);
            var margin = VatDevGui.Margin * scale;
            var lineWidth = Screen.width - 2f * margin;
            if (GUI.Button(new Rect(margin, margin, ButtonWidth * scale, VatDevGui.ButtonHeight * scale), "Compare", button))
            {
                SceneManager.LoadScene(CompareScene);
            }

            GUI.Label(new Rect(margin, margin + StatusTop * scale, lineWidth, LineHeight * scale), Status(), style);
            GUI.Label(new Rect(margin, margin + DeviceTop * scale, lineWidth, LineHeight * scale),
                $"{SystemInfo.graphicsDeviceType} · {SystemInfo.graphicsDeviceName}", style);
            if (_result != null)
            {
                GUI.DrawTexture(new Rect(margin, margin + ResultTop * scale, lineWidth, ResultHeight * scale), _result, ScaleMode.StretchToFill);
            }
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
            var texture = new Texture2D(Width, Height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var texels = new Color32[Width * Height];
            for (var x = 0; x < Width; x++)
            {
                texels[x] = ByteTexel(x);
            }

            for (var i = 0; i < FieldCount; i++)
            {
                texels[Width + i] = Pack(i, (i * FieldFactor + FieldOffset) & FieldMax, FieldMax - i, i & IndexMask);
            }

            texture.SetPixelData(texels, 0);
            texture.Apply(false, true);
            return texture;
        }

        private static Color32 ByteTexel(int x)
        {
            return new Color32((byte)x, (byte)(ByteMask - x),
                (byte)((x * SecondByteFactor + SecondByteOffset) & ByteMask), (byte)((x * ThirdByteFactor + ThirdByteOffset) & ByteMask));
        }

        private static Color32 Pack(int a, int b, int c, int index)
        {
            var high = index << IndexShift | (c >> FieldLowBits) << HighBitsCShift | (b >> FieldLowBits) << HighBitsBShift | a >> FieldLowBits;
            return new Color32((byte)(a & ByteMask), (byte)(b & ByteMask), (byte)(c & ByteMask), (byte)high);
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
                if (pixel.r != 0 || pixel.g != PassGreen)
                {
                    errors++;
                }
            }

            Destroy(read);
            return errors;
        }

        private string Status()
        {
            if (!_hasResult)
            {
                return "RGBA8: ...";
            }

            return _errors == 0
                ? FormattableString.Invariant($"RGBA8: OK, {Width * Height} texels exact")
                : FormattableString.Invariant($"RGBA8: {_errors} of {Width * Height} texels wrong");
        }
    }
}
