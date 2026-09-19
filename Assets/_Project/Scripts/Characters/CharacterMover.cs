using UnityEngine;

namespace Hidden.Characters
{
    // Autonomous waypoint follower. Deliberately knows nothing about input,
    // camera, discovery, learning, or rewards -- it only reads a
    // CharacterPath and moves/rotates this transform. The player never
    // controls this directly, so the same behavior runs identically on PC,
    // Android, and iOS.
    public class CharacterMover : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Turning,
            Walking
        }

        [SerializeField] private CharacterPath path;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float pauseDuration = 2f;
        [SerializeField] private float arrivalThreshold = 0.15f;
        [SerializeField] private float turnThresholdDegrees = 5f;
        [SerializeField] private bool loop = true;

        public State CurrentState { get; private set; } = State.Idle;
        public float MoveSpeed => moveSpeed;
        public float PauseDuration => pauseDuration;
        public float ArrivalThreshold => arrivalThreshold;
        public bool Loop => loop;

        private int targetWaypointIndex;
        private float idleTimer;
        private bool routeFinished;

        public void SetPath(CharacterPath newPath)
        {
            path = newPath;
        }

        private void Start()
        {
            Initialize();
        }

        // Exposed so both Unity's own Start() and EditMode tests (which
        // never run Start automatically outside Play Mode) can trigger the
        // same initialization deterministically.
        public void Initialize()
        {
            if (path == null || path.WaypointCount == 0)
            {
                enabled = false;
                return;
            }

            routeFinished = false;
            transform.position = path.GetWaypointPosition(0);
            targetWaypointIndex = path.WaypointCount > 1 ? 1 : 0;
            EnterIdle();
        }

        private void Update()
        {
            if (routeFinished)
            {
                return;
            }

            switch (CurrentState)
            {
                case State.Idle:
                    TickIdle();
                    break;
                case State.Turning:
                    TickTurning();
                    break;
                case State.Walking:
                    TickWalking();
                    break;
            }
        }

        private void EnterIdle()
        {
            CurrentState = State.Idle;
            idleTimer = pauseDuration;
        }

        private void TickIdle()
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                CurrentState = State.Turning;
            }
        }

        private void TickTurning()
        {
            var toTarget = FlatDirectionTo(path.GetWaypointPosition(targetWaypointIndex));

            if (toTarget == Vector3.zero)
            {
                CurrentState = State.Walking;
                return;
            }

            var targetRotation = Quaternion.LookRotation(toTarget);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            if (Quaternion.Angle(transform.rotation, targetRotation) <= turnThresholdDegrees)
            {
                CurrentState = State.Walking;
            }
        }

        private void TickWalking()
        {
            var targetPosition = path.GetWaypointPosition(targetWaypointIndex);
            var toTarget = targetPosition - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= arrivalThreshold)
            {
                AdvanceWaypoint();
                return;
            }

            var direction = toTarget.normalized;
            var targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            transform.position += direction * moveSpeed * Time.deltaTime;
        }

        private Vector3 FlatDirectionTo(Vector3 worldPosition)
        {
            var direction = worldPosition - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude < 0.0001f ? Vector3.zero : direction.normalized;
        }

        private void AdvanceWaypoint()
        {
            var nextIndex = targetWaypointIndex + 1;

            if (nextIndex >= path.WaypointCount)
            {
                if (!loop)
                {
                    routeFinished = true;
                    CurrentState = State.Idle;
                    return;
                }

                nextIndex = 0;
            }

            targetWaypointIndex = nextIndex;
            EnterIdle();
        }
    }
}
