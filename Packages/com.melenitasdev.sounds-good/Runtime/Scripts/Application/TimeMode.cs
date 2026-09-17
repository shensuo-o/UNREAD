/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

namespace MelenitasDev.SoundsGood
{
    /// <summary>
    /// Whether an audio's playback speed follows <c>Time.timeScale</c> (Scaled) or ignores it
    /// (Unscaled). Scaled makes a sound speed up and finish sooner in fast motion, and slow down in
    /// slow motion, so it stays in sync with game time — at <c>timeScale</c> 0 (pause) it freezes.
    /// Unscaled keeps it at its normal speed regardless of the time scale, which is what you want for
    /// menus, UI, or music that must keep playing while the game is paused.
    /// </summary>
    public enum TimeMode
    {
        Scaled,
        Unscaled
    }
}
