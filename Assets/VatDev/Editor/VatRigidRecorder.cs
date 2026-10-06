using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Util;
using Object = UnityEngine.Object;

namespace VATyakov.Dev
{
    internal static class VatRigidRecorder
    {
        public static void Record(string path, GameObject root, int frames, float fps, Action<int> pose)
        {
            Record(path, root, frames, fps, true, pose);
        }

        public static void RecordDeforming(string path, GameObject rig, string name, int frames, float fps, Action<int> pose)
        {
            VatDeformGeometry geometry = null;
            try
            {
                geometry = new VatDeformGeometry(rig, name);
                Record(path, geometry.Root, frames, fps, false, frame =>
                {
                    pose(frame);
                    geometry.Update();
                });
            }
            finally
            {
                geometry?.Dispose();
                Object.DestroyImmediate(rig);
            }
        }

        public static GameObject Piece(Transform parent, string name, Mesh mesh, Material material)
        {
            var piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            piece.AddComponent<MeshRenderer>().sharedMaterial = material;
            return piece;
        }

        private static void Record(string path, GameObject root, int frames, float fps, bool isConstant, Action<int> pose)
        {
            var meta = $"{path}.meta";
            var metaText = File.Exists(meta) ? File.ReadAllText(meta) : null;
            AssetDatabase.DeleteAsset(path);
            try
            {
                using var recorder = new AlembicRecorder { Settings = Settings(Path.GetFullPath(path), root, fps, isConstant) };
                for (var frame = 0; frame < frames; frame++)
                {
                    pose(frame);
                    if (frame == 0)
                    {
                        recorder.BeginRecording();
                    }

                    recorder.ProcessRecording();
                }

                recorder.EndRecording();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            if (metaText != null)
            {
                File.WriteAllText(meta, metaText);
            }

            AssetDatabase.ImportAsset(path);
        }

        private static AlembicRecorderSettings Settings(string path, GameObject target, float fps, bool isConstant)
        {
            var settings = new AlembicRecorderSettings
            {
                OutputPath = path,
                Scope = ExportScope.TargetBranch,
                TargetBranch = target,
                CaptureMeshRenderer = true,
                CaptureSkinnedMeshRenderer = false,
                CaptureCamera = false,
                AssumeNonSkinnedMeshesAreConstant = isConstant,
                MeshNormals = true,
                MeshUV0 = true,
                MeshUV1 = false,
                MeshColors = false,
                MeshSubmeshes = true,
            };
            settings.ExportOptions.TimeSamplingType = TimeSamplingType.Uniform;
            settings.ExportOptions.FrameRate = fps;
            return settings;
        }
    }
}
