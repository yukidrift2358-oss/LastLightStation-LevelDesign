using UnityEngine;

namespace LastLight
{
    public sealed class StationGate : MonoBehaviour
    {
        public Transform leaf;
        public bool IsOpen { get; private set; }
        Vector3 closedPosition;
        void Awake() { closedPosition=leaf.localPosition; }
        public void SetOpen(bool open)
        {
            IsOpen=open;
            // A retracted leaf also releases its collider; opening is never an invisible wall.
            leaf.GetComponent<Collider>().enabled=!open;
        }
        void Update() { leaf.localPosition=Vector3.MoveTowards(leaf.localPosition,closedPosition+(IsOpen?Vector3.up*5.5f:Vector3.zero),Time.deltaTime*7); }
    }
}
