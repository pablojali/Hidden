using UnityEngine;
using UnityEngine.InputSystem;

namespace Hidden.Camera
{
    // DioramaCameraController depends only on this interface, not on any
    // specific input device. A future touch-based implementation (one-finger
    // drag -> PanDelta, pinch -> ZoomDelta) can replace CameraInput on mobile
    // without the controller changing at all.
    public interface ICameraInputSource
    {
        Vector2 PanDelta { get; }
        float ZoomDelta { get; }
    }

    public class CameraInput : MonoBehaviour, ICameraInputSource
    {
        public Vector2 PanDelta { get; private set; }
        public float ZoomDelta { get; private set; }

        private Vector2 lastPointerPosition;
        private bool isPanning;

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                PanDelta = Vector2.zero;
                ZoomDelta = 0f;
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                isPanning = true;
                lastPointerPosition = mouse.position.ReadValue();
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                isPanning = false;
            }

            if (isPanning)
            {
                var currentPointerPosition = mouse.position.ReadValue();
                PanDelta = currentPointerPosition - lastPointerPosition;
                lastPointerPosition = currentPointerPosition;
            }
            else
            {
                PanDelta = Vector2.zero;
            }

            ZoomDelta = mouse.scroll.ReadValue().y;
        }
    }
}
