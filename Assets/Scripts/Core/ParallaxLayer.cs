using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>Moves a background layer with a fraction of the camera's X movement.</summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0, 1)] public float factor = 0.5f;

        Transform cam;
        float startX;

        void Start()
        {
            cam = Camera.main.transform;
            startX = transform.position.x;
        }

        void LateUpdate()
        {
            var p = transform.position;
            transform.position = new Vector3(startX + cam.position.x * factor, p.y, p.z);
        }
    }
}
