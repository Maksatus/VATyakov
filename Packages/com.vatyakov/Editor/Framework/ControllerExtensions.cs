using System.Collections.Generic;

namespace VATyakov.Editor
{
    static class ControllerExtensions
    {
        public static void Activate(this IReadOnlyList<IController> controllers)
        {
            foreach (var controller in controllers)
                controller.Activate();
        }

        public static void Deactivate(this IReadOnlyList<IController> controllers)
        {
            for (int i = controllers.Count - 1; i >= 0; i--)
                controllers[i].Deactivate();
        }
    }
}
