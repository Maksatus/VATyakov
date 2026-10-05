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
            AssetDatabase.DeleteAsset(path);
            try
            {
                using var recorder = new AlembicRecorder { Settings = Settings(Path.GetFullPath(path), root, fps) };
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

            AssetDatabase.ImportAsset(path);
        }

        public static GameObject Piece(Transform parent, string name, Mesh mesh, Material material)
        {
            var piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            piece.AddComponent<MeshRenderer>().sharedMaterial = material;
            return piece;
        }

        private static AlembicRecorderSettings Settings(string path, GameObject target, float fps)
        {
            var settings = new AlembicRecorderSettings
            {
                OutputPath = path,
                Scope = ExportScope.TargetBranch,
                TargetBranch = target,
                CaptureMeshRenderer = true,
                CaptureSkinnedMeshRenderer = false,
                CaptureCamera = false,
                AssumeNonSkinnedMeshesAreConstant = true,
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
