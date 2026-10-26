using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LastLight;

// Explicit batch entry without -quit: capture the advancing Play Mode camera.
// Camera.Render does not include the IMGUI HUD; label the evidence accordingly.
[InitializeOnLoad]
public static class StationCreamRuntimeCapture
{
    const string Key="LastLight.CreamRuntimeCapture";
    static double started;
    static StationCreamRuntimeCapture(){if(SessionState.GetBool(Key,false))EditorApplication.delayCall+=Resume;}
    public static void Begin()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/LastLightStation.unity");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;Resume();
    }
    static void Resume(){started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup-started>90){Finish(2);return;}
        if(!EditorApplication.isPlaying||Time.time<2)return;
        var d=UnityEngine.Object.FindObjectOfType<StationDirector>();if(!d)return;
        var camera=d.player.view;var old=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try
        {
            rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            string folder=Path.GetFullPath("../output/长夜泊站_素白版/运行时相机");Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,"GameplayCamera.png"),image.EncodeToPNG());
            File.WriteAllText(Path.Combine(folder,"说明.txt"),"Unity Play Mode camera render at Time.time="+Time.time+"; CharacterController and route guidance active; IMGUI HUD excluded; not a ScreenCapture of a visible player window.");
            Debug.Log("CREAM_RUNTIME_CAMERA_CAPTURED at "+Time.time);
        }
        finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
        Finish(0);
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;
        EditorApplication.delayCall+=()=>EditorApplication.Exit(code);
    }
}
