using UnityEngine;
using UnityEngine.InputSystem;

namespace Hidden.Camera
{
    // DioramaCameraController depends only on this interface, not on any
    // specific input device.
    public interface ICameraInputSource
    {
        Vector2 PanDelta { get; }
        float ZoomDelta { get; }
    }

    // Reads mouse (Editor) and touch (device) in the same component and
    // exposes one unified pan/zoom delta, so DioramaCameraController works
    // unchanged on either platform: one-finger drag mirrors left-click
    // drag, pinch mirrors scroll.
    public class CameraInput : MonoBehaviour, ICameraInputSource
    {
        [SerializeField] private float pinchZoomScale = 0.02f;

        public Vector2 PanDelta { get; private set; }
        public float ZoomDelta { get; private set; }

        private Vector2 lastMousePosition;
        private bool isMousePanning;

        private Vector2 lastTouchPosition;
        private bool isTouchPanning;
        private float lastPinchDistance;
        private bool isPinching;

        private void Update()
        {
            PanDelta = Vector2.zero;
            ZoomDelta = 0f;

            ReadMouse();
            ReadTouch();
        }

        private void ReadMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                isMousePanning = true;
                lastMousePosition = mouse.position.ReadValue();
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                isMousePanning = false;
            }

            if (isMousePanning)
            {
                var currentPosition = mouse.position.ReadValue();
                PanDelta += currentPosition - lastMousePosition;
                lastMousePosition = currentPosition;
            }

            ZoomDelta += mouse.scroll.ReadValue().y;
        }

        private void ReadTouch()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            var activeTouchCount = 0;
            foreach (var touch in touchscreen.touches)
            {
                if (touch.press.isPressed)
                {
                    activeTouchCount++;
                }
            }

            if (activeTouchCount >= 2)
            {
                isTouchPanning = false;

                var distance = Vector2.Distance(
                    touchscreen.touches[0].position.ReadValue(),
                    touchscreen.touches[1].position.ReadValue());

                if (isPinching)
                {
                    ZoomDelta += (distance - lastPinchDistance) * pinchZoomScale;
                }

                lastPinchDistance = distance;
                isPinching = true;
                return;
            }

            isPinching = false;

            if (activeTouchCount != 1)
            {
                isTouchPanning = false;
                return;
            }

            var currentTouchPosition = touchscreen.touches[0].position.ReadValue();

            if (!isTouchPanning)
            {
                isTouchPanning = true;
                lastTouchPosition = currentTouchPosition;
            }

            PanDelta += currentTouchPosition - lastTouchPosition;
            lastTouchPosition = currentTouchPosition;
        }
    }
}
