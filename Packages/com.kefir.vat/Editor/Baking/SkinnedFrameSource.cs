using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Kefir.Vat.Editor
{
    /// <summary>
    /// Samples a SkinnedMeshRenderer (§2.1). The bake copy lives in a preview scene; Generic and Humanoid clips play
    /// through a PlayableGraph with foot IK and root motion off, legacy clips through SampleAnimation.
    /// BakeMesh includes blend shapes; the result is converted to the space of the prefab root.
    /// </summary>
    sealed class SkinnedFrameSource : IVatFrameSource
    {
        readonly AnimationClip[] _clips;
        readonly VatSourceClip[] _clipInfos;
        readonly List<Vector3> _positions = new List<Vector3>();
        readonly List<Vector3> _normals = new List<Vector3>();
        readonly List<Vector4> _tangents = new List<Vector4>();

        Scene _scene;
        GameObject _root;
        SkinnedMeshRenderer _renderer;
        Animator _animator;
        Mesh _baked;
        PlayableGraph _graph;
        AnimationClipPlayable _playable;
        int _graphClip = -1;

        public SkinnedFrameSource(SkinnedMeshRenderer source, IReadOnlyList<AnimationClip> clips)
        {
            if (source == null || source.sharedMesh == null)
                throw new ArgumentException("Source SkinnedMeshRenderer with a mesh is required.", nameof(source));

            _clips = new AnimationClip[clips.Count];
            _clipInfos = new VatSourceClip[clips.Count];
            for (int i = 0; i < clips.Count; i++)
            {
                _clips[i] = clips[i];
                _clipInfos[i] = new VatSourceClip(clips[i].name, clips[i].length);
            }

            Mesh = ReadMesh(source.sharedMesh);

            try
            {
                CreateBakeCopy(source);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public VatSourceMesh Mesh { get; }

        public IReadOnlyList<VatSourceClip> Clips => _clipInfos;

        public void Sample(int clip, double time, VatFrame frame)
        {
            var animationClip = _clips[clip];
            if (animationClip.legacy)
            {
                animationClip.SampleAnimation(_root, (float)time);
            }
            else
            {
                UseGraph(clip);
                _playable.SetTime(time);
                _graph.Evaluate();
            }

            _renderer.BakeMesh(_baked, true); // vertices in the renderer's local space, scale included
            _baked.GetVertices(_positions);
            _baked.GetNormals(_normals);
            _baked.GetTangents(_tangents);

            int count = Mesh.VertexCount;
            if (_positions.Count != count)
                throw new InvalidOperationException($"BakeMesh returned {_positions.Count} vertices, the source mesh has {count}.");

            var toRoot = _root.transform.worldToLocalMatrix * _renderer.transform.localToWorldMatrix;
            var normalMatrix = toRoot.inverse.transpose;
            float handedness = toRoot.determinant < 0f ? -1f : 1f;
            bool hasNormals = _normals.Count == count;
            bool hasTangents = _tangents.Count == count;

            for (int i = 0; i < count; i++)
            {
                frame.Positions[i] = toRoot.MultiplyPoint3x4(_positions[i]);
                frame.Normals[i] = hasNormals ? normalMatrix.MultiplyVector(_normals[i]).normalized : Vector3.forward;
                if (hasTangents)
                {
                    var tangent = _tangents[i];
                    var direction = toRoot.MultiplyVector(tangent).normalized;
                    frame.Tangents[i] = new Vector4(direction.x, direction.y, direction.z, tangent.w * handedness);
                }
                else
                {
                    frame.Tangents[i] = new Vector4(1f, 0f, 0f, 1f);
                }
            }
        }

        public void Dispose()
        {
            DestroyGraph();
            if (_baked != null)
                Object.DestroyImmediate(_baked);
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            _baked = null;
            _root = null;
        }

        void CreateBakeCopy(SkinnedMeshRenderer source)
        {
            var sourceRoot = source.transform.root;
            string rendererPath = AnimationUtility.CalculateTransformPath(source.transform, sourceRoot);

            // A preview scene keeps the copy out of the user's scenes (no dirty flag, no leftovers).
            _scene = EditorSceneManager.NewPreviewScene();
            var holder = EditorUtility.CreateGameObjectWithHideFlags("VatBake", HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(holder, _scene);

            _root = Object.Instantiate(sourceRoot.gameObject, holder.transform);
            _root.name = sourceRoot.name;
            _root.transform.localPosition = Vector3.zero;
            _root.transform.localRotation = Quaternion.identity;
            _root.transform.localScale = Vector3.one;

            var rendererTransform = rendererPath.Length == 0 ? _root.transform : _root.transform.Find(rendererPath);
            _renderer = rendererTransform != null ? rendererTransform.GetComponent<SkinnedMeshRenderer>() : null;
            if (_renderer == null)
                throw new InvalidOperationException($"Renderer '{rendererPath}' not found in the bake copy of '{sourceRoot.name}'.");
            _renderer.quality = SkinQuality.Bone4; // independent of the editor's current quality level
            _baked = new Mesh { name = "VatBakeFrame" };

            bool needsAnimator = Array.Exists(_clips, c => !c.legacy);
            if (!needsAnimator)
                return;

            _animator = _root.GetComponentInChildren<Animator>(true);
            if (_animator == null)
                _animator = _root.AddComponent<Animator>();
            _animator.enabled = true;
            _animator.runtimeAnimatorController = null;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (!_animator.hasTransformHierarchy)
                AnimatorUtility.DeoptimizeTransformHierarchy(_animator.gameObject);
        }

        void UseGraph(int clip)
        {
            if (_graphClip == clip)
                return;

            DestroyGraph();
            _graph = PlayableGraph.Create("VatBake");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(_graph, "out", _animator);
            _playable = AnimationClipPlayable.Create(_graph, _clips[clip]);
            _playable.SetApplyFootIK(false); // on by default for a new playable
            _playable.SetApplyPlayableIK(false);
            output.SetSourcePlayable(_playable);
            _graphClip = clip;
        }

        void DestroyGraph()
        {
            if (_graph.IsValid())
                _graph.Destroy();
            _graphClip = -1;
        }

        static VatSourceMesh ReadMesh(Mesh mesh)
        {
            var uv = mesh.uv;
            var subMeshes = new VatSourceMesh.SubMesh[mesh.subMeshCount];
            for (int i = 0; i < subMeshes.Length; i++)
                subMeshes[i] = new VatSourceMesh.SubMesh(mesh.GetIndices(i, true), mesh.GetTopology(i));
            return new VatSourceMesh(mesh.name, mesh.vertexCount, uv.Length == mesh.vertexCount ? uv : null, subMeshes);
        }
    }
}
