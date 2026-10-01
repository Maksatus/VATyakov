using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    public class VatShaderGraphTests
    {
        const string HalfParent = "Packages/com.vatyakov/Tests/Editor/Fixtures/VAT_HalfParent.shadergraph";
        const string UnlitSample = "Packages/com.vatyakov/Samples/UnlitVertex/VAT_Unlit_Vertex.shadergraph";

        [Test]
        public void HalfPrecisionParent_CompilesAndCallsFloatWrapper()
        {
            AssertCompiles(HalfParent);

            string code = GeneratedCode(HalfParent);
            if (code == null)
                Assert.Inconclusive("Shader Graph internals changed: generated code is not available.");

            // Only VatVertexPosition_float exists, so a _half call would also fail to compile.
            StringAssert.Contains("VatVertexPosition_float(", code);
            StringAssert.DoesNotContain("VatVertexPosition_half", code);
            // Promoted state must stay float4 in UnityPerMaterial: half breaks rows and W above 2048.
            StringAssert.Contains("float4 _VatFrame;", code);
            StringAssert.Contains("float4 _VatLayout;", code);
            StringAssert.DoesNotContain("half4 _VatFrame", code);
            StringAssert.DoesNotContain("half4 _VatLayout", code);
            StringAssert.Contains("UNITY_ACCESS_HYBRID_INSTANCED_PROP(_VatFrame, float4)", code, "state is Hybrid Per Instance");
        }

        [Test]
        public void UnlitSample_Compiles()
        {
            Assume.That(AssetDatabase.LoadAssetAtPath<Shader>(UnlitSample), Is.Not.Null, "sample not in the project");
            AssertCompiles(UnlitSample);
        }

        static void AssertCompiles(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.IsNotNull(shader, path);
            var material = new Material(shader);
            try
            {
                for (int pass = 0; pass < shader.passCount; pass++)
                    ShaderUtil.CompilePass(material, pass, true);
                var errors = ShaderUtil.GetShaderMessages(shader)
                    .Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    .Select(m => $"{m.message} (line {m.line})").ToArray();
                Assert.IsEmpty(errors, string.Join("\n", errors));
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        // Through Shader Graph internals; null when they are not available.
        static string GeneratedCode(string path)
        {
            var importer = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter"))
                .FirstOrDefault(t => t != null);
            var method = importer?.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "GetShaderText" && m.GetParameters().Length == 4);
            if (method == null)
                return null;

            var collection = Activator.CreateInstance(method.GetParameters()[2].ParameterType, true);
            return method.Invoke(null, new object[] { path, null, collection, null }) as string;
        }
    }
}
