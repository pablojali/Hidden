using UnityEngine;

namespace Hidden.Characters
{
    // Waypoints are this object's children, in hierarchy order. Reordering,
    // adding, or removing child transforms in the Editor changes the route
    // -- no code changes needed.
    public class CharacterPath : MonoBehaviour
    {
        public int WaypointCount => transform.childCount;

        public Vector3 GetWaypointPosition(int index)
        {
            return transform.GetChild(index).position;
        }

        private void OnDrawGizmos()
        {
            var count = WaypointCount;
            if (count == 0)
            {
                return;
            }

            Gizmos.color = Color.yellow;

            for (var i = 0; i < count; i++)
            {
                var point = GetWaypointPosition(i);
                Gizmos.DrawSphere(point, 0.2f);

                if (i < count - 1)
                {
                    Gizmos.DrawLine(point, GetWaypointPosition(i + 1));
                }
            }
        }
    }
}
