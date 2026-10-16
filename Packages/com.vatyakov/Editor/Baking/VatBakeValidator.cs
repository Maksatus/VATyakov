using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal static class VatBakeValidator
    {
        private const string NotVatHint = "Duplicate a VAT template, or clear the field to create one.";
        private const string SharedHint = "the profiles were duplicated before 0.16.4: delete one of them and duplicate the other again.";

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
            Add(problems, SharedProblem(profile));
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

            problems.AddRange(VatClipListProblems.Find(profile.Clips));
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
            var material = profile.Material;
            if (material == null || VatMaterialCopies.IsVat(material))
            {
                return null;
            }

            return $"Template Material '{material.name}' uses '{material.shader.name}', not a VAT shader, and the bake would replace it. {NotVatHint}";
        }

        private static string SharedProblem(VatBakeProfile profile)
        {
            var other = VatProfileLookup.Sharing(profile, out var shared);
            if (other == null)
            {
                return null;
            }

            var field = shared is VatAsset ? "VAT Asset" : "Template Material";
            return $"{field} '{shared.name}' is baked by profile '{other.name}' too: {SharedHint}";
        }

        private static bool IsPositive(float value)
        {
            return float.IsFinite(value) && value > 0f;
        }
    }
}
