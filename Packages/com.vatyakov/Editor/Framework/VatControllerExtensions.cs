using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal static class VatControllerExtensions
    {
        public static void Deactivate(this IReadOnlyList<IVatController> controllers)
        {
            for (var i = controllers.Count - 1; i >= 0; i--)
            {
                controllers[i].Deactivate();
            }
        }

        public static void Activate(this IReadOnlyList<IVatController> controllers)
        {
            foreach (var controller in controllers)
            {
                controller.Activate();
            }
        }
    }
}
