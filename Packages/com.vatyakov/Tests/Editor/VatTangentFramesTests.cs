using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    public class VatTangentFramesTests
    {
        private const float Tolerance = 1e-6f;
        private const float UnitTolerance = 1e-5f;

        private static readonly Vector3 _tilted = new Vector3(0.3f, -0.4f, 0.866f).normalized;

        [Test]
        public void Normals_AreNormalized_ZeroFallsBackToForward()
        {
            var frames = Build(0, Frame(new Vector3(0f, 3f, 0f), new Vector4(1f, 0f, 0f, 1f)));
            AssertClose(Vector3.up, frames.Normals[0], "normalized normal");

            frames = Build(0, Frame(Vector3.zero, new Vector4(1f, 0f, 0f, 1f)));
            AssertClose(Vector3.forward, frames.Normals[0], "zero normal falls back to forward");
        }

        [Test]
        public void Tangent_IsOrthogonalizedAgainstTheNormal()
        {
            var frames = Build(0, Frame(Vector3.up, new Vector4(2f, 1f, 0f, -1f)));
            Assert.AreEqual(0f, Vector3.Dot(frames.Normals[0], frames.Tangents[0]), Tolerance, "tangent is orthogonal to the normal");
            AssertClose(Vector3.right, frames.Tangents[0], "orthogonalized tangent");
        }

        [Test]
        public void Frame0_DegenerateTangent_GetsTheBasisFromN([Values(0, 1, 2)] int kind)
        {
            var frames = new VatTangentFrames(1);
            Assert.AreEqual(1, frames.Build(0, Frame(_tilted, Degenerate(kind, _tilted))), "one degenerate tangent");
            AssertOrthonormal(frames.Normals[0], frames.Tangents[0]);
            AssertClose(VatTangentFrames.Basis(_tilted), frames.Tangents[0], "the tangent is the basis of the normal");
            AssertClose(VatTangentFrames.Basis(_tilted), VatTangentFrames.Basis(_tilted * 1f), "the basis is deterministic");
        }

        [Test]
        public void Basis_IsOrthonormal_ForAnyNormal()
        {
            foreach (var normal in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.forward, _tilted, new Vector3(0.9f, 0.1f, 0f).normalized })
            {
                AssertOrthonormal(normal, VatTangentFrames.Basis(normal));
            }
        }

        [Test]
        public void LaterFrames_CarryThePreviousTangent([Values(0, 1, 2)] int kind)
        {
            var frames = new VatTangentFrames(1);
            frames.Build(0, Frame(Vector3.up, new Vector4(1f, 0f, 0f, 1f)));
            var normal = Quaternion.Euler(0f, 0f, 20f) * Vector3.up;
            Assert.AreEqual(1, frames.Build(1, Frame(normal, Degenerate(kind, normal))), "one degenerate tangent");

            var expected = (Vector3.right - normal * Vector3.Dot(normal, Vector3.right)).normalized;
            AssertClose(expected, frames.Tangents[0], "the previous tangent is carried");
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

        private static Vector4 Degenerate(int kind, Vector3 normal)
        {
            return kind switch
            {
                0 => Vector4.zero,
                1 => new Vector4(normal.x * 2f, normal.y * 2f, normal.z * 2f, 1f),
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

        private static void AssertClose(Vector3 expected, Vector3 actual, string message)
        {
            Assert.That((expected - actual).magnitude, Is.LessThan(Tolerance), $"{message}: {expected} vs {actual}");
        }

        private static void AssertOrthonormal(Vector3 normal, Vector3 tangent)
        {
            Assert.AreEqual(1f, normal.magnitude, UnitTolerance, "unit normal");
            Assert.AreEqual(1f, tangent.magnitude, UnitTolerance, "unit tangent");
            Assert.AreEqual(0f, Vector3.Dot(normal, tangent), UnitTolerance, "orthogonal frame");
        }
    }
}
