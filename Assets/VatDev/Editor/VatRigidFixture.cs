using UnityEngine;

namespace VATyakov.Dev
{
    internal static class VatRigidFixture
    {
        internal const float Fps = 120f;
        internal const int Frames = 61;
        internal const int LateFrame = 30;

        private const string Path = "Packages/com.vatyakov/Tests/Editor/Fixtures/vat_rigid.abc";
        private const float Size = 0.5f;
        private const float SlideSpeed = 2f;
        private const float SlideTurnRate = 90f;
        private const float LateHeight = 2f;
        private const float Gravity = 9.8f;
        private const float SpinTurnRate = 3600f;
        private static readonly Vector3 _slideAxis = new(1f, 1f, 0f);
        private static readonly Vector3 _lateStart = new(0f, LateHeight, 1f);
        private static readonly Vector3 _spinCenter = new(3f, 0f, 0f);

        public static void Regenerate()
        {
            var root = new GameObject("vat_rigid");
            var mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var slide = Piece(root, "slide", mesh);
            var late = Piece(root, "late", mesh);
            var spin = Piece(root, "spin", mesh);
            VatRigidRecorder.Record(Path, root, Frames, Fps, frame =>
            {
                var time = frame / Fps;
                slide.SetLocalPositionAndRotation(new Vector3(SlideSpeed * time, 0f, 0f), Quaternion.AngleAxis(SlideTurnRate * time, _slideAxis));
                late.gameObject.SetActive(frame >= LateFrame);
                late.localPosition = _lateStart + 0.5f * Gravity * time * time * Vector3.down;
                spin.SetLocalPositionAndRotation(_spinCenter, Quaternion.AngleAxis(SpinTurnRate * time, Vector3.up));
            });
        }

        private static Transform Piece(GameObject root, string name, Mesh mesh)
        {
            var piece = VatRigidRecorder.Piece(root.transform, name, mesh, null).transform;
            piece.localScale = Vector3.one * Size;
            return piece;
        }
    }
}
