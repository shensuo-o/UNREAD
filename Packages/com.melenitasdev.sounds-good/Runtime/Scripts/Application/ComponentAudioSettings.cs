/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    /// <summary>
    /// How the volume falls off with distance, as exposed by the no-code components. Mirrors the
    /// builder API: Logarithmic/Linear map to SetVolumeRolloffCurve, Custom to
    /// SetCustomVolumeRolloffCurve (which has no matching enum value in VolumeRolloffCurve).
    /// </summary>
    public enum ComponentRolloff { Logarithmic, Linear, Custom }

    /// <summary>
    /// Applies the shared 3D distance settings (hear distance + rolloff curve) that every no-code
    /// component exposes, so the wiring lives in one place instead of being repeated per builder.
    /// </summary>
    internal static class ComponentAudioSettings
    {
        public static void Apply (Sound s, float min, float max, ComponentRolloff rolloff, AnimationCurve curve,
            Effect effect, float effectIntensity)
        {
            s.SetHearDistance(min, max);
            if (rolloff == ComponentRolloff.Custom) s.SetCustomVolumeRolloffCurve(curve);
            else s.SetVolumeRolloffCurve(ToCurve(rolloff));
//#SG_PRO_BEGIN
            s.SetEffect(effect, effectIntensity);
//#SG_PRO_END
        }

        public static void Apply (Music m, float min, float max, ComponentRolloff rolloff, AnimationCurve curve,
            Effect effect, float effectIntensity)
        {
            m.SetHearDistance(min, max);
            if (rolloff == ComponentRolloff.Custom) m.SetCustomVolumeRolloffCurve(curve);
            else m.SetVolumeRolloffCurve(ToCurve(rolloff));
//#SG_PRO_BEGIN
            m.SetEffect(effect, effectIntensity);
//#SG_PRO_END
        }

        public static void Apply (Playlist p, float min, float max, ComponentRolloff rolloff, AnimationCurve curve,
            Effect effect, float effectIntensity)
        {
            p.SetHearDistance(min, max);
            if (rolloff == ComponentRolloff.Custom) p.SetCustomVolumeRolloffCurve(curve);
            else p.SetVolumeRolloffCurve(ToCurve(rolloff));
//#SG_PRO_BEGIN
            p.SetEffect(effect, effectIntensity);
//#SG_PRO_END
        }

//#SG_PRO_BEGIN
        public static void Apply (DynamicMusic d, float min, float max, ComponentRolloff rolloff, AnimationCurve curve,
            Effect effect, float effectIntensity)
        {
            d.SetHearDistance(min, max);
            if (rolloff == ComponentRolloff.Custom) d.SetCustomVolumeRolloffCurve(curve);
            else d.SetVolumeRolloffCurve(ToCurve(rolloff));
            d.SetEffect(effect, effectIntensity);
        }
//#SG_PRO_END

        private static VolumeRolloffCurve ToCurve (ComponentRolloff rolloff) =>
            rolloff == ComponentRolloff.Linear ? VolumeRolloffCurve.Linear : VolumeRolloffCurve.Logarithmic;
    }
}
