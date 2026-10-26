using UnityEngine;
namespace LastLight {
 public sealed class StationPassage:MonoBehaviour {
  public int linkIndex; public Vector3[] points; public StationGate[] gates; public StationLift lift;
  public StationLink Spec {get{return StationTopology.Links[linkIndex];}}
  public Vector3[] Path(int from){var copy=(Vector3[])points.Clone();if(from!=Spec.a)System.Array.Reverse(copy);return copy;}
  public void Unlock(){if(gates!=null)foreach(var g in gates)g.SetOpen(true);if(lift!=null)lift.Unlock();}
 }
}