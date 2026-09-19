using UnityEngine;

namespace Hidden.Camera
{
    // Sits on the camera rig (this GameObject's own Transform is the pivot
    // that pans); the actual Camera is a child offset at a fixed elevated
    // angle. Zoom dollies that child along the offset direction instead of
    // changing field of view, which keeps the diorama's perspective
    // consistent at any zoom level.
    [RequireComponent(typeof(CameraInput))]
    public class DioramaCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        [Header("Angle")]
        [SerializeField] private float cameraAngle = 50f;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 0.015f;
        [SerializeField] private float panSmoothing = 8f;
        [SerializeField] private Rect panBounds = new Rect(-7f, -7f, 14f, 14f);

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 1f;
        [SerializeField] private float zoomSmoothing = 10f;
        [SerializeField] private float minZoomDistance = 8f;
        [SerializeField] private float maxZoomDistance = 95f;

        private ICameraInputSource cameraInput;
        private Vector3 offsetDirection;
        private Vector3 targetPosition;
        private float targetDistance;
        private float currentDistance;

        private void Awake()
        {
            cameraInput = GetComponent<CameraInput>();

            if (cameraTransform == null)
            {
                var mainCamera = UnityEngine.Camera.main;
                cameraTransform = mainCamera != null ? mainCamera.transform : null;
            }

            offsetDirection = (Quaternion.Euler(cameraAngle, 0f, 0f) * Vector3.back).normalized;

            targetPosition = ClampToBounds(transform.position);
            transform.position = targetPosition;

            var startingDistance = cameraTransform != null
                ? cameraTransform.localPosition.magnitude
                : (minZoomDistance + maxZoomDistance) * 0.5f;
            currentDistance = Mathf.Clamp(startingDistance, minZoomDistance, maxZoomDistance);
            targetDistance = currentDistance;

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(cameraAngle, 0f, 0f);
            }

            ApplyCameraOffset();
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                return;
            }

            targetPosition = ClampToBounds(targetPosition - new Vector3(cameraInput.PanDelta.x, 0f, cameraInput.PanDelta.y) * panSpeed);
            transform.position = Vector3.Lerp(transform.position, targetPosition, panSmoothing * Time.deltaTime);

            targetDistance = Mathf.Clamp(targetDistance - cameraInput.ZoomDelta * zoomSpeed, minZoomDistance, maxZoomDistance);
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSmoothing * Time.deltaTime);

            ApplyCameraOffset();
        }

        private void ApplyCameraOffset()
        {
            cameraTransform.localPosition = offsetDirection * currentDistance;
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, panBounds.xMin, panBounds.xMax);
            position.z = Mathf.Clamp(position.z, panBounds.yMin, panBounds.yMax);
            position.y = transform.position.y;
            return position;
        }
    }
}
