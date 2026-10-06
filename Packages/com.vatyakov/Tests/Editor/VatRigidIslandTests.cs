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
        private const double SourceStep = 2.0 / Fps;
        private const float HalfSize = 0.25f;
        private const int FaceVertices = 4;
        private const int FaceIndices = 6;
        private const int CubeFaces = 6;
        private const int Cubes = 2;
        private const int CubeVertices = FaceVertices * CubeFaces;
        private const float Tolerance = 1e-4f;
        private const float Push = 5e-3f;
        private const float StartTime = 0.5f;

        private static readonly Vector3 _secondCube = new(2f, 0f, 0f);
        private static readonly Matrix4x4 _localToWorld = Matrix4x4.TRS(new Vector3(0.3f, 1f, -2f), Quaternion.Euler(0f, 0f, 30f), Vector3.one);

        [Test]
        public void IslandsCutBySeams_MergeIntoOnePiecePerBody()
        {
            var motion = new Motion { Pose = Pose, LocalToWorld = Moving };
            var node = motion.Drive();

            Assert.IsTrue(node.IsSplit, "the mesh moves inside the node");
            Assert.AreEqual(Cubes * CubeFaces, node.Islands.Count, "every face of a seamed cube is an island");
            var pieces = VatRigidSplit.Pieces(new[] { node }, NodeName);
            Assert.AreEqual(Cubes, pieces.Length, "the faces of each cube move together");
            for (var body = 0; body < pieces.Length; body++)
            {
                var piece = pieces[body];
                Assert.AreEqual($"{NodeName}/{body}", piece.Name);
                Assert.AreEqual(CubeVertices, piece.Source.VertexCount);
                Assert.AreEqual(CubeFaces * FaceIndices, piece.Source.SubMeshes[0].Indices.Length, "all triangles of the cube");
                Assert.AreEqual(body == 0 ? 0f : _secondCube.x, piece.Local.Positions[0].x, HalfSize + Tolerance, "pieces keep the order of the mesh");
                AssertFollows(piece, body, time => Moving(time) * Pose(time, body));
            }
        }

        [Test]
        public void DeformingIsland_FailsWithAReport()
        {
            var node = new Motion { Pose = Pose, Push = (vertex, time) => vertex == 0 ? Vector3.one.normalized * (Push * (float)time) : Vector3.zero }.Drive();

            var error = Assert.Throws<VatBakeException>(() => VatRigidSplit.Pieces(new[] { node }, "cloth"));
            StringAssert.Contains("1 of 12 islands", error.Message);
            StringAssert.Contains($"'{NodeName}' island 0: {FaceVertices} vertices", error.Message);
            StringAssert.Contains("mm at 1 s", error.Message, "the push is largest on the last frame");
            StringAssert.Contains("Mode = Vertex", error.Message);
        }

        [Test]
        public void Report_ListsTheTenWorstIslands_SortedByResidual()
        {
            var node = new Motion { Pose = Pose, Push = (vertex, time) => PushFace(vertex, time) }.Drive();

            var message = Assert.Throws<VatBakeException>(() => VatRigidSplit.Pieces(new[] { node }, "cloth")).Message;
            StringAssert.Contains("12 of 12 islands", message);
            StringAssert.Contains("... and 2 more.", message);
            Assert.IsFalse(message.Contains("island 0:") || message.Contains("island 1:"), "the two smallest pushes are left out");
            for (var island = 11; island > 2; island--)
            {
                Assert.Less(message.IndexOf($"island {island}:", StringComparison.Ordinal), message.IndexOf($"island {island - 1}:", StringComparison.Ordinal),
                    $"island {island} has the larger residual");
            }
        }

        [Test]
        public void Residual_IsMeasuredInTheRootSpace()
        {
            Func<int, double, Vector3> push = (vertex, time) => vertex == 0 ? Vector3.up * (Push * (float)time) : Vector3.zero;
            var unit = new Motion { Pose = Pose, Push = push }.Drive();
            var scaled = new Motion { Pose = Pose, Push = push, LocalToWorld = _ => Matrix4x4.Scale(Vector3.one * 2f) }.Drive();

            Assert.AreEqual(2f * unit.Islands[0].Residual, scaled.Islands[0].Residual, Tolerance, "the node scale doubles the residual");
        }

        [Test]
        public void NodeThatOnlyMovesItsTransform_StaysOnePiece()
        {
            var node = new Motion { LocalToWorld = Moving }.Drive();

            Assert.IsFalse(node.IsSplit);
            var pieces = VatRigidSplit.Pieces(new[] { node }, NodeName);
            Assert.AreEqual(1, pieces.Length);
            Assert.AreSame(node.Track, pieces[0]);
            for (var sample = 0; sample < node.Track.SampleCount; sample++)
            {
                Assert.AreEqual(Moving(Time(sample)), node.Track.Sample(sample), $"sample {sample}");
            }
        }

        [Test]
        public void NodeThatStartsMovingMidClip_IsCutFromThere()
        {
            var node = new Motion { Pose = Late }.Drive();

            Assert.IsTrue(node.IsSplit);
            var pieces = VatRigidSplit.Pieces(new[] { node }, NodeName);
            Assert.AreEqual(Cubes, pieces.Length);
            for (var body = 0; body < pieces.Length; body++)
            {
                var index = body;
                AssertFollows(pieces[body], body, time => _localToWorld * Late(time, index));
            }
        }

        [Test]
        public void IslandsWithACommonCentroid_ButDifferentMotion_StayApart()
        {
            var node = new Motion { Pose = Turning }.Drive(Crossed(), CrossedMesh(), vertex => vertex / FaceVertices);

            var pieces = VatRigidSplit.Pieces(new[] { node }, NodeName);
            Assert.AreEqual(2, pieces.Length, "the centroids match on every sample, the corners don't");
        }

        [Test]
        public void SamplesBetweenSourceSamples_AreBlendedRigidly_NotFitted()
        {
            var node = new Motion { Pose = Pose, SourceStep = SourceStep }.Drive();

            var pieces = VatRigidSplit.Pieces(new[] { node }, NodeName);
            Assert.AreEqual(Cubes, pieces.Length, "the shrinking of interpolated vertices is not a deformation");
            for (var body = 0; body < pieces.Length; body++)
            {
                var index = body;
                AssertFollows(pieces[body], body, time => _localToWorld * Blended(time, index));
            }
        }

        [Test]
        public void TransformNode_AndDeformingNode_BakeTogether()
        {
            var still = new Motion { LocalToWorld = Moving }.Drive();
            var deforming = new Motion { Pose = Pose }.Drive();

            var pieces = VatRigidSplit.Pieces(new[] { still, deforming }, NodeName);
            Assert.AreEqual(1 + Cubes, pieces.Length);
            Assert.AreSame(still.Track, pieces[0]);
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

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void RigidFit_OfAPointOrALine_KeepsItsVertices(int count)
        {
            var rest = new[] { new Vector3(0.2f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(2f, 0f, 0f) };
            var motion = Matrix4x4.TRS(new Vector3(3f, -1f, 2f), Quaternion.AngleAxis(70f, new Vector3(1f, 2f, -0.5f)), Vector3.one);
            var current = Array.ConvertAll(rest, motion.MultiplyPoint3x4);
            var vertices = new int[count];
            for (var i = 0; i < count; i++)
            {
                vertices[i] = i;
            }

            var fit = VatRigidFit.Solve(rest, current, vertices);

            Assert.IsTrue(Mathf.Abs(fit.determinant - 1f) < Tolerance, "a proper rotation");
            foreach (var vertex in vertices)
            {
                Assert.Less(Vector3.Distance(current[vertex], fit.MultiplyPoint3x4(rest[vertex])), Tolerance, $"vertex {vertex} of {count}");
            }
        }

        [Test]
        public void Islands_FollowLines_AndKeepLoneVertices()
        {
            var mesh = new VatSourceMesh(NodeName, 5, null, new[] { new VatSourceSubMesh(new[] { 0, 1, 2, 3 }, MeshTopology.Lines) });

            var islands = VatRigidIslands.Find(mesh);

            Assert.AreEqual(3, islands.Length);
            CollectionAssert.AreEqual(new[] { 0, 1 }, islands[0]);
            CollectionAssert.AreEqual(new[] { 2, 3 }, islands[1]);
            CollectionAssert.AreEqual(new[] { 4 }, islands[2], "a vertex of no primitive is an island of its own");
        }

        private static void AssertFollows(VatRigidTrack piece, int body, Func<double, Matrix4x4> pose)
        {
            for (var sample = 0; sample < piece.SampleCount; sample++)
            {
                var expected = pose(Time(sample));
                for (var vertex = 0; vertex < CubeVertices; vertex++)
                {
                    var position = piece.Local.Positions[vertex];
                    var error = Vector3.Distance(expected.MultiplyPoint3x4(position), piece.Sample(sample).MultiplyPoint3x4(position));
                    Assert.Less(error, Tolerance, $"piece {body}, sample {sample}, vertex {vertex}");
                }
            }
        }

        private static Vector3 PushFace(int vertex, double time)
        {
            var face = vertex / FaceVertices;
            var isFirstCorner = vertex % FaceVertices == 0;
            var normal = Vector3.zero;
            normal[face % CubeFaces / 2] = face % 2 == 0 ? 1f : -1f;
            return isFirstCorner ? normal * (Push * (face + 1) * (float)time) : Vector3.zero;
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

        private static Matrix4x4 Late(double time, int body)
        {
            return Pose(Math.Max(0.0, time - StartTime), body);
        }

        private static Matrix4x4 Turning(double time, int body)
        {
            return body == 0 ? Matrix4x4.Rotate(Quaternion.AngleAxis(90f * (float)time, Vector3.forward)) : Matrix4x4.identity;
        }

        private static Matrix4x4 Moving(double time)
        {
            var t = (float)time;
            return Matrix4x4.TRS(new Vector3(t, 1f, -2f), Quaternion.Euler(0f, 0f, 30f + 40f * t), Vector3.one * 2f);
        }

        private static Matrix4x4 Blended(double time, int body)
        {
            var start = Math.Floor(time / SourceStep + 1e-9) * SourceStep;
            var fraction = (float)((time - start) / SourceStep);
            var a = Pose(start, body);
            var b = Pose(start + SourceStep, body);
            var center = body == 0 ? Vector3.zero : _secondCube;
            var rotation = Quaternion.Slerp(a.rotation, b.rotation, fraction);
            var position = Vector3.Lerp(a.MultiplyPoint3x4(center), b.MultiplyPoint3x4(center), fraction);
            return Matrix4x4.TRS(position - rotation * center, rotation, Vector3.one);
        }

        private static VatFrame CubesFrame()
        {
            var frame = new VatFrame(Cubes * CubeVertices);
            for (var body = 0; body < Cubes; body++)
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

        private static VatFrame Crossed()
        {
            var frame = new VatFrame(2 * FaceVertices);
            var corners = new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, 1f) };
            for (var corner = 0; corner < FaceVertices; corner++)
            {
                var c = corners[corner] * HalfSize;
                frame.Positions[corner] = new Vector3(c.x, c.y, 0f);
                frame.Positions[FaceVertices + corner] = new Vector3(c.x, 0f, c.y);
                frame.Normals[corner] = Vector3.forward;
                frame.Normals[FaceVertices + corner] = Vector3.up;
                frame.Tangents[corner] = VatFrame.MissingTangent;
                frame.Tangents[FaceVertices + corner] = VatFrame.MissingTangent;
            }

            return frame;
        }

        private static VatSourceMesh CrossedMesh()
        {
            return Quads(2);
        }

        private static VatSourceMesh CubesMesh()
        {
            return Quads(Cubes * CubeFaces);
        }

        private static VatSourceMesh Quads(int count)
        {
            var indices = new int[count * FaceIndices];
            for (var face = 0; face < count; face++)
            {
                var first = face * FaceVertices;
                var triangles = new[] { first, first + 1, first + 2, first, first + 2, first + 3 };
                triangles.CopyTo(indices, face * FaceIndices);
            }

            return new VatSourceMesh(NodeName, count * FaceVertices, null, new[] { new VatSourceSubMesh(indices, MeshTopology.Triangles) });
        }

        private sealed class Motion
        {
            public Func<double, int, Matrix4x4> Pose = (_, _) => Matrix4x4.identity;
            public Func<int, double, Vector3> Push = (_, _) => Vector3.zero;
            public Func<double, Matrix4x4> LocalToWorld = _ => _localToWorld;
            public double SourceStep;

            public VatRigidNode Drive()
            {
                return Drive(CubesFrame(), CubesMesh(), vertex => vertex / CubeVertices);
            }

            public VatRigidNode Drive(VatFrame local, VatSourceMesh mesh, Func<int, int> bodyOf)
            {
                var track = new VatRigidTrack(NodeName, mesh, local, Frames, InnerCount);
                var node = new VatRigidNode(track);
                for (var sample = 0; sample < track.SampleCount; sample++)
                {
                    var time = Time(sample);
                    var isSource = SourceStep <= 0.0 || IsSourceTime(time);
                    var positions = isSource ? Positions(local, bodyOf, time) : Between(local, bodyOf, time);
                    node.Sample(sample, positions, LocalToWorld(time), true, time, isSource);
                }

                return node;
            }

            private bool IsSourceTime(double time)
            {
                var steps = time / SourceStep;
                return Math.Abs(steps - Math.Round(steps)) < 1e-6;
            }

            private List<Vector3> Between(VatFrame local, Func<int, int> bodyOf, double time)
            {
                var start = Math.Floor(time / SourceStep + 1e-9) * SourceStep;
                var fraction = (float)((time - start) / SourceStep);
                var a = Positions(local, bodyOf, start);
                var b = Positions(local, bodyOf, start + SourceStep);
                for (var vertex = 0; vertex < a.Count; vertex++)
                {
                    a[vertex] = Vector3.Lerp(a[vertex], b[vertex], fraction);
                }

                return a;
            }

            private List<Vector3> Positions(VatFrame local, Func<int, int> bodyOf, double time)
            {
                var positions = new List<Vector3>(local.Positions.Length);
                for (var vertex = 0; vertex < local.Positions.Length; vertex++)
                {
                    positions.Add(Pose(time, bodyOf(vertex)).MultiplyPoint3x4(local.Positions[vertex] + Push(vertex, time)));
                }

                return positions;
            }
        }
    }
}
