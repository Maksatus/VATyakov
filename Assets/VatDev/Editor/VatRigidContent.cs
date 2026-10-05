using UnityEditor;

namespace VATyakov.Dev
{
    internal static class VatRigidContent
    {
        [MenuItem("VATyakov/Dev/Regenerate Rigid Content")]
        private static void Regenerate()
        {
            VatRigidFixture.Regenerate();
            VatRigidFixture.RegenerateDeforming();
            VatRbdTestContent.Regenerate();
            VatRbdTestContent.RegenerateDeforming();
            VatRbdWallContent.Regenerate();
            AssetDatabase.Refresh();
        }
    }
}
