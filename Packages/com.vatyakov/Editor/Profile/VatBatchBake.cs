using System.Collections.Generic;
using UnityEditor;

namespace VATyakov.Editor
{
    internal static class VatBatchBake
    {
        private const string MenuPath = "Assets/Bake VAT Profiles";

        public static void BakeAll(IReadOnlyList<VatBakeProfile> profiles)
        {
            var errors = new List<string>();
            foreach (var profile in profiles)
            {
                try
                {
                    VatBaker.Bake(profile);
                }
                catch (VatBakeException exception)
                {
                    errors.Add($"'{profile.name}': {exception.Message}");
                }
            }

            if (errors.Count > 0)
            {
                throw new VatBakeException(string.Join("\n\n", errors));
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool HasSelectedProfiles()
        {
            return Selection.GetFiltered<VatBakeProfile>(SelectionMode.Assets).Length > 0;
        }

        [MenuItem(MenuPath, false, VatBakeProfile.MenuOrder)]
        private static void BakeSelected()
        {
            VatBakeDialog.Run(() => BakeAll(Selection.GetFiltered<VatBakeProfile>(SelectionMode.Assets)));
        }
    }
}
