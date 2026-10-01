using System.Collections.Generic;

namespace VATyakov.Editor
{
    static class VatBakeValidator
    {
        public static List<string> Validate(VatBakeProfile profile)
        {
            var problems = new List<string>();
            Add(problems, SourceProblem(profile));
            Add(problems, ClipProblem(profile));
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

        static void Add(List<string> problems, string problem)
        {
            if (problem != null)
                problems.Add(problem);
        }

        static string SourceProblem(VatBakeProfile profile)
        {
            if (profile.Source == null)
                return "Не задан Source — SkinnedMeshRenderer из префаба или модели.";
            return profile.Source.sharedMesh == null ? $"У '{profile.Source.name}' нет меша." : null;
        }

        static string ClipProblem(VatBakeProfile profile)
        {
            if (profile.Clip == null)
                return "Не задан Clip.";
            return IsPositive(profile.Clip.length) ? null : $"У клипа '{profile.Clip.name}' нулевая длина.";
        }

        static string FpsProblem(VatBakeProfile profile) => IsPositive(profile.Fps) ? null : "Fps должен быть положительным числом.";

        static string MaterialProblem(VatBakeProfile profile) =>
            profile.Material != null || VatTemplateMaterial.ResolveShader(profile) != null
                ? null
                : $"Нет материала-шаблона: назначьте Material или Shader (по умолчанию '{VatBaker.DefaultShaderName}').";

        static bool IsPositive(float value) => float.IsFinite(value) && value > 0f;
    }
}
