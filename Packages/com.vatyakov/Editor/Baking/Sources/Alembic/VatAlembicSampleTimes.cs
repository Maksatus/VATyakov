#if VAT_ALEMBIC
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Formats.Alembic.Importer;

namespace VATyakov.Editor
{
    internal static class VatAlembicSampleTimes
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static double[] Read(AlembicStreamPlayer player)
        {
            try
            {
                return ReadContext(Get(Get(player, "abcStream"), "abcContext"), player.StartTime);
            }
            catch (Exception e) when (e is NullReferenceException or TargetInvocationException or InvalidCastException or AmbiguousMatchException)
            {
                return null;
            }
        }

        private static double[] ReadContext(object context, double start)
        {
            var times = new SortedSet<double>();
            var samplingCount = (int)Get(context, "timeSamplingCount");
            for (var index = 0; index < samplingCount; index++)
            {
                var sampling = Call(context, "GetTimeSampling", index);
                var sampleCount = (int)Get(sampling, "sampleCount");
                for (var sample = 0; sample < sampleCount; sample++)
                {
                    times.Add(Convert.ToDouble(Call(sampling, "GetTime", sample)) - start);
                }
            }

            var result = new double[times.Count];
            times.CopyTo(result);
            return result;
        }

        private static object Get(object target, string name)
        {
            return target.GetType().GetProperty(name, Members).GetValue(target);
        }

        private static object Call(object target, string name, int argument)
        {
            return target.GetType().GetMethod(name, Members).Invoke(target, new object[] { argument });
        }
    }
}
#endif
