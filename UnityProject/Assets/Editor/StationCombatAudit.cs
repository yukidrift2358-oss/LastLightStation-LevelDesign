using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LastLight;
public static class StationCombatAudit
{
 public static void Baseline()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/LastLightStation.unity");Physics.SyncTransforms();
  var director=Object.FindObjectOfType<StationDirector>();var room=director.rooms[8];var enemy=room.enemies[1];
  string report="V3 eye drone physics baseline\n";
  foreach(float z in new[]{6f,0f,-3f})
  {
   Vector3 target=room.transform.TransformPoint(new Vector3(4,.15f,z));
   bool blocked=Physics.Linecast(enemy.transform.position+Vector3.up,target+Vector3.up,out var hit,1,QueryTriggerInteraction.Ignore);
   report+="ground local z="+z+" source="+enemy.transform.position+" blocked="+blocked+" collider="+(blocked?hit.collider.name:"none")+"\n";
  }
  Directory.CreateDirectory("Validation");File.WriteAllText("Validation/v4-enemy-baseline-valid.txt",report);Debug.Log(report);
 }
}
