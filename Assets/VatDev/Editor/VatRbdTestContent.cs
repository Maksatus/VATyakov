using UnityEditor;
using UnityEngine;

namespace VATyakov.Dev
{
    internal static class VatRbdTestContent
    {
        private const string Source = "Assets/VatDev/Content/RBDDestroy/rbd_test_rig.fbx";
        private const string Path = "Assets/VatDev/Content/RBDDestroy/rbd_test.abc";
        private const string ClipName = "Main";

        public static void Regenerate()
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source));
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "rbd_test";
            var clip = Clip();
            var frames = Mathf.RoundToInt(clip.length * clip.frameRate) + 1;
            VatRigidRecorder.Record(Path, root, frames, clip.frameRate, frame => clip.SampleAnimation(root, frame / clip.frameRate));
        }

        private static AnimationClip Clip()
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Source))
            {
                if (asset is AnimationClip clip && clip.name == ClipName)
                {
                    return clip;
                }
            }

            throw new MissingReferenceException($"'{Source}' has no clip '{ClipName}'.");
        }
    }
}
