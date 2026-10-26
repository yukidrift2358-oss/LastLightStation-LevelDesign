using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LastLight;

[InitializeOnLoad]
public static class StationPresentation
{
    static double start;
    static int stage;
    static StationDirector director;
    const string SessionKey="LastLight.CapturePresentation";
    static StationPresentation()
    {
        if(SessionState.GetBool(SessionKey,false))EditorApplication.delayCall+=Resume;
    }
    public static void OpenAndCaptureV6(){StationV6Audit.Snapshot();SessionState.SetBool("LastLight.V6Extras",true);OpenAndCapture();}
    public static void OpenAndCapture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/LastLightStation.unity");
        Directory.CreateDirectory("Validation/Previews");
        SessionState.SetBool(SessionKey,true);
        var gameType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if(gameType!=null)EditorWindow.GetWindow(gameType).Focus();
        EditorApplication.isPlaying=true;
        Resume();
    }
    static void Resume()
    {
        start=EditorApplication.timeSinceStartup;stage=0;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        double elapsed=EditorApplication.timeSinceStartup-start;
        if(elapsed>45){Finish();Debug.LogWarning("Presentation capture timed out; see editor previews instead.");return;}
        if(!EditorApplication.isPlaying)return;
        if(director==null)director=UnityEngine.Object.FindObjectOfType<StationDirector>();
        if(director==null)return;
        if(stage==0&&elapsed>4)
        {
            var gameType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if(gameType!=null)EditorWindow.GetWindow(gameType).Focus();
            CaptureHeroPose("HeroIdle");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/Previews/Gameplay.png"));stage=1;start=EditorApplication.timeSinceStartup;
        }
        else if(stage==1&&elapsed>2)
        {director.player.BeginPulseCharge();stage=2;start=EditorApplication.timeSinceStartup;}
        else if(stage==2&&elapsed>1.5)
        {CaptureHeroPose("HeroCharge");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/Previews/QCharged.png"));stage=3;start=EditorApplication.timeSinceStartup;}
        else if(stage==3&&elapsed>1)
        {director.player.ReleasePulseCharge();stage=4;start=EditorApplication.timeSinceStartup;}
        else if(stage==4&&elapsed>2)
        {director.player.Attack();stage=5;start=EditorApplication.timeSinceStartup;}
        else if(stage==5&&elapsed>.18&&director.player.visual.GetComponent<StationHumanoid>().slashTrail.emitting)
        {CaptureHeroPose("HeroSlash");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/Previews/SwordSlash.png"));stage=6;start=EditorApplication.timeSinceStartup;}
        else if(stage==6&&elapsed>2)
        {director.ToggleMap();stage=7;start=EditorApplication.timeSinceStartup;}
        else if(stage==7&&elapsed>2)
        {ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/Previews/Map.png"));stage=8;start=EditorApplication.timeSinceStartup;}
        else if(stage==8&&elapsed>2)
        {
            if(!SessionState.GetBool("LastLight.V6Extras",false)){Finish();Debug.Log("LAST_LIGHT_PRESENTATION_CAPTURED");return;}
            director.ToggleMap();int[] complete={0,1,5,2,3,6,7};uint mask=0;foreach(int id in complete){director.rooms[id].MarkComplete();mask|=1u<<id;}
            // Editor-only presentation setup. The runtime route tests exercise legitimate progression separately.
            SetRuntime("TutorialStage",4);SetRuntime("Step",7);SetRuntime("CompletedMask",mask);SetRuntime("ActiveSector",7);SetRuntime("RoomEntered",true);
            director.player.Warp(director.rooms[7].entryPoint);director.player.Restore();director.TryCompleteRoom(director.rooms[7]);stage=9;start=EditorApplication.timeSinceStartup;
        }
        else if(stage==9&&elapsed>2){ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/Previews/V6RouteChoice.png"));stage=10;start=EditorApplication.timeSinceStartup;}
        else if(stage==10&&elapsed>1){director.CloseChoices();director.ToggleMap();stage=11;start=EditorApplication.timeSinceStartup;}
        else if(stage==11&&elapsed>2){ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/Previews/V6BranchMap.png"));stage=12;start=EditorApplication.timeSinceStartup;}
        else if(stage==12&&elapsed>1){SessionState.SetBool("LastLight.V6Extras",false);Finish();Debug.Log("V6_PRESENTATION_CAPTURED");}
    }

    static void SetRuntime(string name,object value){typeof(StationDirector).GetField("<"+name+">k__BackingField",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(director,value);}
    static void CaptureHeroPose(string name)
    {
        // Isolated inspection camera: restore all layers synchronously before the next frame.
        var p=director.player;var transforms=p.visual.GetComponentsInChildren<Transform>();var layers=new int[transforms.Length];
        var camera=new GameObject("HeroInspection").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.fieldOfView=38;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.01f,.02f,.035f);
        camera.transform.position=p.transform.position+p.visual.forward*3.8f+p.visual.right*2+Vector3.up*1.7f;camera.transform.LookAt(p.transform.position+Vector3.up*.95f);
        var rt=new RenderTexture(800,1000,24);var previous=RenderTexture.active;var texture=new Texture2D(800,1000,TextureFormat.RGB24,false);
        for(int i=0;i<transforms.Length;i++){layers[i]=transforms[i].gameObject.layer;transforms[i].gameObject.layer=31;}
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,800,1000),0,0);texture.Apply();File.WriteAllBytes("Validation/Previews/"+name+".png",texture.EncodeToPNG());
        }
        finally
        {
            for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];
            camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(camera.gameObject);
        }
    }
    static void Finish()
    {
        EditorApplication.update-=Tick;SessionState.SetBool(SessionKey,false);
        EditorApplication.isPlaying=false;
        EditorApplication.delayCall+=ShowOverview;
    }
    static void ShowOverview()
    {
        var scene=SceneView.lastActiveSceneView;
        if(scene!=null){scene.LookAt(new Vector3(-12,24,22),Quaternion.Euler(48,40,0),135,false,true);scene.Focus();}
    }
}
