using System;
using UnityEngine;
using UnityEngine.Animations;

namespace VATyakov.Editor
{
    internal static class VatConstraints
    {
        private const string RigBuilderName = "RigBuilder";

        public static string Warning(GameObject root)
        {
            var hasConstraints = root.GetComponentsInChildren<IConstraint>(true).Length > 0;
            var hasRig = Array.Exists(root.GetComponentsInChildren<MonoBehaviour>(true), IsRigBuilder);
            return hasConstraints || hasRig ? $"'{root.name}' has constraints or Animation Rigging: the bake plays the clips without them." : null;
        }

        private static bool IsRigBuilder(MonoBehaviour behaviour)
        {
            return behaviour != null && behaviour.GetType().Name == RigBuilderName;
        }
    }
}
