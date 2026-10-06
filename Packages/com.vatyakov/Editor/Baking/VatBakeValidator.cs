using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal static class VatBakeValidator
    {
        public static List<string> Validate(VatBakeProfile profile)
        {
            var problems = new List<string>();
            if (profile.Kind == VatSourceKind.Alembic)
            {
                Add(problems, AlembicProblem(profile));
            }
            else
            {
                AddSkinned(problems, profile);
            }

            Add(problems, ModeProblem(profile));
            Add(problems, FpsProblem(profile));
            Add(problems, MaterialProblem(profile));
            return problems;
        }

        public static void ThrowIfInvalid(VatBakeProfile profile)
        {
            var problems = Validate(profile);
            if (problems.Count > 0)
            {
                throw new VatBakeException(string.Join("\n", problems));
            }
        }

        private static void AddSkinned(List<string> problems, VatBakeProfile profile)
        {
            var sourceProblem = SourceProblem(profile);
            Add(problems, sourceProblem);
            if (sourceProblem == null && profile.IsBone)
            {
                Add(problems, VatBoneRig.Problem(profile.Source));
            }

            if (sourceProblem == null)
            {
                AddExtraRenderers(problems, profile);
            }

            problems.AddRange(VatClipListProblems.Find(profile.Clips));
        }

        private static void AddExtraRenderers(List<string> problems, VatBakeProfile profile)
        {
            if (profile.ExtraRenderers.Count > 0 && !profile.IsBone)
            {
                problems.Add("Extra Renderers bake only in Mode = Bone.");
                return;
            }

            foreach (var extra in profile.ExtraRenderers)
            {
                Add(problems, VatBoneBinding.Problem(profile.Source, extra.Renderer));
            }
        }

        private static void Add(List<string> problems, string problem)
        {
            if (problem != null)
            {
                problems.Add(problem);
            }
        }

        private static string SourceProblem(VatBakeProfile profile)
        {
            if (profile.Source == null)
            {
                return "Skinned Mesh Renderer is not set: assign one from a prefab or model.";
            }

            return profile.Source.sharedMesh == null ? $"'{profile.Source.name}' has no mesh." : null;
        }

        private static string AlembicProblem(VatBakeProfile profile)
        {
            if (!VatAlembic.IsInstalled)
            {
                return VatAlembic.MissingPackage;
            }

            return profile.Alembic == null ? "Alembic is not set: assign an .abc from the project." : VatAlembicProbe.For(profile.Alembic).Problem;
        }

        private static string ModeProblem(VatBakeProfile profile)
        {
            if (profile.Kind == VatSourceKind.Alembic && profile.Mode == VatMode.Bone)
            {
                return "Mode = Bone takes a Skinned Mesh Renderer: use Vertex or Rigid for an Alembic.";
            }

            return profile.Kind == VatSourceKind.Skinned && profile.Mode == VatMode.Rigid
                ? "Mode = Rigid takes an Alembic with rigid pieces: use Vertex or Bone for a Skinned Mesh Renderer."
                : null;
        }

        private static string FpsProblem(VatBakeProfile profile)
        {
            return IsPositive(profile.Fps) ? null : "Frames Per Second must be positive.";
        }

        private static string MaterialProblem(VatBakeProfile profile)
        {
            return profile.Material != null || VatTemplateMaterial.ResolveShader(profile) != null
                ? null
                : $"No template material: assign a Template Material or a Shader (default '{VatBaker.DefaultShaderName}').";
        }

        private static bool IsPositive(float value)
        {
            return float.IsFinite(value) && value > 0f;
        }
    }
}
