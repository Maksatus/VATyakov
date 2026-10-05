using UnityEditor;
using UnityEngine;

namespace VATyakov.Dev
{
    internal static class VatRbdTestContent
    {
        private const string Source = "Assets/VatDev/Content/RBDDestroy/rbd_test_rig.fbx";
        private const string Path = "Assets/VatDev/Content/RBDDestroy/rbd_test.abc";
        private const string DeformPath = "Assets/VatDev/Content/RBDDestroy/rbd_test_deform.abc";
        private const string ClipName = "Main";

        public static void Regenerate()
        {
            var root = Rig("rbd_test");
            var clip = Clip();
            VatRigidRecorder.Record(Path, root, FrameCount(clip), clip.frameRate, frame => clip.SampleAnimation(root, frame / clip.frameRate));
        }

        public static void RegenerateDeforming()
        {
            var rig = Rig("rbd_test");
            var clip = Clip();
            VatRigidRecorder.RecordDeforming(DeformPath, rig, "rbd_test_deform", FrameCount(clip), clip.frameRate,
                frame => clip.SampleAnimation(rig, frame / clip.frameRate));
        }

        private static GameObject Rig(string name)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source));
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = name;
            return root;
        }

        private static int FrameCount(AnimationClip clip)
        {
            return Mathf.RoundToInt(clip.length * clip.frameRate) + 1;
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
