using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    public class VatRigidIslandTests
    {
        private const string NodeName = "node";
        private const int Frames = 31;
        private const int InnerCount = 10;
        private const float Fps = 30f;
        private const float HalfSize = 0.25f;
        private const int FaceVertices = 4;
        private const int CubeFaces = 6;
        private const int CubeVertices = FaceVertices * CubeFaces;
        private const float Tolerance = 1e-4f;
        private const float Push = 5e-3f;

        private static readonly Vector3 _secondCube = new(2f, 0f, 0f);
        private static readonly Matrix4x4 _localToWorld = Matrix4x4.TRS(new Vector3(0.3f, 1f, -2f), Quaternion.Euler(0f, 0f, 30f), Vector3.one);

        [Test]
        public void IslandsCutBySeams_MergeIntoOnePiecePerBody()
        {
            var node = Drive(Pose);

            Assert.IsTrue(node.IsSplit, "the mesh moves inside the node");
            Assert.AreEqual(2 * CubeFaces, node.Islands.Count, "every face of a seamed cube is an island");
            var pieces = VatRigidSplit.Pieces(new[] { node }, NodeName);
            Assert.AreEqual(2, pieces.Length, "the faces of each cube move together");
            for (var body = 0; body < pieces.Length; body++)
            {
                var piece = pieces[body];
                Assert.AreEqual($"{NodeName}/{body}", piece.Name);
                Assert.AreEqual(CubeVertices, piece.Source.VertexCount);
                Assert.AreEqual(CubeFaces * 2 * 3, piece.Source.SubMeshes[0].Indices.Length, "all triangles of the cube");
                Assert.AreEqual(body == 0 ? 0f : _secondCube.x, piece.Local.Positions[0].x, HalfSize + Tolerance, "pieces keep the order of the mesh");
                for (var sample = 0; sample < piece.SampleCount; sample++)
                {
                    var pose = _localToWorld * Pose(Time(sample), body);
                    for (var vertex = 0; vertex < CubeVertices; vertex++)
                    {
                        var expected = pose.MultiplyPoint3x4(piece.Local.Positions[vertex]);
                        var actual = piece.Sample(sample).MultiplyPoint3x4(piece.Local.Positions[vertex]);
                        Assert.Less(Vector3.Distance(expected, actual), Tolerance, $"piece {body}, sample {sample}, vertex {vertex}");
                    }
                }
            }
        }

        [Test]
        public void DeformingIsland_FailsWithAReport()
        {
            var node = Drive(Pose, (vertex, time) => vertex == 0 ? Vector3.one.normalized * (Push * (float)time) : Vector3.zero);

            var error = Assert.Throws<VatBakeException>(() => VatRigidSplit.Pieces(new[] { node }, "cloth"));
            StringAssert.Contains("1 of 12 islands", error.Message);
            StringAssert.Contains($"'{NodeName}' island 0: {FaceVertices} vertices", error.Message);
            StringAssert.Contains("mm at 1 s", error.Message, "the push is largest on the last frame");
            StringAssert.Contains("Mode = Vertex", error.Message);
        }

        [Test]
        public void NodeThatOnlyMovesItsTransform_StaysOnePiece()
        {
            var node = Drive((_, _) => Matrix4x4.identity);

            Assert.IsFalse(node.IsSplit);
            var pieces = VatRigidSplit.Pieces(new[] { node }, NodeName);
            Assert.AreEqual(1, pieces.Length);
            Assert.AreSame(node.Track, pieces[0]);
        }

        [Test]
        public void RigidFit_RecoversTheMotion_OfAFlatIsland()
        {
            var rest = new[] { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(1f, 0.5f, 0f), new Vector3(0f, 0.5f, 0f) };
            var motion = Matrix4x4.TRS(new Vector3(3f, -1f, 2f), Quaternion.AngleAxis(160f, new Vector3(1f, 2f, -0.5f)), Vector3.one);
            var current = Array.ConvertAll(rest, motion.MultiplyPoint3x4);

            var fit = VatRigidFit.Solve(rest, current, new[] { 0, 1, 2, 3 });

            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    Assert.AreEqual(motion[row, column], fit[row, column], Tolerance, $"m{row}{column}");
                }
            }
        }

        private static VatRigidNode Drive(Func<double, int, Matrix4x4> pose, Func<int, double, Vector3> push = null)
        {
            var local = Cubes();
            var track = new VatRigidTrack(NodeName, Mesh(), local, Frames, InnerCount);
            var node = new VatRigidNode(track);
            var positions = new List<Vector3>(local.Positions);
            for (var sample = 0; sample < track.SampleCount; sample++)
            {
                var time = Time(sample);
                for (var vertex = 0; vertex < positions.Count; vertex++)
                {
                    var offset = push?.Invoke(vertex, time) ?? Vector3.zero;
                    positions[vertex] = pose(time, vertex / CubeVertices).MultiplyPoint3x4(local.Positions[vertex]) + offset;
                }

                node.Sample(sample, positions, _localToWorld, true, time);
            }

            return node;
        }

        private static double Time(int sample)
        {
            return sample < Frames ? sample / Fps : (sample - Frames + 0.5) / Fps;
        }

        private static Matrix4x4 Pose(double time, int body)
        {
            var t = (float)time;
            return body == 0
                ? Matrix4x4.TRS(new Vector3(t, 0f, 0f), Quaternion.AngleAxis(90f * t, new Vector3(1f, 1f, 0f)), Vector3.one)
                : Matrix4x4.TRS(new Vector3(0f, -t * t, 0.5f * t), Quaternion.AngleAxis(200f * t, Vector3.up), Vector3.one);
        }

        private static VatFrame Cubes()
        {
            var frame = new VatFrame(2 * CubeVertices);
            for (var body = 0; body < 2; body++)
            {
                var center = body == 0 ? Vector3.zero : _secondCube;
                for (var face = 0; face < CubeFaces; face++)
                {
                    var axis = face / 2;
                    var normal = Vector3.zero;
                    normal[axis] = face % 2 == 0 ? 1f : -1f;
                    var u = Vector3.zero;
                    u[(axis + 1) % 3] = HalfSize;
                    var v = Vector3.zero;
                    v[(axis + 2) % 3] = HalfSize;
                    for (var corner = 0; corner < FaceVertices; corner++)
                    {
                        var index = body * CubeVertices + face * FaceVertices + corner;
                        var su = corner == 1 || corner == 2 ? 1f : -1f;
                        var sv = corner >= 2 ? 1f : -1f;
                        frame.Positions[index] = center + normal * HalfSize + su * u + sv * v;
                        frame.Normals[index] = normal;
                        frame.Tangents[index] = VatFrame.MissingTangent;
                    }
                }
            }

            return frame;
        }

        private static VatSourceMesh Mesh()
        {
            var faces = 2 * CubeFaces;
            var indices = new int[faces * 6];
            for (var face = 0; face < faces; face++)
            {
                var first = face * FaceVertices;
                var triangles = new[] { first, first + 1, first + 2, first, first + 2, first + 3 };
                triangles.CopyTo(indices, face * 6);
            }

            return new VatSourceMesh(NodeName, faces * FaceVertices, null, new[] { new VatSourceSubMesh(indices, MeshTopology.Triangles) });
        }
    }
}
