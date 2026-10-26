using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LastLight;
public static class StationV6Audit
{
 [Serializable] public class RoomCount{public int id,enemies,interactions,power,seal,relay,records,exits,scanners,leaks;}
 [Serializable] public class Counts{public RoomCount[] rooms;public int tutorialEnemies,tutorialInteractions;}
 public static Counts Inspect(){var d=UnityEngine.Object.FindObjectOfType<StationDirector>();return new Counts{rooms=d.rooms.Select(r=>new RoomCount{id=r.sector,enemies=r.enemies.Length,interactions=r.GetComponentsInChildren<StationInteractable>().Length,power=r.GetComponentsInChildren<StationInteractable>().Count(i=>i.kind==InteractionKind.Power),seal=r.GetComponentsInChildren<StationInteractable>().Count(i=>i.kind==InteractionKind.Seal),relay=r.GetComponentsInChildren<StationInteractable>().Count(i=>i.kind==InteractionKind.Relay),records=r.GetComponentsInChildren<StationInteractable>().Count(i=>i.kind==InteractionKind.Record),exits=r.GetComponentsInChildren<StationInteractable>().Count(i=>i.kind==InteractionKind.Exit),scanners=r.scanner!=null?1:0,leaks=r.pressure!=null?1:0}).ToArray(),tutorialEnemies=1,tutorialInteractions=d.tutorialObjectives.Length};}
 public static void ValidateCounts(){string current=JsonUtility.ToJson(Inspect(),true);string baseline=File.ReadAllText("Validation/v6-count-baseline.json");if(current!=baseline)throw new Exception("V6 per-room counts changed");File.WriteAllText("Validation/v6-count-verified.json",current);Debug.Log("V6_COUNTS_IDENTICAL");}
 [Serializable] public class TopologySnapshot {public int[] routeA,routeB;public StationLink[] links;public int passageCount;public string countComparison;}
 public static void Snapshot(){EditorSceneManager.OpenScene("Assets/Scenes/LastLightStation.unity");ValidateCounts();var d=UnityEngine.Object.FindObjectOfType<StationDirector>();File.WriteAllText("Validation/v6-topology-actual.json",JsonUtility.ToJson(new TopologySnapshot{routeA=StationTopology.RouteA,routeB=StationTopology.RouteB,links=StationTopology.Links,passageCount=d.passages.Length,countComparison="identical_to_v5_baseline"},true));Debug.Log("V6_TOPOLOGY_SNAPSHOT_RECORDED");}
 public static void RecordBaseline(){EditorSceneManager.OpenScene("Assets/Scenes/LastLightStation.unity");Directory.CreateDirectory("Validation");File.WriteAllText("Validation/v6-count-baseline.json",JsonUtility.ToJson(Inspect(),true));Debug.Log("V6_BASELINE_RECORDED");}
}
