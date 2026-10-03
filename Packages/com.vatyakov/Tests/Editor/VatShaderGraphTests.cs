using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using VATyakov.Editor;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    public class VatShaderGraphTests
    {
        private const string HalfParent = "Packages/com.vatyakov/Tests/Editor/Fixtures/vat_half_parent.shadergraph";
        private const string HalfBlendParent = "Packages/com.vatyakov/Tests/Editor/Fixtures/vat_half_parent_blend.shadergraph";
        private const string UnlitSample = "Packages/com.vatyakov/Samples/UnlitVertex/vat_unlit_vertex.shadergraph";
        private const string LitSample = "Packages/com.vatyakov/Samples/LitVertex/vat_lit_vertex.shadergraph";
        private const string LitBlendSample = "Packages/com.vatyakov/Samples/LitVertexBlend/vat_lit_vertex_blend.shadergraph";
        private const string TriplanarSample = "Packages/com.vatyakov/Samples/LitVertexTriplanar/vat_lit_vertex_triplanar.shadergraph";
        private const string LitBlendShaderName = "VATyakov/vat_lit_vertex_blend";

        [Test]
        public void HalfPrecisionParent_CompilesAndCallsFloatWrapper()
        {
            AssertCompiles(HalfParent);

            var code = RequireGeneratedCode(HalfParent);
            StringAssert.Contains("VatVertexPosition_float(", code);
            StringAssert.DoesNotContain("VatVertexPosition_half", code);
            AssertFloat4(code, "_VatLayout");
            AssertFloat4(code, "_VatPosScale");
            AssertInstanceState(code, "_VatFrame", "_VatDrift");
            StringAssert.DoesNotContain("_VatFrameB", code, "no transition without the blend subgraph");
        }

        [Test]
        public void HalfPrecisionBlendParent_CompilesAndCallsFloatWrapper()
        {
            AssertCompiles(HalfBlendParent);

            var code = RequireGeneratedCode(HalfBlendParent);
            StringAssert.Contains("VatVertexPositionBlend_float(", code);
            StringAssert.DoesNotContain("VatVertexPositionBlend_half", code);
            AssertFloat4(code, "_VatLayout");
            AssertFloat4(code, "_VatPosScale");
            AssertInstanceState(code, "_VatFrame", "_VatFrameB", "_VatDrift");
        }

        [Test]
        public void LitSample_CompilesAndTakesNormalAndTangentFromTheRotationTexture()
        {
            AssertCompiles(LitSample);
            Assert.AreEqual(VatBaker.DefaultShaderName, AssetDatabase.LoadAssetAtPath<Shader>(LitSample).name, "default template shader");

            var code = RequireGeneratedCode(LitSample);
            StringAssert.Contains("VatVertexNormalTangent_float(", code);
            StringAssert.DoesNotContain("VatVertexNormalTangent_half", code);
            StringAssert.DoesNotContain("Blend_float(", code, "the default template reads one clip");
            StringAssert.DoesNotContain("_VatFrameB", code, "the default template has no transition");
            StringAssert.Contains("TEXTURE2D(_VatRotTex)", code);
            StringAssert.DoesNotContain("_VatDriftTex", code, "drift comes from the CPU, not a texture");
            StringAssert.Contains("TEXTURE2D(_BumpMap)", code);
        }

        [Test]
        public void LitBlendSample_CompilesAndReadsTheTransitionClip()
        {
            AssertCompiles(LitBlendSample);
            Assert.AreEqual(LitBlendShaderName, AssetDatabase.LoadAssetAtPath<Shader>(LitBlendSample).name, "blend template shader");

            var code = RequireGeneratedCode(LitBlendSample);
            StringAssert.Contains("VatVertexPositionBlend_float(", code);
            StringAssert.Contains("VatVertexNormalTangentBlend_float(", code);
            AssertInstanceState(code, "_VatFrameB");
            StringAssert.Contains("TEXTURE2D(_BumpMap)", code);
        }

        [Test]
        public void TriplanarSample_CompilesWithoutUv()
        {
            AssertCompiles(TriplanarSample);
            Assert.AreEqual(VatBaker.TriplanarShaderName, AssetDatabase.LoadAssetAtPath<Shader>(TriplanarSample).name, "triplanar template shader");

            var code = RequireGeneratedCode(TriplanarSample);
            StringAssert.Contains("VatVertexNormalTangent_float(", code);
            StringAssert.Contains("TEXTURE2D(_BumpMap)", code);
            StringAssert.Contains("Triplanar", code);
            StringAssert.DoesNotContain("IN.uv0", code, "no UV sampling");
        }

        [Test]
        public void UnlitSample_Compiles()
        {
            Assume.That(AssetDatabase.LoadAssetAtPath<Shader>(UnlitSample), Is.Not.Null, "sample not in the project");
            AssertCompiles(UnlitSample);
        }

        private static void AssertInstanceState(string code, params string[] properties)
        {
            foreach (var property in properties)
            {
                AssertFloat4(code, property);
                StringAssert.Contains($"UNITY_ACCESS_HYBRID_INSTANCED_PROP({property}, float4)", code, $"{property} is Hybrid Per Instance");
            }
        }

        private static void AssertFloat4(string code, string property)
        {
            StringAssert.Contains($"float4 {property};", code);
            StringAssert.DoesNotContain($"half4 {property};", code);
        }

        private static void AssertCompiles(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.IsNotNull(shader, path);
            var material = new Material(shader);
            try
            {
                for (var pass = 0; pass < shader.passCount; pass++)
                {
                    ShaderUtil.CompilePass(material, pass, true);
                }

                var errors = ShaderUtil.GetShaderMessages(shader)
                    .Where(message => message.severity == ShaderCompilerMessageSeverity.Error)
                    .Select(message => $"{message.message} (line {message.line})").ToArray();
                Assert.IsEmpty(errors, string.Join("\n", errors));
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), $"{path} has errors");
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        private static string RequireGeneratedCode(string path)
        {
            var code = GeneratedCode(path);
            if (code == null)
            {
                Assert.Inconclusive("Shader Graph internals changed: generated code is not available.");
            }

            return code;
        }

        private static string GeneratedCode(string path)
        {
            var importer = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter"))
                .FirstOrDefault(type => type != null);
            var method = importer?.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "GetShaderText" && candidate.GetParameters().Length == 4);
            if (method == null)
            {
                return null;
            }

            var collection = Activator.CreateInstance(method.GetParameters()[2].ParameterType, true);
            return method.Invoke(null, new object[] { path, null, collection, null }) as string;
        }
    }
}
