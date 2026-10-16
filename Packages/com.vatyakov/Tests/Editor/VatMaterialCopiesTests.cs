using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    public class VatMaterialCopiesTests
    {
        private const string UnlitShaderName = "VATyakov/vat_unlit_vertex";
        private const string BlendShaderName = "VATyakov/vat_lit_vertex_blend";
        private const string PlainShaderName = "Universal Render Pipeline/Lit";
        private const int Elements = 4;
        private const int Rows = 3;

        private static readonly Vector4 _foreignLayout = new(7f, 9f, 0f, 0f);

        private Scene _scene;
        private GameObject _root;
        private Material _vat;
        private Material _otherVat;
        private Material _blend;
        private Material _plain;
        private VatAsset _asset;
        private VatMaterialCopies _copies;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            _root = NewObject("Unit");
            _vat = new Material(Shader.Find(VatBaker.DefaultShaderName)) { name = "Vat" };
            _otherVat = new Material(Shader.Find(UnlitShaderName)) { name = "OtherVat" };
            _blend = new Material(Shader.Find(BlendShaderName)) { name = "Blend" };
            _plain = new Material(Shader.Find(PlainShaderName)) { name = "Plain" };
            _asset = ScriptableObject.CreateInstance<VatAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            _copies?.Dispose();
            _copies = null;
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_vat);
            Object.DestroyImmediate(_otherVat);
            Object.DestroyImmediate(_blend);
            Object.DestroyImmediate(_plain);
            Object.DestroyImmediate(_asset);
            EditorSceneManager.ClosePreviewScene(_scene);
        }

        [Test]
        public void EveryUniqueTemplate_GetsOneCopy_InAllItsSlots()
        {
            var body = AddRenderer(_root, _vat, _plain, _vat);
            var lod = AddRenderer(Child("LOD1"), _vat);
            var weapon = AddRenderer(Child("Weapon"), _otherVat);
            var plain = AddRenderer(Child("Plain"), _plain);

            _copies = new VatMaterialCopies(_root.GetComponentsInChildren<Renderer>(true), _root.name, _asset);

            Assert.AreEqual(2, _copies.Materials.Count, "one copy per unique VAT template");
            var copy = body.sharedMaterials[0];
            Assert.AreNotSame(_vat, copy, "the slot gets a copy");
            Assert.AreSame(copy, body.sharedMaterials[2], "the same template shares the copy");
            Assert.AreSame(_plain, body.sharedMaterials[1], "a plain material stays");
            Assert.AreSame(copy, lod.sharedMaterial, "the LOD shares the copy");
            Assert.AreSame(_copies.Materials[1], weapon.sharedMaterial, "another template gets its own copy");
            Assert.AreSame(_plain, plain.sharedMaterial, "a renderer without VAT stays");
            Assert.AreEqual(HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild, copy.hideFlags, "the copy is not saved, but unloads as an unused asset");
        }

        [Test]
        public void Dispose_RestoresTheTemplates_AndDestroysTheCopies()
        {
            var body = AddRenderer(_root, _vat, _plain);
            _copies = new VatMaterialCopies(new Renderer[] { body }, _root.name, _asset);
            var copy = _copies.Materials[0];

            _copies.Dispose();

            CollectionAssert.AreEqual(new[] { _vat, _plain }, body.sharedMaterials, "templates are back");
            Assert.IsTrue(copy == null, "the copy is destroyed");
            Assert.AreEqual(0, _copies.Materials.Count, "no copies left");
        }

        [Test]
        public void AnimatorInEditMode_DoesNotTouchMaterials()
        {
            var body = AddRenderer(_root, _vat);
            var animator = _root.AddComponent<VatAnimator>();

            Assert.AreEqual(0, animator.Materials.Count, "no copies in edit mode");
            Assert.AreSame(_vat, body.sharedMaterial, "the template stays");
        }

        [Test]
        public void Copies_OfTheAssetMaterials_GetTheAssetData_OthersKeepTheirs()
        {
            var position = new Texture2D(Elements, Rows);
            var foreign = new Texture2D(Elements, Rows);
            try
            {
                var clip = new VatClip("Clip", 0, Rows, 1f, Rows - 1f, isLooping: false);
                _asset.SetData(new VatLayoutInfo(Elements, Elements, 1, Rows), null, position, null, VatPositionFormat.Half, VatPositionRange.Identity,
                    new Vector3[Rows], new[] { clip }, default, string.Empty);
                _vat.SetTexture(VatShaderIds.PositionTexture, position);
                _otherVat.SetTexture(VatShaderIds.PositionTexture, foreign);
                _otherVat.SetVector(VatShaderIds.Layout, _foreignLayout);
                var templateLayout = _vat.GetVector(VatShaderIds.Layout);
                var body = AddRenderer(_root, _vat);
                var weapon = AddRenderer(Child("Weapon"), _otherVat);

                _copies = new VatMaterialCopies(_root.GetComponentsInChildren<Renderer>(true), _root.name, _asset);

                Assert.AreEqual(_asset.Layout.ShaderLayout, body.sharedMaterial.GetVector(VatShaderIds.Layout), "a stale copy gets the asset layout");
                Assert.AreEqual(_foreignLayout, weapon.sharedMaterial.GetVector(VatShaderIds.Layout), "a material of another asset keeps its data");
                Assert.AreEqual(templateLayout, _vat.GetVector(VatShaderIds.Layout), "the template stays as it is");
            }
            finally
            {
                Object.DestroyImmediate(position);
                Object.DestroyImmediate(foreign);
            }
        }

        [Test]
        public void NestedAnimator_KeepsItsRenderers()
        {
            var body = AddRenderer(_root, _vat);
            var rider = Child("Rider");
            var riderBody = AddRenderer(rider, _otherVat);
            var riderWeapon = AddRenderer(NewObject("Weapon"), _vat);
            riderWeapon.transform.SetParent(rider.transform);
            var unit = _root.AddComponent<VatAnimator>();
            var nested = rider.AddComponent<VatAnimator>();

            CollectionAssert.AreEquivalent(new Renderer[] { body }, unit.OwnRenderers(), "the parent skips the renderers of the nested animator");
            CollectionAssert.AreEquivalent(new Renderer[] { riderBody, riderWeapon }, nested.OwnRenderers(), "the nested animator owns its subtree");
        }

        [Test]
        public void CanBlend_OnlyWhenEveryVatTemplateHasATransition()
        {
            var body = AddRenderer(_root, _blend, _plain);
            var weapon = AddRenderer(Child("Weapon"), _vat);

            _copies = new VatMaterialCopies(new Renderer[] { body }, _root.name, _asset);
            Assert.IsTrue(_copies.CanBlend, "blend template and a plain material");
            _copies.Dispose();

            _copies = new VatMaterialCopies(new Renderer[] { body, weapon }, _root.name, _asset);
            Assert.IsFalse(_copies.CanBlend, "a VAT template without a transition");
        }

        private GameObject NewObject(string name)
        {
            var target = new GameObject(name);
            SceneManager.MoveGameObjectToScene(target, _scene);
            return target;
        }

        private GameObject Child(string name)
        {
            var child = NewObject(name);
            child.transform.SetParent(_root.transform);
            return child;
        }

        private static MeshRenderer AddRenderer(GameObject target, params Material[] materials)
        {
            var renderer = target.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }
    }
}
