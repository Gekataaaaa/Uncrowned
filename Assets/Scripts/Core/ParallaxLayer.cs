using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>
    /// Moves a background layer with a fraction of the camera's movement: 1 = glued to the screen,
    /// 0 = fixed in the world. With a tile width the layer jumps by whole tiles to stay around the
    /// camera, so three side-by-side copies of a seamless image never run out.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0, 1)] public float factor = 0.5f;
        [Range(0, 1)] public float factorY;
        public float tileWidth;

        Transform cam;
        Vector3 start, camStart;

        void Start()
        {
            cam = Camera.main.transform;
            start = transform.position;
            camStart = cam.position;
        }

        void LateUpdate()
        {
            var c = cam.position;
            float x = start.x + (c.x - camStart.x) * factor;
            if (tileWidth > 0) x += Mathf.Round((c.x - x) / tileWidth) * tileWidth;
            float y = start.y + (c.y - camStart.y) * factorY;
            transform.position = new Vector3(x, y, start.z);
        }
    }
}
