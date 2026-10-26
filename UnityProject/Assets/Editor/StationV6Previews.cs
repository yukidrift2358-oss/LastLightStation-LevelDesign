using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;
using LastLight;
public static partial class StationWhiteboxBuilder {
 public static void GenerateBuildAndCaptureV6(){GenerateAndBuild();CaptureCurrentV6();}
 public static void CaptureV6(){Generate();CaptureCurrentV6();}
 public static void CaptureCurrentV6(){EditorSceneManager.OpenScene(ScenePath);director=Object.FindObjectOfType<StationDirector>();root=GameObject.Find("STATION_WHITEBOX").transform;Directory.CreateDirectory("Validation/Previews");
  var camera=new GameObject("V6PreviewCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.008f,.014f,.025f);camera.farClipPlane=1400;
  Capture(camera,new Vector3(180,125,190),new Vector3(-12,24,20),"V6Overview",true,110);
  var shaft=director.passages[0];foreach(var p in director.passages)if(p.Spec.kind==PassageKind.Stair){shaft=p;break;}
  foreach(var p in director.passages)if(p.Spec.kind==PassageKind.Stair)foreach(Transform t in p.transform)if(t.name=="ShaftSideWall"||t.name=="ShaftInnerWall")t.GetComponent<Renderer>().enabled=false;
  Capture(camera,shaft.transform.TransformPoint(new Vector3(18,17,-18)),shaft.transform.TransformPoint(new Vector3(0,12,0)),"V6StairCutaway",false);
  var clones=new System.Collections.Generic.List<GameObject>();
  for(int n=0;n<3;n++){GameObject source=null;if(n==2)source=director.rooms[4].scanner.gameObject;else foreach(var i in director.rooms[n==0?0:1].GetComponentsInChildren<StationInteractable>())if(i.kind==(n==0?InteractionKind.Power:InteractionKind.Seal)){source=i.gameObject;break;}
   var clone=Object.Instantiate(source);clone.transform.SetParent(null);clone.transform.position=new Vector3((n-1)*2f,n==2?1.3f:1.1f,0);clone.transform.rotation=Quaternion.identity;
   foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
   foreach(var c in clone.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
   foreach(var r in clone.GetComponentsInChildren<Renderer>())if(r.name=="WarningVolume")r.enabled=false;
   clones.Add(clone);
  }
  camera.cullingMask=1<<31;RenderSettings.ambientLight=new Color(.6f,.65f,.7f);Capture(camera,new Vector3(0,2.2f,-7),new Vector3(0,1.2f,0),"V6DeviceSilhouettes",true,1.8f);foreach(var c in clones)Object.DestroyImmediate(c);Object.DestroyImmediate(camera.gameObject);Debug.Log("V6_PREVIEWS_CAPTURED");
 }
}
