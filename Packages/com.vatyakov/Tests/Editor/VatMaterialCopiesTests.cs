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
        private const string PlainShaderName = "Universal Render Pipeline/Lit";
        private const string BaseColor = "_BaseColor";

        private Scene _scene;
        private GameObject _root;
        private Material _vat;
        private Material _otherVat;
        private Material _plain;
        private VatMaterialCopies _copies;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            _root = NewObject("Unit");
            _vat = new Material(Shader.Find(VatBaker.DefaultShaderName)) { name = "Vat" };
            _otherVat = new Material(Shader.Find(UnlitShaderName)) { name = "OtherVat" };
            _plain = new Material(Shader.Find(PlainShaderName)) { name = "Plain" };
        }

        [TearDown]
        public void TearDown()
        {
            _copies?.Dispose();
            _copies = null;
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_vat);
            Object.DestroyImmediate(_otherVat);
            Object.DestroyImmediate(_plain);
            EditorSceneManager.ClosePreviewScene(_scene);
        }

        [Test]
        public void EveryUniqueTemplate_GetsOneCopy_InAllItsSlots()
        {
            var body = AddRenderer(_root, _vat, _plain, _vat);
            var lod = AddRenderer(Child("LOD1"), _vat);
            var weapon = AddRenderer(Child("Weapon"), _otherVat);
            var plain = AddRenderer(Child("Plain"), _plain);

            _copies = new VatMaterialCopies(_root.GetComponentsInChildren<Renderer>(true), _root.name);

            Assert.AreEqual(2, _copies.Materials.Count, "one copy per unique VAT template");
            var copy = body.sharedMaterials[0];
            Assert.AreNotSame(_vat, copy, "the slot gets a copy");
            Assert.AreSame(copy, body.sharedMaterials[2], "the same template shares the copy");
            Assert.AreSame(_plain, body.sharedMaterials[1], "a plain material stays");
            Assert.AreSame(copy, lod.sharedMaterial, "the LOD shares the copy");
            Assert.AreSame(_copies.Materials[1], weapon.sharedMaterial, "another template gets its own copy");
            Assert.AreSame(_plain, plain.sharedMaterial, "a renderer without VAT stays");
            Assert.AreEqual(HideFlags.DontSave, copy.hideFlags, "the copy is not saved");
            CollectionAssert.AreEquivalent(new Renderer[] { body, lod, weapon }, _copies.Renderers, "only renderers with VAT");
        }

        [Test]
        public void Dispose_RestoresTheTemplates_AndDestroysTheCopies()
        {
            var body = AddRenderer(_root, _vat, _plain);
            _copies = new VatMaterialCopies(new Renderer[] { body }, _root.name);
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
            var color = _vat.GetColor(BaseColor);

            animator.SetColor(BaseColor, Color.red);

            Assert.AreEqual(0, animator.Materials.Count, "no copies in edit mode");
            Assert.AreSame(_vat, body.sharedMaterial, "the template stays");
            Assert.AreEqual(color, _vat.GetColor(BaseColor), "the template color stays");
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
