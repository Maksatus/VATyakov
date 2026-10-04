using System;
using UnityEngine;

namespace VATyakov.Editor
{
    [Serializable]
    public sealed class VatExtraRenderer
    {
        [Tooltip("Skinned Mesh Renderer on bones of the source rig with the same bindposes (LOD, another skin), " +
                 "or Mesh Renderer under a bone of the rig (equipment): it follows that bone.")]
        [SerializeField]
        private Renderer _renderer;

        [Tooltip("Optional. VAT material of this mesh: every bake writes the bone texture into it and gives it a Bone shader. " +
                 "Empty: the mesh plays on the template material of the profile.")]
        [SerializeField]
        private Material _material;

        public Renderer Renderer { get => _renderer; set => _renderer = value; }
        public Material Material { get => _material; set => _material = value; }
    }
}
