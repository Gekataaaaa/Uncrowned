using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>
    /// Horizontal limits of where characters may walk. A combat zone narrows them
    /// to lock the player in until the wave is cleared.
    /// </summary>
    public static class Arena
    {
        public const int CharacterLayer = 8;
        public const float GroundY = 0f;

        public static float MinX;
        public static float MaxX;

        public static float ClampX(float x, float margin = 0.5f) => Mathf.Clamp(x, MinX + margin, MaxX - margin);
    }
}
