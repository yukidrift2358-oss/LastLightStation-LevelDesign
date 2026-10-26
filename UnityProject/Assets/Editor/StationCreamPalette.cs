using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using LastLight;

// A presentation pass over the saved scene. It never regenerates gameplay geometry.
public static class StationCreamPalette
{
    const string Scene="Assets/Scenes/LastLightStation.unity";
    const string Folder="Assets/Generated/Materials/";
    static readonly Color Wall=new Color(.93f,.91f,.85f);
    static readonly Color Floor=new Color(.84f,.82f,.76f);
    static readonly Color Trim=new Color(.50f,.49f,.45f);
    static readonly Color Brass=new Color(.55f,.43f,.27f);
    static readonly Color Teal=new Color(.24f,.43f,.44f);
    [MenuItem("Last Light/Palette/Apply ivory and limestone")]
    public static void ApplySavedScene()
    {
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EditorSceneManager.OpenScene(Scene);
        ApplyToOpenScene();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("CREAM_PALETTE_SAVED");
    }
    public static void ApplyToOpenScene()
    {
        string before=Fingerprint();var counts=JsonUtility.ToJson(StationV6Audit.Inspect());
        Set("Structure",Wall);Set("Deck",Floor);Set("DarkTrim",Trim);
        Set("CircuitBlue",Teal,true);Set("MemoryAmber",Brass,true);
        Set("WarningRed",new Color(.65f,.22f,.15f),true);
        Set("DeadPlants",new Color(.37f,.38f,.29f));
        var walkway=Set("CreamWalkSurface",new Color(.88f,.86f,.80f));
        var props=Set("CreamEquipment",new Color(.87f,.85f,.78f));
        foreach(var surface in UnityEngine.Object.FindObjectsOfType<StationWalkSurface>(true))
        {var r=surface.GetComponent<Renderer>();if(r)r.sharedMaterial=walkway;}
        foreach(var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
        {
            var materials=renderer.sharedMaterials;bool changed=false;
            for(int i=0;i<materials.Length;i++)if(materials[i] && (materials[i].name.StartsWith("SciFi_")||materials[i].name=="CreamMachineShell"))
            {
                if(renderer.GetComponentInParent<StationEnemy>()!=null)
                {
                    bool large=false;for(var tr=renderer.transform;tr!=null;tr=tr.parent)if(tr.name.Contains("Trilobite"))large=true;
                    var original=AssetDatabase.LoadAssetAtPath<Material>(Folder+(large?"SciFi_T_Enemies_Large":"SciFi_T_Enemies")+".mat");
                    if(!original||!original.mainTexture)throw new Exception("Missing original mechanical texture");
                    original.color=new Color(.51f,.53f,.49f);original.SetFloat("_Glossiness",.24f);EditorUtility.SetDirty(original);
                    materials[i]=original;
                }
                else materials[i]=props;
                changed=true;
            }
            if(changed)renderer.sharedMaterials=materials;
        }
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.48f,.46f,.42f);
        foreach(var light in UnityEngine.Object.FindObjectsOfType<Light>(true))
        {
            if(light.type==LightType.Directional){light.color=new Color(1,.97f,.90f);light.intensity=.9f;}
            else{light.color=new Color(1,.95f,.84f);light.intensity=Mathf.Min(light.intensity,1.05f);}
        }
        foreach(var text in UnityEngine.Object.FindObjectsOfType<TextMesh>(true))text.color=new Color(.26f,.29f,.28f);
        if(before!=Fingerprint()||counts!=JsonUtility.ToJson(StationV6Audit.Inspect()))throw new Exception("Palette pass changed geometry or encounter counts");
        StationV6Audit.ValidateCounts();
        Directory.CreateDirectory("Validation");
        File.WriteAllText("Validation/cream-palette-audit.json","{\"geometryAndCollidersUnchanged\":true,\"perRoomCountsUnchanged\":true,\"walls\":\"ivory\",\"walkSurfaces\":\"limestone\",\"equipment\":\"warm white\",\"lighting\":\"neutral warm\"}");
        Debug.Log("CREAM_PALETTE_GEOMETRY_AND_COUNTS_UNCHANGED");
    }
    static Material Set(string name,Color color,bool emission=false)
    {
        string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Metallic",.06f);m.SetFloat("_Glossiness",.18f);
        if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.35f);}
        else{m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}
        EditorUtility.SetDirty(m);return m;
    }
    static string Fingerprint()
    {
        var b=new StringBuilder();
        foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            b.Append(t.name).Append(t.localPosition.ToString("R")).Append(t.localRotation.ToString("R")).Append(t.localScale.ToString("R"));
            foreach(var c in t.GetComponents<Collider>())b.Append(EditorJsonUtility.ToJson(c));
        }
        return b.ToString();
    }
    public static void ApplyAndBuild()
    {
        ApplySavedScene();Directory.CreateDirectory("Builds/CreamWindows");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/CreamWindows/LastLightStation.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        File.WriteAllText("Validation/cream-build-result.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Cream Windows build failed");
        Debug.Log("CREAM_WINDOWS_BUILD_SUCCEEDED");
    }
}
