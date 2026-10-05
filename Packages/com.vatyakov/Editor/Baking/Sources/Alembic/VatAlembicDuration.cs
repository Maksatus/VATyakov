#if VAT_ALEMBIC
using System;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

namespace VATyakov.Editor
{
    internal static class VatAlembicDuration
    {
        public static float Require(GameObject alembic)
        {
            var player = alembic.GetComponent<AlembicStreamPlayer>();
            if (player == null)
            {
                throw new VatBakeException($"'{alembic.name}' is not an Alembic: no AlembicStreamPlayer on the root. Assign an .abc from the project.");
            }

            var duration = player.Duration;
            if (!float.IsFinite(duration) || duration <= 0f)
            {
                throw new VatBakeException(
                    FormattableString.Invariant($"'{alembic.name}' has a duration of {duration} s, nothing to bake. Check the Time Range of the .abc."));
            }

            return duration;
        }
    }
}
#endif
