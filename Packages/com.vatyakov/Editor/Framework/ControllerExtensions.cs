using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal static class ControllerExtensions
    {
        public static void Deactivate(this IReadOnlyList<IController> controllers)
        {
            for (var i = controllers.Count - 1; i >= 0; i--)
            {
                controllers[i].Deactivate();
            }
        }

        public static void Activate(this IReadOnlyList<IController> controllers)
        {
            foreach (var controller in controllers)
            {
                controller.Activate();
            }
        }
    }
}
