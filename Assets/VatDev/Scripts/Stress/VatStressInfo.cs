using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VATyakov.Dev
{
    internal static class VatStressInfo
    {
#if ENABLE_IL2CPP
        private const string ScriptingBackend = "IL2CPP";
#else
        private const string ScriptingBackend = "Mono";
#endif

        private const float Megabyte = 1024f * 1024f;

        public static List<string> Describe(VatStress stress, Camera camera)
        {
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            var lines = new List<string>();
            Add(lines, "device", SystemInfo.deviceModel);
            Add(lines, "gpu", SystemInfo.graphicsDeviceName);
            Add(lines, "graphics_api", SystemInfo.graphicsDeviceType.ToString());
            Add(lines, "system_memory_mb", SystemInfo.systemMemorySize);
            Add(lines, "unity", Application.unityVersion);
            Add(lines, "scripting_backend", ScriptingBackend);
            Add(lines, "development_build", Debug.isDebugBuild.ToString());
            Add(lines, "resolution", FormattableString.Invariant($"{Screen.width}x{Screen.height}"));
            Add(lines, "render_scale", pipeline.renderScale);
            Add(lines, "msaa", pipeline.msaaSampleCount);
            Add(lines, "hdr", pipeline.supportsHDR.ToString());
            Add(lines, "shadow_cascades", pipeline.shadowCascadeCount);
            Add(lines, "shadow_distance", pipeline.shadowDistance);
            Add(lines, "shadow_resolution", pipeline.mainLightShadowmapResolution);
            Add(lines, "target_frame_rate", Application.targetFrameRate);
            Add(lines, "vsync", QualitySettings.vSyncCount);
            Add(lines, "variant", stress.Variant.Name);
            Add(lines, "units", stress.Units.Count);
            Add(lines, "visible_units", VisibleCount(stress.Units, camera));
            Add(lines, "lod0_units", Lod0Count(stress.Units));
            Add(lines, "vertices_lod0", VertexCount(stress.Units, 0));
            Add(lines, "vertices_lod1", VertexCount(stress.Units, 1));
            Add(lines, "transition_share", stress.TransitionShare);
            Add(lines, "total_used_mb", Profiler.GetTotalAllocatedMemoryLong() / Megabyte);
            Add(lines, "total_reserved_mb", Profiler.GetTotalReservedMemoryLong() / Megabyte);
            Add(lines, "gfx_used_mb", Profiler.GetAllocatedMemoryForGraphicsDriver() / Megabyte);
            return lines;
        }

        private static int VisibleCount(IReadOnlyList<IVatStressUnit> units, Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var count = 0;
            foreach (var unit in units)
            {
                if (GeometryUtility.TestPlanesAABB(planes, LodRenderers(unit, 0)[0].bounds))
                {
                    count++;
                }
            }

            return count;
        }

        private static int Lod0Count(IReadOnlyList<IVatStressUnit> units)
        {
            var count = 0;
            foreach (var unit in units)
            {
                if (LodRenderers(unit, 0)[0].isVisible)
                {
                    count++;
                }
            }

            return count;
        }

        private static int VertexCount(IReadOnlyList<IVatStressUnit> units, int lod)
        {
            if (units.Count == 0)
            {
                return 0;
            }

            var count = 0;
            foreach (var renderer in LodRenderers(units[0], lod))
            {
                count += SharedMesh(renderer).vertexCount;
            }

            return count;
        }

        private static Renderer[] LodRenderers(IVatStressUnit unit, int lod)
        {
            return unit.Root.GetComponent<LODGroup>().GetLODs()[lod].renderers;
        }

        private static Mesh SharedMesh(Renderer renderer)
        {
            return renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : renderer.GetComponent<MeshFilter>().sharedMesh;
        }

        private static void Add(List<string> lines, string key, string value)
        {
            lines.Add($"{key}{VatStressMessages.KeySeparator}{value}");
        }

        private static void Add(List<string> lines, string key, int value)
        {
            Add(lines, key, value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Add(List<string> lines, string key, float value)
        {
            Add(lines, key, value.ToString("0.###", CultureInfo.InvariantCulture));
        }
    }
}
