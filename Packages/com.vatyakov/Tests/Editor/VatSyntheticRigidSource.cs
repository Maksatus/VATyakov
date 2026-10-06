using System;
using System.Collections.Generic;
using UnityEngine;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    internal sealed class VatSyntheticRigidSource : IVatRigidSource
    {
        public const float HalfSize = 0.25f;
        public const int CubeVertices = 8;

        private static readonly int[] _cubeTriangles =
        {
            0, 2, 1, 1, 2, 3, 4, 5, 6, 5, 7, 6, 0, 1, 4, 1, 5, 4, 2, 6, 3, 3, 6, 7, 0, 4, 2, 2, 4, 6, 1, 3, 5, 3, 7, 5,
        };

        private readonly VatSyntheticPiece[] _pieces;

        public VatSourceClip Clip { get; }
        public int PieceCount => _pieces.Length;
        public IReadOnlyList<double> SampleTimes { get; }
        public IReadOnlyList<string> Warnings => Array.Empty<string>();

        public VatSyntheticRigidSource(float length, IReadOnlyList<double> sampleTimes, params VatSyntheticPiece[] pieces)
        {
            _pieces = pieces;
            Clip = new VatSourceClip("Synthetic", length);
            SampleTimes = sampleTimes;
        }

        public static double[] Uniform(float length, float rate)
        {
            var times = new double[Mathf.RoundToInt(length * rate) + 1];
            for (var sample = 0; sample < times.Length; sample++)
            {
                times[sample] = sample / (double)rate;
            }

            return times;
        }

        public static Matrix4x4 Trs(Vector3 position, Quaternion rotation)
        {
            return Matrix4x4.TRS(position, rotation, Vector3.one);
        }

        public VatRigidTrack[] Extract(VatClip clip, VatRigidInnerTimes inner)
        {
            var tracks = new VatRigidTrack[_pieces.Length];
            for (var index = 0; index < tracks.Length; index++)
            {
                var piece = _pieces[index];
                var track = new VatRigidTrack(piece.Name, Cube(piece.Name), CubeFrame(), clip.FrameCount, inner.Count);
                for (var frame = 0; frame < clip.FrameCount; frame++)
                {
                    track.Frames[frame] = piece.Pose(clip.FrameTime(frame));
                    track.Visible[frame] = piece.IsVisible(clip.FrameTime(frame));
                }

                for (var sample = 0; sample < inner.Count; sample++)
                {
                    track.Inner[sample] = piece.Pose(inner.Times[sample]);
                    track.InnerVisible[sample] = piece.IsVisible(inner.Times[sample]);
                }

                tracks[index] = track;
            }

            return tracks;
        }

        public void Dispose()
        {
        }

        private static VatSourceMesh Cube(string name)
        {
            return new VatSourceMesh(name, CubeVertices, null, new[] { new VatSourceSubMesh(_cubeTriangles, MeshTopology.Triangles) });
        }

        private static float Corner(int corner, int bit)
        {
            return (corner & bit) == 0 ? -HalfSize : HalfSize;
        }

        private static VatFrame CubeFrame()
        {
            var frame = new VatFrame(CubeVertices);
            for (var corner = 0; corner < CubeVertices; corner++)
            {
                var position = new Vector3(Corner(corner, 1), Corner(corner, 2), Corner(corner, 4));
                frame.Positions[corner] = position;
                frame.Normals[corner] = position.normalized;
                frame.Tangents[corner] = VatFrame.MissingTangent;
            }

            return frame;
        }
    }
}
