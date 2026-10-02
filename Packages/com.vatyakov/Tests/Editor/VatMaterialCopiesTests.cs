using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    public class VatMaterialCopiesTests
    {
        private GameObject _root;
        private Material _vat;
        private Material _otherVat;
        private Material _plain;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Unit");
            _vat = new Material(Shader.Find("VATyakov/vat_lit_vertex")) { name = "Vat" };
            _otherVat = new Material(Shader.Find("VATyakov/vat_unlit_vertex")) { name = "OtherVat" };
            _plain = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Plain" };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_vat);
            Object.DestroyImmediate(_otherVat);
            Object.DestroyImmediate(_plain);
        }

        [Test]
        public void EveryUniqueTemplate_GetsOneCopy_InAllItsSlots()
        {
            var body = Renderer(_root, _vat, _plain, _vat);
            var lod = Renderer(Child("LOD1"), _vat);
            var weapon = Renderer(Child("Weapon"), _otherVat);
            var plain = Renderer(Child("Plain"), _plain);

            var copies = new VatMaterialCopies(_root.GetComponentsInChildren<Renderer>(true), _root.name);

            Assert.AreEqual(2, copies.Materials.Count);
            var copy = body.sharedMaterials[0];
            Assert.AreNotSame(_vat, copy);
            Assert.AreSame(copy, body.sharedMaterials[2]);
            Assert.AreSame(_plain, body.sharedMaterials[1]);
            Assert.AreSame(copy, lod.sharedMaterial);
            Assert.AreSame(copies.Materials[1], weapon.sharedMaterial);
            Assert.AreSame(_plain, plain.sharedMaterial);
            Assert.AreEqual(HideFlags.DontSave, copy.hideFlags);
            CollectionAssert.AreEquivalent(new Renderer[] { body, lod, weapon }, copies.Renderers);
            copies.Dispose();
        }

        [Test]
        public void Dispose_RestoresTheTemplates_AndDestroysTheCopies()
        {
            var body = Renderer(_root, _vat, _plain);
            var copies = new VatMaterialCopies(new Renderer[] { body }, _root.name);
            var copy = copies.Materials[0];

            copies.Dispose();

            CollectionAssert.AreEqual(new[] { _vat, _plain }, body.sharedMaterials);
            Assert.IsTrue(copy == null);
            Assert.AreEqual(0, copies.Materials.Count);
        }

        [Test]
        public void AnimatorInEditMode_DoesNotTouchMaterials()
        {
            var body = Renderer(_root, _vat);
            var animator = _root.AddComponent<VatAnimator>();
            var color = _vat.GetColor("_BaseColor");

            animator.SetColor("_BaseColor", Color.red);

            Assert.AreEqual(0, animator.Materials.Count);
            Assert.AreSame(_vat, body.sharedMaterial);
            Assert.AreEqual(color, _vat.GetColor("_BaseColor"));
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(_root.transform);
            return child;
        }

        private static MeshRenderer Renderer(GameObject target, params Material[] materials)
        {
            var renderer = target.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }
    }
}
