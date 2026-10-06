using System;
using System.Linq;

namespace VATyakov.Editor
{
    internal static class VatExtraMaterials
    {
        public static string[] ShownClips(VatBakeProfile profile)
        {
            if (!profile.IsBone)
            {
                return Array.Empty<string>();
            }

            return profile.ExtraRenderers.Select(extra => VatTemplateMaterial.ShownClip(profile.Asset, extra.Material)).ToArray();
        }

        public static void Apply(VatBakeProfile profile, VatAsset asset, string[] clipNames)
        {
            for (var i = 0; i < clipNames.Length; i++)
            {
                var material = profile.ExtraRenderers[i].Material;
                if (material != null)
                {
                    VatTemplateMaterial.Fit(material, asset, clipNames[i]);
                }
            }
        }
    }
}
