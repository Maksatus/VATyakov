using UnityEngine;

namespace VATyakov.Dev
{
    internal static class VatRigidFixture
    {
        internal const float Fps = 120f;
        internal const int Frames = 61;
        internal const int LateFrame = 30;

        private const string Path = "Packages/com.vatyakov/Tests/Editor/Fixtures/vat_rigid.abc";
        private const string DeformPath = "Packages/com.vatyakov/Tests/Editor/Fixtures/vat_rigid_deform.abc";
        private const float Size = 0.5f;
        private const float SlideSpeed = 2f;
        private const float SlideTurnRate = 90f;
        private const float LateHeight = 2f;
        private const float Gravity = 9.8f;
        private const float SpinTurnRate = 3600f;
        private const int Slide = 0;
        private const int Late = 1;
        private const int Spin = 2;

        private static readonly Vector3 _slideAxis = new(1f, 1f, 0f);
        private static readonly Vector3 _lateStart = new(0f, LateHeight, 1f);
        private static readonly Vector3 _spinCenter = new(3f, 0f, 0f);

        public static void Regenerate()
        {
            var root = new GameObject("vat_rigid");
            var pieces = Pieces(root);
            VatRigidRecorder.Record(Path, root, Frames, Fps, frame =>
            {
                Pose(pieces, frame);
                pieces[Late].gameObject.SetActive(frame >= LateFrame);
            });
        }

        public static void RegenerateDeforming()
        {
            var rig = new GameObject("vat_rigid");
            var pieces = Pieces(rig);
            VatRigidRecorder.RecordDeforming(DeformPath, rig, "vat_rigid_deform", Frames, Fps, frame => Pose(pieces, frame));
        }

        private static Transform[] Pieces(GameObject root)
        {
            var mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            return new[] { Piece(root, "slide", mesh), Piece(root, "late", mesh), Piece(root, "spin", mesh) };
        }

        private static void Pose(Transform[] pieces, int frame)
        {
            var time = frame / Fps;
            pieces[Slide].SetLocalPositionAndRotation(new Vector3(SlideSpeed * time, 0f, 0f), Quaternion.AngleAxis(SlideTurnRate * time, _slideAxis));
            pieces[Late].localPosition = _lateStart + 0.5f * Gravity * time * time * Vector3.down;
            pieces[Spin].SetLocalPositionAndRotation(_spinCenter, Quaternion.AngleAxis(SpinTurnRate * time, Vector3.up));
        }

        private static Transform Piece(GameObject root, string name, Mesh mesh)
        {
            var piece = VatRigidRecorder.Piece(root.transform, name, mesh, null).transform;
            piece.localScale = Vector3.one * Size;
            return piece;
        }
    }
}
