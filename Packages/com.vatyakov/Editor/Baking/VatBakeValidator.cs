using System.Collections.Generic;

namespace VATyakov.Editor
{
    static class VatBakeValidator
    {
        public static List<string> Validate(VatBakeProfile profile)
        {
            var problems = new List<string>();
            if (profile.Kind == VatSourceKind.Alembic)
                Add(problems, AlembicProblem(profile));
            else
                AddSkinned(problems, profile);
            Add(problems, FpsProblem(profile));
            Add(problems, MaterialProblem(profile));
            return problems;
        }

        public static void ThrowIfInvalid(VatBakeProfile profile)
        {
            var problems = Validate(profile);
            if (problems.Count > 0)
                throw new VatBakeException(string.Join("\n", problems));
        }

        static void AddSkinned(List<string> problems, VatBakeProfile profile)
        {
            Add(problems, SourceProblem(profile));
            problems.AddRange(VatClipListProblems.Find(profile.Clips));
        }

        static void Add(List<string> problems, string problem)
        {
            if (problem != null)
                problems.Add(problem);
        }

        static string SourceProblem(VatBakeProfile profile)
        {
            if (profile.Source == null)
                return "Skinned Mesh Renderer is not set: assign one from a prefab or model.";
            return profile.Source.sharedMesh == null ? $"'{profile.Source.name}' has no mesh." : null;
        }

        static string AlembicProblem(VatBakeProfile profile)
        {
            if (!VatAlembic.IsInstalled)
                return VatAlembic.MissingPackage;
            return profile.Alembic == null ? "Alembic is not set: assign an .abc from the project." : VatAlembicProbe.For(profile.Alembic).Problem;
        }

        static string FpsProblem(VatBakeProfile profile) => IsPositive(profile.Fps) ? null : "Frames Per Second must be positive.";

        static string MaterialProblem(VatBakeProfile profile) =>
            profile.Material != null || VatTemplateMaterial.ResolveShader(profile) != null
                ? null
                : $"No template material: assign a Template Material or a Shader (default '{VatBaker.DefaultShaderName}').";

        static bool IsPositive(float value) => float.IsFinite(value) && value > 0f;
    }
}
