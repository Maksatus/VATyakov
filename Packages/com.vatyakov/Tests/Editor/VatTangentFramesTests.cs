using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    public class VatTangentFramesTests
    {
        private static readonly Vector3 _tilted = new Vector3(0.3f, -0.4f, 0.866f).normalized;

        [Test]
        public void Normals_AreNormalized_ZeroFallsBackToForward()
        {
            var frames = Build(0, Frame(new Vector3(0f, 3f, 0f), new Vector4(1f, 0f, 0f, 1f)));
            Assert.AreEqual(Vector3.up, frames.Normals[0]);

            frames = Build(0, Frame(Vector3.zero, new Vector4(1f, 0f, 0f, 1f)));
            Assert.AreEqual(Vector3.forward, frames.Normals[0]);
        }

        [Test]
        public void Tangent_IsOrthogonalizedAgainstTheNormal()
        {
            var frames = Build(0, Frame(Vector3.up, new Vector4(2f, 1f, 0f, -1f)));
            Assert.AreEqual(0f, Vector3.Dot(frames.Normals[0], frames.Tangents[0]), 1e-6f);
            Assert.Less((frames.Tangents[0] - Vector3.right).magnitude, 1e-6f);
        }

        [Test]
        public void Frame0_DegenerateTangent_GetsTheBasisFromN([Values(0, 1, 2)] int kind)
        {
            var frames = new VatTangentFrames(1);
            Assert.AreEqual(1, frames.Build(0, Frame(_tilted, Degenerate(kind, _tilted))));
            AssertOrthonormal(frames.Normals[0], frames.Tangents[0]);
            Assert.AreEqual(VatTangentFrames.Basis(_tilted), frames.Tangents[0]);
            Assert.AreEqual(VatTangentFrames.Basis(_tilted), VatTangentFrames.Basis(_tilted * 1f));
        }

        [Test]
        public void Basis_IsOrthonormal_ForAnyNormal()
        {
            foreach (var n in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.forward, _tilted, new Vector3(0.9f, 0.1f, 0f).normalized })
            {
                AssertOrthonormal(n, VatTangentFrames.Basis(n));
            }
        }

        [Test]
        public void LaterFrames_CarryThePreviousTangent([Values(0, 1, 2)] int kind)
        {
            var frames = new VatTangentFrames(1);
            frames.Build(0, Frame(Vector3.up, new Vector4(1f, 0f, 0f, 1f)));
            var n1 = Quaternion.Euler(0f, 0f, 20f) * Vector3.up;
            Assert.AreEqual(1, frames.Build(1, Frame(n1, Degenerate(kind, n1))));

            var expected = (Vector3.right - n1 * Vector3.Dot(n1, Vector3.right)).normalized;
            Assert.Less((frames.Tangents[0] - expected).magnitude, 1e-6f);
            AssertOrthonormal(frames.Normals[0], frames.Tangents[0]);
        }

        [Test]
        public void Carry_IsDeterministic_AndDoesNotCrossClips()
        {
            var sequence = new[]
            {
                Frame(Vector3.up, new Vector4(0f, 0f, 1f, 1f)),
                Frame(_tilted, Vector4.zero),
                Frame(Vector3.forward, new Vector4(0f, 0f, 3f, 1f)),
                Frame(new Vector3(0f, 1f, 1f), new Vector4(float.NaN, 0f, 0f, 1f)),
            };
            var first = Run(new VatTangentFrames(1), sequence);
            var second = Run(new VatTangentFrames(1), sequence);
            CollectionAssert.AreEqual(first, second, "same input, same tangents");

            var reused = new VatTangentFrames(1);
            Run(reused, new[] { Frame(Vector3.right, new Vector4(0f, 1f, 0f, 1f)), Frame(Vector3.right, Vector4.zero) });
            CollectionAssert.AreEqual(first, Run(reused, sequence), "a new clip starts fresh");
        }

        private static Vector3[] Run(VatTangentFrames frames, VatFrame[] sequence)
        {
            var result = new Vector3[sequence.Length];
            for (var k = 0; k < sequence.Length; k++)
            {
                frames.Build(k, sequence[k]);
                result[k] = frames.Tangents[0];
            }

            return result;
        }

        private static Vector4 Degenerate(int kind, Vector3 n)
        {
            return kind switch
            {
                0 => Vector4.zero,
                1 => new Vector4(n.x * 2f, n.y * 2f, n.z * 2f, 1f),
                _ => new Vector4(float.NaN, float.NaN, 0f, 1f),
            };
        }

        private static VatTangentFrames Build(int frame, VatFrame data)
        {
            var frames = new VatTangentFrames(1);
            frames.Build(frame, data);
            return frames;
        }

        private static VatFrame Frame(Vector3 normal, Vector4 tangent)
        {
            var frame = new VatFrame(1);
            frame.Normals[0] = normal;
            frame.Tangents[0] = tangent;
            return frame;
        }

        private static void AssertOrthonormal(Vector3 n, Vector3 t)
        {
            Assert.AreEqual(1f, n.magnitude, 1e-5f);
            Assert.AreEqual(1f, t.magnitude, 1e-5f);
            Assert.AreEqual(0f, Vector3.Dot(n, t), 1e-5f);
        }
    }
}
