using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LastLight;

// Presentation only: temporary cameras and render visibility, never saves the scene.
public static class StationVideoCapture
{
    const int Width=1920, Height=1080, Fps=12;
    static Camera cam;
    static RenderTexture target;
    static Texture2D pixels;
    static string output;
    [MenuItem("Last Light/Video/Capture spatial presentation")]
    public static void Capture()
    { CaptureAt("../output/长夜泊站视频介绍/镜头帧"); }
    [MenuItem("Last Light/Video/Capture cream presentation")]
    public static void CaptureCream()
    { CaptureAt("../output/长夜泊站_素白版/场景镜头帧"); }
    static void CaptureAt(string folder)
    {
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            output=Path.GetFullPath(folder);
            Directory.CreateDirectory(output);
            EditorSceneManager.OpenScene("Assets/Scenes/LastLightStation.unity");
            cam=new GameObject("TemporaryPresentationCamera").AddComponent<Camera>();
            cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.008f,.014f,.025f);
            cam.farClipPlane=1400;cam.nearClipPlane=.08f;cam.fieldOfView=56;
            target=new RenderTexture(Width,Height,24);target.Create();cam.targetTexture=target;
            pixels=new Texture2D(Width,Height,TextureFormat.RGB24,false);
            var d=UnityEngine.Object.FindObjectOfType<StationDirector>();
            Shot("01_Overview",60,t=> {float a=(34+t*22)*Mathf.Deg2Rad;return new Vector3(Mathf.Sin(a)*250,150,Mathf.Cos(a)*250);},t=>new Vector3(0,24,18),true,113);
            Shot("02_Floors",48,t=>new Vector3(185,65+t*50,160),t=>new Vector3(0,24,0),true,100);
            var cutaway=new System.Collections.Generic.Dictionary<Renderer,bool>();
            foreach(var p in d.passages) if(p.Spec.kind==PassageKind.Stair)
                foreach(Transform c in p.transform) if(c.name=="ShaftSideWall"||c.name=="ShaftInnerWall") {var r=c.GetComponent<Renderer>();if(r){cutaway[r]=r.enabled;r.enabled=false;}}
            var stair=Array.Find(d.passages,p=>p.Spec.kind==PassageKind.Stair);
            Shot("03_StairCutaway",72,t=>stair.transform.TransformPoint(new Vector3(16-t*5,10+t*10,-18)),t=>stair.transform.TransformPoint(new Vector3(0,7+t*10,0)));
            foreach(var item in cutaway)item.Key.enabled=item.Value;
            Route("04_RouteA",StationTopology.RouteA);
            Route("05_RouteB",StationTopology.RouteB);
            var bridge=d.bridges[1];var rotation=bridge.rotation;
            Shot("06_BridgeReorientation",60,t=>new Vector3(-34,42,-30),t=>{bridge.rotation=Quaternion.Euler(0,StationPlan.Angle(7)+t*120,0);return new Vector3(0,24,0);});
            bridge.rotation=rotation;
            Shot("07_HighGround",60,t=>StationPlan.Point(8,38,-13+t*8,8),t=>StationPlan.Point(8,49,5,3));
            Shot("08_Rest",60,t=>StationPlan.Point(7,37,-8+t*6,4),t=>StationPlan.Point(7,48,0,1.5f));
            Shot("09_Boss",60,t=>d.boss.transform.position+new Vector3(-28+t*12,16,-30),t=>d.boss.transform.position+Vector3.up*2);
            Shot("10_Teaching",48,t=>new Vector3(-2+t*4,3.8f,129),t=>new Vector3(0,1.5f,113));
            var clones=new System.Collections.Generic.List<GameObject>();
            for(int n=0;n<3;n++)
            {
                GameObject source=null;
                if(n==2)source=d.rooms[4].scanner.gameObject;
                else foreach(var item in d.rooms[n==0?0:1].GetComponentsInChildren<StationInteractable>())
                    if(item.kind==(n==0?InteractionKind.Power:InteractionKind.Seal)){source=item.gameObject;break;}
                var clone=UnityEngine.Object.Instantiate(source);clone.transform.SetParent(null);clone.transform.position=new Vector3((n-1)*2f,n==2?1.3f:1.1f,0);clone.transform.rotation=Quaternion.identity;
                foreach(var tr in clone.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=31;
                foreach(var col in clone.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(col);
                foreach(var ren in clone.GetComponentsInChildren<Renderer>())if(ren.name=="WarningVolume")ren.enabled=false;
                clones.Add(clone);
            }
            cam.cullingMask=1<<31;cam.backgroundColor=new Color(.955f,.945f,.915f);RenderSettings.ambientLight=new Color(.64f,.62f,.57f);
            Shot("11_Devices",48,t=>new Vector3(-.2f+t*.4f,2.2f,-7),t=>new Vector3(0,1.2f,0),true,1.8f);
            foreach(var clone in clones)UnityEngine.Object.DestroyImmediate(clone);
            File.WriteAllText(Path.Combine(output,"capture.json"),"{\"fps\":12,\"width\":1920,\"height\":1080,\"type\":\"Editor camera render; no gameplay simulation\",\"routeFramesPerRoom\":24,\"sceneSaved\":false}");
            Debug.Log("STATION_VIDEO_CAPTURE_COMPLETE "+output);
        }
        finally
        {
            if(cam)UnityEngine.Object.DestroyImmediate(cam.gameObject);
            if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}
            if(pixels)UnityEngine.Object.DestroyImmediate(pixels);
            RenderTexture.active=null;
            if(!Application.isBatchMode && Array.Exists(previous,s=>s.isLoaded && s.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
    static void Route(string name,int[] ids)
    {
        // An exploded camera tour, not a path which a player could walk.
        Shot(name,ids.Length*24,t=>{int k=Mathf.Min(ids.Length-1,(int)(t*ids.Length));float f=t*ids.Length-k;return StationPlan.Point(ids[k],35,-9+f*5,7);},t=>{int k=Mathf.Min(ids.Length-1,(int)(t*ids.Length));return StationPlan.Point(ids[k],47,0,1.4f);});
    }
    static void Shot(string name,int frames,Func<float,Vector3> position,Func<float,Vector3> look,bool ortho=false,float size=50)
    {
        string folder=Path.Combine(output,name);Directory.CreateDirectory(folder);cam.orthographic=ortho;cam.orthographicSize=size;
        for(int i=0;i<frames;i++)
        {
            float t=i/(float)frames;cam.transform.position=position(t);cam.transform.LookAt(look(t));cam.Render();
            var old=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,Width,Height),0,0);pixels.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Path.Combine(folder,i.ToString("D5")+".jpg"),pixels.EncodeToJPG(83));
        }
        Debug.Log("VIDEO_SHOT "+name+" "+frames+" frames @ "+Fps);
    }
}
