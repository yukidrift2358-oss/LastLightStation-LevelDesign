using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Build.Reporting;
using LastLight;

public static partial class StationWhiteboxBuilder
{
    const string ScenePath="Assets/Scenes/LastLightStation.unity";
    static Material floor,wall,trim,cyan,amber,red,dark,star,leaf,pulse,guide;
    static StationDirector director;
    static Transform root;
    static bool automated;
    

    [MenuItem("Last Light/Generate whitebox scene")]
    public static void Generate()
    {
        if(!automated&&!Application.isBatchMode&&!EditorUtility.DisplayDialog("Generate whitebox","This creates a new generated scene. Save your current scene before continuing.","Generate","Cancel"))return;
        Directory.CreateDirectory("Assets/Generated/Materials");Directory.CreateDirectory("Assets/Generated/Meshes");Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        floor=Mat("Deck",new Color(.29f,.36f,.41f));wall=Mat("Structure",new Color(.52f,.59f,.63f));trim=Mat("DarkTrim",new Color(.075f,.12f,.16f));
        cyan=Mat("CircuitBlue",new Color(.12f,.7f,.85f),true);amber=Mat("MemoryAmber",new Color(1,.6f,.19f),true);
        red=Mat("WarningRed",new Color(.85f,.18f,.09f),true);dark=Mat("DeepSpace",new Color(.035f,.065f,.1f));
        star=Mat("Starlight",new Color(.65f,.8f,.9f),true);leaf=Mat("DeadPlants",new Color(.25f,.3f,.18f));
        pulse=Transparent("Pulse",new Color(.15f,.8f,1,.12f));
        guide=Mat("RouteLight",Color.white);guide.shader=Shader.Find("LastLight/Guide");
        PrepareSciFiImports();
        director=new GameObject("StationFlow").AddComponent<StationDirector>();
        director.rooms=new StationRoom[StationPlan.Count];director.sideGates=new StationGate[StationPlan.Count];director.dockGates=new StationGate[StationPlan.Count];director.tutorialGates=new StationGate[4];director.tutorialObjectives=new StationInteractable[4];
        director.pulseMaterial=pulse;director.attackMaterial=Transparent("Strike",new Color(1,.65f,.2f,.25f));
        root=new GameObject("STATION_WHITEBOX").transform;
        BuildSpace();BuildPlayer();
        for(int i=0;i<StationPlan.Count;i++)BuildRoom(i);
        BuildBoundaries();BuildBridge();BuildTutorial();BuildBoss();BuildLifts();BuildConnections();DeviceSilhouettes();
        StationV6Audit.ValidateCounts();
        foreach(var lift in director.lifts)for(int y=1;y<24;y+=3){Shape("LiftDirectionBeacon",PrimitiveType.Sphere,lift.lower+new Vector3(3.3f,y,0),Vector3.one*.3f,cyan,root,false);}

        var guidance=director.gameObject.AddComponent<StationGuidance>();guidance.director=director;guidance.material=guide;guidance.arrowMesh=GuideArrow();
        var feedback=director.player.gameObject.AddComponent<StationPulseFeedback>();feedback.lineMaterial=guide;
        director.gameObject.AddComponent<StationHud>().director=director;
        director.gameObject.AddComponent<StationSmokeRunner>().director=director;
        RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.25f,.31f,.37f);RenderSettings.fog=false;
        var sun=new GameObject("DistantStarlight").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(.65f,.8f,1);sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(35,-40,0);sun.shadows=LightShadows.Soft;
        StationCreamPalette.ApplyToOpenScene();
        QualitySettings.shadowDistance=70;QualitySettings.pixelLightCount=4;
        PlayerSettings.companyName="Whitebox Lab";PlayerSettings.productName="Last Light Station";PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Validation");
        File.WriteAllText("Validation/layout-report.json",JsonUtility.ToJson(new LayoutReport{sectors=StationPlan.Count,route=StationPlan.Route,roomCount=director.rooms.Length,sideGateCount=director.sideGates.Length,dockGateCount=director.dockGates.Length,scene=ScenePath},true));
        Debug.Log("LAST_LIGHT_GENERATED: "+ScenePath);
    }
    [Serializable] class LayoutReport {public int sectors;public int[] route;public int roomCount,sideGateCount,dockGateCount;public string scene;}

    static Material Mat(string name,Color color,bool emissive=false)
    {
        string path="Assets/Generated/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Glossiness",.25f);m.SetFloat("_Metallic",.2f);
        if(emissive){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.8f);}
        EditorUtility.SetDirty(m);return m;
    }
    static Material Transparent(string name,Color color)
    {
        var m=Mat(name,color);m.SetFloat("_Mode",3);m.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite",0);m.DisableKeyword("_ALPHATEST_ON");m.EnableKeyword("_ALPHABLEND_ON");m.renderQueue=3000;return m;
    }
    static GameObject Shape(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material material,Transform parent=null,bool collider=true)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;var basis=parent??root;o.transform.SetParent(basis,false);
        // Decorations are authored in metres, even when their parent is a scaled primitive.
        var inverse=new Vector3(1/basis.lossyScale.x,1/basis.lossyScale.y,1/basis.lossyScale.z);
        o.transform.localPosition=Vector3.Scale(pos,inverse);o.transform.localScale=Vector3.Scale(scale,inverse);o.GetComponent<Renderer>().sharedMaterial=material;
        if(!collider)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        else if(type==PrimitiveType.Cylinder)
        {
            // Unity's default cylinder collider is a capsule; a wide, thin deck needs its actual mesh.
            UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
            o.AddComponent<MeshCollider>().sharedMesh=o.GetComponent<MeshFilter>().sharedMesh;
        }
        if(collider&&(name.EndsWith("Deck")||name.EndsWith("Ramp")||name=="BossApproach"))o.AddComponent<StationWalkSurface>();
        return o;
    }
    static GameObject Box(string name,Vector3 pos,Vector3 scale,Material material,Transform parent=null,bool collider=true)
    {return Shape(name,PrimitiveType.Cube,pos,scale,material,parent,collider);}
    static void Label(string text,Vector3 pos,float yaw,float size,Transform parent=null)
    {
        var o=new GameObject("Sign_"+text);var basis=parent??root;
        var inverse=new Vector3(1/basis.lossyScale.x,1/basis.lossyScale.y,1/basis.lossyScale.z);
        o.transform.SetParent(root,false);o.transform.position=basis.TransformPoint(Vector3.Scale(pos,inverse));o.transform.rotation=basis.rotation*Quaternion.Euler(0,yaw+180,0);
        o.transform.SetParent(basis,true);
        var t=o.AddComponent<TextMesh>();t.text=text;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.6f,.85f,.9f);
        t.font.RequestCharactersInTexture(text,64);
        var material=new Material(Shader.Find("LastLight/WorldText"));material.mainTexture=t.font.material.mainTexture;
        o.GetComponent<MeshRenderer>().sharedMaterial=material;
        var label=o.AddComponent<StationWorldLabel>();label.textShader=Shader.Find("LastLight/WorldText");
    }
    static GameObject ArcFloor(int sector,float inner,float outer,float height,string name,Material material,bool ceiling=false)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();int slices=12;
        for(int k=0;k<=slices;k++)
        {
            float a=(StationPlan.Angle(sector)-StationPlan.Width(sector)/2+StationPlan.Width(sector)*k/slices)*Mathf.Deg2Rad;
            vertices.Add(new Vector3(Mathf.Sin(a)*inner,StationPlan.Height(sector)+height,Mathf.Cos(a)*inner));vertices.Add(new Vector3(Mathf.Sin(a)*outer,StationPlan.Height(sector)+height,Mathf.Cos(a)*outer));
        }
        for(int k=0;k<slices;k++)
        {
            int i=k*2;
            if(!ceiling)triangles.AddRange(new[]{i,i+1,i+3,i,i+3,i+2});
            else triangles.AddRange(new[]{i,i+3,i+1,i,i+2,i+3});
        }
        CutShafts(vertices,triangles);
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path="Assets/Generated/Meshes/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing!=null){EditorUtility.CopySerialized(mesh,existing);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
        var go=new GameObject(name);go.transform.parent=root;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;go.AddComponent<MeshCollider>().sharedMesh=mesh;if(!ceiling)go.AddComponent<StationWalkSurface>();return go;
    }
    static void BuildSpace()
    {
        var space=new GameObject("SPACE_SILHOUETTES").transform;space.parent=root;
        Shape("DistantHome",PrimitiveType.Sphere,new Vector3(-380,-100,-650),Vector3.one*390,Mat("Homeworld",new Color(.15f,.27f,.39f)),space,false);
        var random=new System.Random(32);
        for(int i=0;i<180;i++)
        {
            Vector3 d=new Vector3((float)random.NextDouble()*2-1,(float)random.NextDouble()*1.5f-.4f,(float)random.NextDouble()*2-1).normalized;
            Shape("Star",PrimitiveType.Sphere,d*650,Vector3.one*(1+(float)random.NextDouble()*2),star,space,false);
        }
        for(int i=0;i<8;i++)
        {
            var wing=Box("BrokenSolarWing",new Vector3(-95+i*28,-10-i%2*10,-115),new Vector3(16,.4f,9),dark,space);
            wing.transform.rotation=Quaternion.Euler(i*9,20+i*23,12);
            for(int j=-3;j<=3;j++)Box("Cell",new Vector3(j*2, .3f,0),new Vector3(.08f,.05f,8),cyan,wing.transform,false);
        }
        Shape("CentralServiceCore",PrimitiveType.Cylinder,new Vector3(0,-15,0),new Vector3(13,10,13),trim,space);
        for(int i=0;i<12;i++)
        {
            var rib=Box("OuterHullRib",StationPlan.Point(i,59,0,-6),new Vector3(6,9,4),trim,space,false);rib.transform.rotation=Quaternion.Euler(0,StationPlan.Angle(i),0);
        }
    }
    static void BuildPlayer()
    {
        var p=new GameObject("Player");p.layer=2;p.transform.position=new Vector3(0,.15f,126);
        var motor=p.AddComponent<CharacterController>();motor.height=1.8f;motor.radius=.35f;motor.center=Vector3.up*.9f;motor.stepOffset=.35f;motor.slopeLimit=50;
        director.player=p.AddComponent<StationPlayer>();director.player.director=director;
        var model=new GameObject("WhiteboxSwordsman").transform;model.SetParent(p.transform,false);model.localRotation=Quaternion.Euler(0,180,0);director.player.visual=model;
        BuildHumanoid(model,director.player);
        var c=new GameObject("ThirdPersonCamera");c.tag="MainCamera";var cam=c.AddComponent<Camera>();cam.fieldOfView=65;cam.farClipPlane=1400;cam.nearClipPlane=.05f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.008f,.014f,.025f);c.AddComponent<AudioListener>();director.player.view=cam;
        c.transform.SetPositionAndRotation(new Vector3(0,4,132),Quaternion.Euler(20,180,0));
    }
    static Transform Joint(string name,Transform parent,Vector3 position)
    {var bone=new GameObject(name).transform;bone.SetParent(parent,false);bone.localPosition=position;return bone;}
    static void BuildHumanoid(Transform rootModel,StationPlayer player)
    {
        var white=Mat("WhiteFigure",new Color(.92f,.94f,.95f));white.SetFloat("_Glossiness",.4f);
        var joints=Mat("FigureJoints",new Color(.065f,.085f,.11f));
        StationHeroAssets.Build(rootModel,player,white,joints,guide,SwordMesh());
    }
    static Mesh SwordMesh()
    {
        string path="Assets/Generated/Meshes/WhiteSword.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.vertices=new[]{new Vector3(-.055f,-.022f,.2f),new Vector3(.055f,-.022f,.2f),new Vector3(-.055f,.022f,.2f),new Vector3(.055f,.022f,.2f),new Vector3(-.055f,-.022f,1),new Vector3(.055f,-.022f,1),new Vector3(-.055f,.022f,1),new Vector3(.055f,.022f,1),new Vector3(0,0,1.2f)};
        mesh.triangles=new[]{0,2,1,1,2,3,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5,4,5,8,6,8,7,4,8,6,5,7,8};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static void BuildRoom(int id)
    {
        ArcFloor(id,28,56,0,"Sector_"+(id+1).ToString("00")+"_Deck",floor);
        // Local roof strips frame the station while preserving an inspectable blockout from above.
        ArcFloor(id,47,56,7.5f,"Sector_"+(id+1).ToString("00")+"_Canopy",trim,true);
        var o=new GameObject("SECTOR_"+(id+1).ToString("00")+"_"+StationPlan.Names[id]);o.transform.parent=root;o.transform.position=StationPlan.Point(id,42);o.transform.rotation=Quaternion.Euler(0,StationPlan.Angle(id),0);
        var room=o.AddComponent<StationRoom>();room.sector=id;room.director=director;room.entryPoint=StationPlan.Point(id,41,0,.15f);director.rooms[id]=room;
        var trigger=o.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up*2;trigger.size=new Vector3(20,5,25);
        var indicator=Box("CircuitState_A",new Vector3(-7,1.5f,1),new Vector3(.25f,2.5f,.25f),amber,o.transform,false);room.powerIndicator=indicator.GetComponent<Renderer>();
        if((room.Need&RoomNeed.Power)!=0)Interact(room,InteractionKind.Power,"切换电力支路 A / B",new Vector3(-6,1.1f,1),cyan);
        if((room.Need&RoomNeed.Seal)!=0)
        {
            Interact(room,InteractionKind.Seal,"关闭泄漏侧隔离阀",new Vector3(6,1.1f,2),amber);
            room.pressure=Hazard(room,new Vector3(2,.8f,6),false);
        }
        if((room.Need&RoomNeed.Pulse)!=0)room.scanner=Hazard(room,new Vector3(0,1.3f,-5),true);
        if((room.Need&RoomNeed.Record)!=0)
            Interact(room,InteractionKind.Record,"读取："+StationPlan.Records[id],id==4?new Vector3(6,1.1f,-4):new Vector3(3,1.1f,8),amber);
        Interact(room,InteractionKind.Exit,id==20?"登记最后交接 / 开启 Boss 舱":"完成本区 / 开启下一通路",new Vector3(-3,1.1f,-7),cyan);
        Label("L"+(StationPlan.Floor(id)+1)+" / "+(StationPlan.Local(id)+1).ToString("00"),new Vector3(0,4.6f,10),180,.09f,o.transform);
        Label("A : ARCHIVE   /   B : TRANSIT",new Vector3(-5,2.6f,2),180,.035f,o.transform);
        for(int i=0;i<12;i++)
        {
            float a=StationPlan.Angle(id)-StationPlan.Width(id)/2+(i+.5f)*StationPlan.Width(id)/12;float h=StationPlan.Height(id);float segment=56*StationPlan.Width(id)*Mathf.Deg2Rad/12;
            // The open strip at the outside of 01 and 10 is used by tutorial and boss approaches.
            bool opening=(id==0||id==3||id==6||id==15||id==18||id==20)&&i>=4&&i<=7;
            if(!opening)
            {
                var w=Box("WindowSill",Polar(a,56,h+1.4f),new Vector3(segment+.15f,2.8f,.55f),wall);w.transform.rotation=Quaternion.Euler(0,a,0);
                var beam=Box("WindowFrame",Polar(a,56,h+5.4f),new Vector3(.14f,5,.7f),trim);beam.transform.rotation=w.transform.rotation;
                var top=Box("WindowLintel",Polar(a,56,h+7),new Vector3(segment+.15f,.8f,.6f),wall);top.transform.rotation=w.transform.rotation;
            }

        }
        for(int k=0;k<36;k++)
        {
            float relative=-StationPlan.Width(id)/2+(k+.5f)*StationPlan.Width(id)/36;
            if(Mathf.Abs(relative)<5.5f)continue;
            float a=StationPlan.Angle(id)+relative;
            var w=Box("InnerBulkhead",Polar(a,28,StationPlan.Height(id)+3),new Vector3(28*StationPlan.Width(id)*Mathf.Deg2Rad/36+.05f,6,.6f),wall);w.transform.rotation=Quaternion.Euler(0,a,0);
        }
        // Active directional lighting is supplied by StationGuidance.
        BuildFurniture(room,o.transform);
        if(id==23||id==25){foreach(var item in room.GetComponentsInChildren<StationInteractable>()){if(id==23&&item.kind==InteractionKind.Power)item.transform.localPosition=new Vector3(8,3.9f,11);if(id==25&&item.kind==InteractionKind.Record)item.transform.localPosition=new Vector3(-7,4.1f,11);}}
        var light=new GameObject("EmergencyLight").AddComponent<Light>();light.transform.parent=o.transform;light.transform.localPosition=new Vector3(0,5,0);light.type=LightType.Point;light.range=24;light.intensity=1.1f;light.color=StationPlan.Rest(id)?new Color(1,.67f,.35f):new Color(.3f,.7f,1);light.shadows=LightShadows.None;
        if((room.Need&RoomNeed.Relay)!=0)
        {
            for(int j=1;j<=3;j++){var item=Interact(room,InteractionKind.Relay,"接通继电器 "+j+"（顺序 "+room.RelayHint+"）",new Vector3((j-2)*5,1.1f,3),cyan);item.relayIndex=j;if(id==16&&j==3)item.transform.localPosition=new Vector3(4,1.1f,-4);if(id==9&&j==1)item.transform.localPosition=new Vector3(-6,1.1f,-5);if(id==9&&j==3)item.transform.localPosition=new Vector3(8,3.9f,11);}
            Label("RELAY : "+room.RelayHint.Replace("→",">"),new Vector3(0,3.8f,5),180,.06f,o.transform);
        }
        var threats=new List<StationEnemy>();
        if((room.Need&RoomNeed.Combat)!=0)
        {
            threats.Add(Enemy(room,new Vector3(id==8?-4:-3,.1f,id==8?-1:5),false));
            if(id!=2){var other=Enemy(room,new Vector3(id==8||id==11||id==17?8:5,id==8||id==11||id==17?3.1f:.1f,id==8?8.7f:id==11||id==17?12:5),true);other.kind=id==11||id==17?EnemyKind.Repair:id==19?EnemyKind.Tractor:EnemyKind.Sentry;threats.Add(other);}
            if(id==24){var repair=Enemy(room,new Vector3(8,3.1f,11),false);repair.kind=EnemyKind.Repair;threats.Add(repair);}
        }
        room.enemies=threats.ToArray();foreach(var enemy in threats)DecorateEnemy(enemy);
    }
    static Vector3 Polar(float degrees,float radius,float y) {float a=degrees*Mathf.Deg2Rad;return new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius);}
    static StationInteractable Interact(StationRoom room,InteractionKind kind,string title,Vector3 position,Material material,Transform parent=null)
    {
        var o=Box(kind.ToString(),position,new Vector3(1.15f,1.7f,.8f),material,parent??room.transform);
        var item=o.AddComponent<StationInteractable>();item.director=director;item.room=room;item.kind=kind;item.title=title;
        Box("Screen",new Vector3(0,.3f,.43f),new Vector3(.75f,.45f,.05f),dark,o.transform,false);
        Label(kind==InteractionKind.Power||kind==InteractionKind.TutorialPower||kind==InteractionKind.BossPower?"POWER  A / B":kind==InteractionKind.Seal||kind==InteractionKind.TutorialSeal||kind==InteractionKind.BossSeal?"SEAL":kind==InteractionKind.Record?"ARCHIVE":kind==InteractionKind.Relay?"RELAY":"TRANSIT",new Vector3(0,1.6f,0),180,.035f,o.transform);
        return item;
    }
    static StationHazard Hazard(StationRoom room,Vector3 pos,bool scanner)
    {
        var o=new GameObject(scanner?"InfectedScanner":"PressureLeak");o.transform.SetParent(room.transform,false);o.transform.localPosition=pos;o.layer=2;
        var c=o.AddComponent<SphereCollider>();c.isTrigger=true;c.radius=scanner?1:3.4f;
        var h=o.AddComponent<StationHazard>();h.director=director;h.room=room;h.suppressible=scanner;
        var v=Shape("WarningVolume",PrimitiveType.Cylinder,new Vector3(0,-pos.y+.06f,0),new Vector3(6.8f,.04f,6.8f),Transparent(scanner?"ScanField":"LowPressure",scanner?new Color(1,.2f,.07f,.3f):new Color(.2f,.65f,1,.23f)),o.transform,false);
        h.visual=v.GetComponent<Renderer>();
        Box("DamagedUnit",Vector3.zero,new Vector3(.6f,1.2f,.6f),red,o.transform,false);
        return h;
    }
    static StationEnemy Enemy(StationRoom room,Vector3 pos,bool turret,Transform parent=null)
    {
        var o=new GameObject(turret?"DefenceTurret":"InfectedMaintenanceUnit");o.transform.SetParent(parent??room.transform,false);o.transform.localPosition=pos;o.layer=2;
        var c=o.AddComponent<CapsuleCollider>();c.center=Vector3.up*.8f;c.radius=.6f;c.height=1.6f;c.isTrigger=true;
        var e=o.AddComponent<StationEnemy>();e.room=room;e.director=director;e.turret=turret;e.kind=turret?EnemyKind.Sentry:EnemyKind.Charger;
        var body=Box("MachineBody",new Vector3(0,.8f,0),new Vector3(1,1.3f,1),trim,o.transform,false);body.layer=2;e.body=body.GetComponent<Renderer>();
        Box("Infection",new Vector3(0,1,.55f),new Vector3(.65f,.2f,.1f),red,o.transform,false).layer=2;
        e.warning=Box("EnemyTelegraph",Vector3.zero,Vector3.one,Transparent("EnemyWarning",new Color(1,.48f,.1f,.35f)),null,false).transform;e.warning.gameObject.SetActive(false);
        return e;
    }
    static void DecorateEnemy(StationEnemy e)
    {
        foreach(string n in new[]{"MachineBody","Infection"}){var old=e.transform.Find(n);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
        string model=e.kind==EnemyKind.Charger?"Enemy_Trilobite":e.kind==EnemyKind.Tractor?"Enemy_QuadShell":"Enemy_EyeDrone";
        var go=SciFiModel(model,new Vector3(0,e.kind==EnemyKind.Repair||e.kind==EnemyKind.Sentry?.7f:0,0),e.kind==EnemyKind.Charger?2.3f:e.kind==EnemyKind.Tractor?2.1f:1.5f,e.transform);
        e.body=go.GetComponentInChildren<Renderer>();
        foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=2;
        var animation=go.AddComponent<StationMachineVisual>();animation.enemy=e;
        var beamObject=new GameObject("EyeDroneBeam");beamObject.transform.SetParent(e.transform,false);beamObject.layer=2;
        e.beam=beamObject.AddComponent<LineRenderer>();e.beam.sharedMaterial=guide;e.beam.positionCount=2;e.beam.startWidth=e.beam.endWidth=.09f;e.beam.enabled=false;
        if(e.kind==EnemyKind.Charger)Shape("InfectedOptic",PrimitiveType.Sphere,new Vector3(0,1.3f,.6f),Vector3.one*.24f,red,e.transform,false);
        if(e.kind==EnemyKind.Repair){Shape("RepairHalo",PrimitiveType.Cylinder,new Vector3(0,2,0),new Vector3(1.5f,.04f,1.5f),cyan,e.transform,false);}
        if(e.kind==EnemyKind.Tractor)for(int k=-1;k<=1;k+=2)Box("MagnetProng",new Vector3(k*.65f,1,.65f),new Vector3(.15f,.75f,.6f),amber,e.transform,false);
        if(e.kind==EnemyKind.Sentry)Box("ScannerLens",new Vector3(0,1,.8f),new Vector3(.15f,.15f,.15f),red,e.transform,false);
        Label(e.kind.ToString().ToUpper(),new Vector3(0,2.5f,0),180,.035f,e.transform);
        animation.statusLabel=e.GetComponentInChildren<TextMesh>();
    }
    static void PrepareSciFiImports()
    {
        const string folder="Assets/ThirdParty/QuaterniusSciFi/Textures";
        foreach(var path in Directory.GetFiles(folder,"*_Normal.png"))
        {
            var importer=AssetImporter.GetAtPath(path.Replace('\\','/')) as TextureImporter;
            if(importer!=null&&importer.textureType!=TextureImporterType.NormalMap){importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}
        }
    }
    static Material SciFiMaterial(string texture,string sourceName)
    {
        string key="SciFi_"+texture;
        var mat=Mat(key,new Color(.7f,.77f,.8f));mat.SetFloat("_Metallic",.5f);mat.SetFloat("_Glossiness",.38f);
        const string folder="Assets/ThirdParty/QuaterniusSciFi/Textures/";
        mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+texture+"_BaseColor.png");
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+texture+"_Normal.png");
        if(normal!=null){mat.SetTexture("_BumpMap",normal);mat.EnableKeyword("_NORMALMAP");}
        var emission=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+texture+"_Emissive.png");
        if(emission!=null){mat.SetTexture("_EmissionMap",emission);mat.SetColor("_EmissionColor",new Color(.7f,.3f,.3f));mat.EnableKeyword("_EMISSION");}
        return mat;
    }
    static GameObject SciFiModel(string name,Vector3 pos,float size,Transform parent)
    {
        string path="Assets/ThirdParty/QuaterniusSciFi/Models/"+name+".fbx";
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(asset==null)throw new Exception("Missing downloaded model "+path);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.name="SciFi_"+name;go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.zero;
        Bounds bounds=new Bounds(go.transform.position,Vector3.zero);bool first=true;
        foreach(var r in go.GetComponentsInChildren<Renderer>())
        {
            if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);
            var mats=r.sharedMaterials;
            for(int i=0;i<mats.Length;i++)
            {
                string original=mats[i]!=null?mats[i].name:"";
                string texture=name=="Enemy_Trilobite"?"T_Enemies_Large":name.StartsWith("Enemy_")?"T_Enemies":name.Contains("Crate")||name.Contains("Chest")?"T_Props_Crates":name.Contains("Barrel")||name.Contains("Health")?"T_Props_Batch1":name.Contains("Locker")?"T_Trim_02":"T_Trim_03";
                if(original.Contains("Trim_01"))texture="T_Trim_01";if(original.Contains("Trim_02"))texture="T_Trim_02";if(original.Contains("Trim_03"))texture="T_Trim_03";
                mats[i]=SciFiMaterial(texture,original);
            }
            r.sharedMaterials=mats;
        }
        float scale=size/Mathf.Max(.01f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
        Vector3 center=parent.InverseTransformPoint(bounds.center),bottom=parent.InverseTransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
        go.transform.localScale=Vector3.one*scale;go.transform.localPosition=pos-new Vector3(center.x,bottom.y,center.z)*scale;
        foreach(var c in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
        var animator=go.GetComponent<Animator>();if(animator!=null){animator.applyRootMotion=false;UnityEngine.Object.DestroyImmediate(animator);}
        // Use the authored looping idle without root motion; behavior remains in StationEnemy.
        var clips=AssetDatabase.LoadAllAssetsAtPath(path);AnimationClip idle=null;
        foreach(var a in clips)if(a is AnimationClip clip&&!clip.name.StartsWith("__")&&(idle==null||clip.name.ToLowerInvariant().Contains("idle")))idle=clip;
        if(idle!=null)
        {
            string controllerPath="Assets/Generated/"+name+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
            if(controller==null){controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddMotion(idle);}
            var anim=go.AddComponent<Animator>();anim.runtimeAnimatorController=controller;anim.applyRootMotion=false;
        }
        return go;
    }
    static Mesh GuideArrow()
    {
        string path="Assets/Generated/Meshes/GuideArrow.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.vertices=new[]{new Vector3(-.34f,0,-.3f),new Vector3(0,0,.4f),new Vector3(.34f,0,-.3f),new Vector3(.15f,0,-.3f),new Vector3(0,0,0),new Vector3(-.15f,0,-.3f)};
        mesh.triangles=new[]{0,1,5,5,1,4,4,1,3,3,1,2};mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white,Color.white,Color.white};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static void Model(string name,Vector3 pos,float size,Transform parent)
    {
        string replacement=name=="desk_chair"?"Prop_Chair":name=="desk_computer"?"Prop_Desk_Medium":name=="barrels"?"Prop_Barrel2_Open":name=="machine_wireless"?"Prop_Locker":name=="satelliteDish"?"Prop_SatelliteDish":null;
        if(replacement!=null){SciFiModel(replacement,pos,size,parent);return;}
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/KenneySpace/"+name+".fbx");
        if(asset==null)return;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.name="Kenney_"+name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;
        var renderers=go.GetComponentsInChildren<Renderer>();Bounds bounds=new Bounds(go.transform.position,Vector3.zero);foreach(var r in renderers){bounds.Encapsulate(r.bounds);r.sharedMaterial=wall;}
        float scale=size/Mathf.Max(.01f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));go.transform.localScale=Vector3.one*scale;
        foreach(var c in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
    }
    static void BuildFurniture(StationRoom room,Transform parent)
    {
        int id=room.sector;
        if(StationPlan.Rest(id))
        {
            if(id==23||id==25)
            {
                float side=id==25?-7:8;
                Box("QuietObservationDeck",new Vector3(side,2.8f,11),new Vector3(3,.4f,6),trim,parent);
                Ramp("QuietAccessRamp",new Vector3(side,1.35f,4),new Vector3(3,.3f,9),-19,trim,parent);
                Box("QuietDeckGuard",new Vector3(side+(side>0?1.5f:-1.5f),3.5f,11),new Vector3(.2f,1.2f,6),wall,parent);
            }
            for(int k=-1;k<=1;k++){Box("EmptySeat",new Vector3(k*3,.5f,8),new Vector3(1.2f,1,1.2f),trim,parent);Model("desk_chair",new Vector3(k*3,1,8),1.4f,parent);}
            Label("SAFE / REMEMBER THEM",new Vector3(0,3.7f,9),180,.065f,parent);
            if(id==12){Box("EmptyCinemaScreen",new Vector3(0,3,11),new Vector3(12,4,.3f),dark,parent);Box("FrozenOcean",new Vector3(0,3,10.8f),new Vector3(10,.15f,.1f),cyan,parent,false);}
            if(id==21)for(int n=0;n<12;n++)Box("NamePlaque",new Vector3(-8+n*1.4f,2.4f,11),new Vector3(.7f,.9f,.15f),amber,parent,false);
            return;
        }
        if(id==8||id==9||id==11||id==17||id==23||id==24||id==25)
        {
            float side=id==25?-7:8;
            Box("UpperFlankDeck",new Vector3(side,2.8f,11),new Vector3(3,.4f,6),trim,parent);
            Ramp("FlankRamp",new Vector3(side,1.35f,4),new Vector3(3,.3f,9),-19,trim,parent);
            Box("UpperGuard",new Vector3(side+(side>0?1.5f:-1.5f),3.5f,11),new Vector3(.2f,1.2f,6),wall,parent);
            Box("SightlineCover",new Vector3(id==8?-3:0,1.2f,1),new Vector3(2,2.4f,1),wall,parent);
            Model("machine_generator",new Vector3(-side,.1f,7),3,parent);
            Label("UPPER SERVICE WALK / +3m",new Vector3(side,4.8f,13),180,.04f,parent);
            if(id==25){var arm=new GameObject("InfectedArmPivot").transform;arm.SetParent(parent,false);arm.localPosition=new Vector3(0,2.5f,-5);room.mechanicalArm=arm;Box("MechanicalArm",new Vector3(2,0,0),new Vector3(4,.4f,.4f),red,arm,false);}
            return;
        }
        if(id==11||id==17)SciFiModel("Prop_Crate_Tarp_Large",new Vector3(-7,0,9),2.4f,parent);
        if(id==1){SciFiModel("Prop_Chest",new Vector3(-7,0,9),1.6f,parent);SciFiModel("Prop_Mug",new Vector3(7,1.23f,0),.22f,parent);}
        if(id==14)SciFiModel("Prop_HealthPack_Tube",new Vector3(-7,0,8),1,parent);
        int motif=id%5;
        for(int k=-1;k<=1;k++)
        {
            if(motif==0){Box("UprightGenerator",new Vector3(8,1.5f,k*5),new Vector3(2.4f,3,2.4f),trim,parent);Model("machine_generator",new Vector3(-8,.2f,k*5),2.6f,parent);}
            else if(motif==1){Box("RationTable",new Vector3(id==16?8:7,.6f,k*5),new Vector3(2.6f,1.2f,2),wall,parent);Model("desk_computer",new Vector3(id==16?-3:-8,.1f,k*5),2.7f,parent);}
            else if(motif==2){Box("EmptyFreightRack",new Vector3(-8,1.3f,k*5),new Vector3(2.2f,2.6f,2),wall,parent);Model("barrels",new Vector3(8,.1f,k*5),2.5f,parent);}
            else if(motif==3){Box("DeadGrowBed",new Vector3(-8,.5f,k*5),new Vector3(2.4f,1,3),wall,parent);for(int n=0;n<3;n++)Shape("DeadStem",PrimitiveType.Cylinder,new Vector3(-8+n*.6f,1.5f,k*5),new Vector3(.1f,.8f,.1f),leaf,parent,false);}
            else {Box("TreatmentPod",new Vector3(8,.6f,k*5),new Vector3(2.4f,1.2f,3),wall,parent);Model("machine_wireless",new Vector3(id==19?-14:-8,.1f,k*5),2.3f,parent);}
        }
        if(id==19){for(int k=-1;k<=1;k+=2)Box("MagneticBaffle",new Vector3(k*3,1.8f,1),new Vector3(1,3.6f,1),trim,parent);}
        if(id==13||id==25){var arm=new GameObject("InfectedArmPivot").transform;arm.SetParent(parent,false);arm.localPosition=new Vector3(0,2.5f,-5);room.mechanicalArm=arm;Box("MechanicalArm",new Vector3(2,0,0),new Vector3(4,.4f,.4f),red,arm,false);}
        if(id==16)Model("satelliteDish",new Vector3(-5,.1f,10),4,parent);
    }
    static void Ramp(string name,Vector3 pos,Vector3 scale,float angle,Material material,Transform parent)
    {var r=Box(name,pos,scale,material,parent);r.transform.localRotation=Quaternion.Euler(angle,0,0);}
    static StationGate Gate(string name,Vector3 pos,float yaw,float width,Transform parent=null)
    {
        var o=new GameObject(name);o.transform.SetParent(parent??root,false);o.transform.localPosition=pos;o.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var g=o.AddComponent<StationGate>();g.leaf=Box("RetractingLeaf",new Vector3(0,2.4f,0),new Vector3(width,4.8f,.6f),trim,o.transform).transform;
        Box("LockStrip",new Vector3(0,0,-.34f),new Vector3(width*.8f,.15f,.04f),amber,g.leaf,false);
        for(int side=-1;side<=1;side+=2)Box("Frame",new Vector3(side*(width/2+.2f),2.7f,0),new Vector3(.4f,5.4f,.9f),wall,o.transform);
        Box("Lintel",new Vector3(0,5.2f,0),new Vector3(width+.8f,.4f,.9f),wall,o.transform);
        return g;
    }
    static void BuildBoundaries()
    {
        for(int i=0;i<StationPlan.Count;i++)
        {
            float angle=StationPlan.Angle(i)+StationPlan.Width(i)/2,h=StationPlan.Height(i);
            for(int side=-1;side<=1;side+=2){var w=Box("RadialBulkhead",Polar(angle,42+side*9,h+3),new Vector3(.65f,6,10.5f),wall);w.transform.rotation=Quaternion.Euler(0,angle,0);}
            director.sideGates[i]=Gate("SideGate_"+i,Polar(angle,42,h),angle+90,7);
            director.dockGates[i]=Gate("DockGate_"+i,StationPlan.Point(i,28),StationPlan.Angle(i),5);
            Label("L"+(StationPlan.Floor(i)+1)+" / "+(StationPlan.Local(i)+1).ToString("00"),StationPlan.Point(i,29,0,4.2f),StationPlan.Angle(i),.045f);
        }
    }
    static void BuildBridge()
    {
        director.bridges=new Transform[3];
        for(int f=0;f<3;f++)
        {
            var bridge=new GameObject("ROTATING_BRIDGE_L"+(f+1)).transform;bridge.parent=root;bridge.position=Vector3.up*(f*24);director.bridges[f]=bridge;bridge.rotation=Quaternion.Euler(0,StationPlan.Rotations[f],0);
            Box("ContinuousDeck",new Vector3(0,-.25f,0),new Vector3(4.4f,.5f,60),floor,bridge);
            for(int side=-1;side<=1;side+=2){Box("SafetyRail",new Vector3(side*2.2f,1.2f,0),new Vector3(.18f,2.4f,60),wall,bridge);Box("Guidance",new Vector3(side*1.75f,.03f,0),new Vector3(.08f,.03f,59),cyan,bridge,false);}
            for(int z=-25;z<=25;z+=10){for(int side=-1;side<=1;side+=2)Box("BridgeRib",new Vector3(side*2.3f,2.5f,z),new Vector3(.25f,5,.3f),trim,bridge);}
            if(f==1){Model("satelliteDish",new Vector3(-10,-10,0),8,bridge);Label("WATER / EMERGENCY NETWORK",new Vector3(0,4,0),180,.07f,bridge);}
            if(f==2){for(int j=-1;j<=1;j++)Box("MemoryBeacon",new Vector3(8+j*4,-5,0),new Vector3(.4f,7,.4f),amber,bridge,false);}
        }
        director.bridge=director.bridges[0];
    }
    static void BuildLifts()
    {
        director.lifts=new StationLift[2];int[] from={3,15},to={6,18};
        for(int f=0;f<2;f++)
        {
            var lift=new GameObject("OBSERVATION_LIFT_"+f).AddComponent<StationLift>();lift.director=director;lift.destinationRoom=to[f];lift.lower=StationPlan.Point(from[f],66);lift.upper=StationPlan.Point(to[f],66);director.lifts[f]=lift;
            var deck=new GameObject("MovingObservationDeck").transform;deck.parent=root;deck.position=lift.lower;deck.rotation=Quaternion.Euler(0,StationPlan.Angle(from[f]),0);lift.platform=deck;
            Box("LiftDeck",new Vector3(0,-.2f,0),new Vector3(5,.4f,5),cyan,deck);
            for(int side=-1;side<=1;side+=2)Box("LiftGuard",new Vector3(side*2.5f,1.1f,0),new Vector3(.15f,2.2f,5),wall,deck);
            Box("OuterGuard",new Vector3(0,1.1f,2.5f),new Vector3(5,2.2f,.15f),wall,deck);
            lift.gates=new StationGate[2];
            int[] ends={from[f],to[f]};for(int j=0;j<2;j++)
            {
                int id=ends[j];var landing=new GameObject("ObservationLanding_"+id).transform;landing.parent=root;landing.position=StationPlan.Point(id,60);landing.rotation=Quaternion.Euler(0,StationPlan.Angle(id),0);
                Box("LandingDeck",new Vector3(0,-.25f,0),new Vector3(7,.5f,9.5f),floor,landing);
                for(int side=-1;side<=1;side+=2)Box("LandingGuard",new Vector3(side*3.4f,1.1f,0),new Vector3(.15f,2.2f,9.5f),wall,landing);
                lift.gates[j]=Gate("LiftAccess_"+id,StationPlan.Point(id,59),StationPlan.Angle(id),6.6f);
                Label("OBSERVATION LIFT / SAFE",StationPlan.Point(id,61,0,4),StationPlan.Angle(id),.06f);
            }
            for(int side=-1;side<=1;side+=2){var rail=Box("LiftGuideRail",lift.lower+Vector3.up*12+Quaternion.Euler(0,StationPlan.Angle(from[f]),0)*new Vector3(side*3,0,2),new Vector3(.3f,28,.3f),trim);}
        }
    }
    static void BuildTutorial()
    {
        var t=new GameObject("LINEAR_APPROACH").transform;t.parent=root;
        Box("ApproachDeck",new Vector3(0,-.25f,94),new Vector3(8,.5f,76),floor,t);
        for(int side=-1;side<=1;side+=2)
        {
            Box("ApproachWall",new Vector3(side*4,3,94),new Vector3(.5f,6,76),wall,t);
            Box("ApproachGuide",new Vector3(side*3.5f,.04f,94),new Vector3(.08f,.04f,74),cyan,t,false);
        }
        Box("DockEnd",new Vector3(0,3,132),new Vector3(8,6,.5f),trim,t);
        for(int i=0;i<4;i++)director.tutorialGates[i]=Gate("TeachingGate_"+i,new Vector3(0,0,112-i*14),0,7.5f,t);
        director.tutorialObjectives[0]=Interact(null,InteractionKind.TutorialPower,"教学：把应急电力切换给通路",new Vector3(-2.4f,1.1f,119),cyan,t);
        director.tutorialObjectives[1]=Interact(null,InteractionKind.TutorialSeal,"教学：关闭泄漏侧隔离阀",new Vector3(2.4f,1.1f,105),amber,t);
        var leak=new GameObject("TeachingPressureLeak");leak.transform.parent=t;leak.transform.position=new Vector3(0,.8f,103);leak.layer=2;
        var leakCollider=leak.AddComponent<SphereCollider>();leakCollider.isTrigger=true;leakCollider.radius=3.4f;
        var leakHazard=leak.AddComponent<StationHazard>();leakHazard.director=director;leakHazard.suppressible=false;director.teachingLeak=leakHazard;
        leakHazard.visual=Shape("LowPressureZone",PrimitiveType.Cylinder,new Vector3(0,-.74f,0),new Vector3(6.8f,.04f,6.8f),Transparent("TeachingLowPressure",new Color(.2f,.65f,1,.23f)),leak.transform,false).GetComponent<Renderer>();
        var h=new GameObject("TeachingScanner");h.transform.parent=t;h.transform.position=new Vector3(0,1.3f,91);h.layer=2;
        var collider=h.AddComponent<SphereCollider>();collider.isTrigger=true;collider.radius=1;
        var hazard=h.AddComponent<StationHazard>();hazard.director=director;hazard.tutorial=true;
        var hv=Box("ScannerBody",Vector3.zero,new Vector3(1,2,1),red,h.transform,false);hazard.visual=hv.GetComponent<Renderer>();
        var placeholder=h.AddComponent<StationInteractable>();placeholder.director=director;placeholder.title="靠近装置按 Q 抑制（4 秒）";director.tutorialObjectives[2]=placeholder;
        director.tutorialEnemy=Enemy(null,new Vector3(0,.1f,77),false,t);director.tutorialEnemy.tutorial=true;DecorateEnemy(director.tutorialEnemy);
        var enemyTarget=director.tutorialEnemy.gameObject.AddComponent<StationInteractable>();enemyTarget.director=director;enemyTarget.title="左键攻击 / Shift 闪避";director.tutorialObjectives[3]=enemyTarget;
        Label("01  /  POWER TRANSFER",new Vector3(0,4,120),0,.065f,t);
        Label("02  /  PRESSURE ISOLATION",new Vector3(0,4,106),0,.065f,t);
        Label("03  /  SUPPRESSION PULSE",new Vector3(0,4,92),0,.065f,t);
        Label("04  /  DEFENCE RESPONSE",new Vector3(0,4,78),0,.065f,t);
        for(int z=60;z<130;z+=14){var l=new GameObject("ApproachLight").AddComponent<Light>();l.transform.parent=t;l.transform.position=new Vector3(0,4,z);l.type=LightType.Point;l.range=16;l.intensity=1.3f;l.color=new Color(.45f,.7f,1);}
    }
    static void BuildBoss()
    {
        var arena=new GameObject("BOSS_CONTROL_DOME").transform;arena.parent=root;arena.position=StationPlan.Point(20,104);arena.rotation=Quaternion.Euler(0,StationPlan.Angle(20),0);
        Shape("CircularCombatDeck",PrimitiveType.Cylinder,new Vector3(0,-.3f,0),new Vector3(44,.3f,44),floor,arena);
        Box("BossApproach",StationPlan.Point(20,70,0,-.25f),new Vector3(7,.5f,28),floor);
        director.bossGate=Gate("BossAirlock",StationPlan.Point(20,60),StationPlan.Angle(20),6.8f);
        for(int side=-1;side<=1;side+=2)Box("ApproachSafetyRail",StationPlan.Point(20,71,side*3.5f,1.2f),new Vector3(.3f,2.4f,27),wall);
        for(int i=0;i<36;i++)
        {
            float a=i*10*Mathf.Deg2Rad;Vector3 pos=new Vector3(Mathf.Sin(a)*22,1.4f,Mathf.Cos(a)*22);
            if(i<16||i>20){var w=Box("ArenaRim",pos,new Vector3(4,2.8f,.5f),wall,arena);w.transform.localRotation=Quaternion.Euler(0,i*10,0);}
            if(i<16||i>20)Box("DomeRib",new Vector3(Mathf.Sin(a)*25,5,Mathf.Cos(a)*25),new Vector3(.5f,10,.5f),trim,arena);
            if(i%4==0)Box("HullFragment",new Vector3(Mathf.Sin(a)*29,3,Mathf.Cos(a)*29),new Vector3(5,6,2),wall,arena);
        }
        for(int i=0;i<4;i++){float a=(45+i*90)*Mathf.Deg2Rad;Box("PeripheralCover",new Vector3(Mathf.Sin(a)*16,.7f,Mathf.Cos(a)*16),new Vector3(3.5f,1.4f,2),trim,arena);}
        var go=new GameObject("GUARDIAN_LAST_WATCH");go.transform.SetParent(arena,false);go.layer=2;
        var bc=go.AddComponent<CapsuleCollider>();bc.isTrigger=true;bc.center=Vector3.up*2.2f;bc.height=5;bc.radius=2.5f;
        var boss=go.AddComponent<StationBoss>();boss.director=director;director.boss=boss;
        var guardianModel=SciFiModel("Enemy_QuadShell",Vector3.zero,7,go.transform);
        boss.body=guardianModel.GetComponentInChildren<Renderer>();
        for(int side=-1;side<=1;side+=2)SciFiModel("Prop_SatelliteDish",new Vector3(side*3,1,0),2,go.transform);
        Shape("IsolationCore",PrimitiveType.Sphere,new Vector3(0,2.2f,.8f),Vector3.one*.6f,red,go.transform,false);
        var warning=Box("AttackTelegraph",Vector3.zero,Vector3.one,Transparent("BossTelegraph",new Color(1,.3f,.08f,.33f)),arena,false);warning.SetActive(false);boss.warning=warning.transform;
        Interact(null,InteractionKind.BossSeal,"Boss 舱：关闭泄漏侧隔离阀",new Vector3(-15,1.1f,0),amber,arena);
        Interact(null,InteractionKind.BossPower,"Boss 舱：给抑制器接入 B 支路",new Vector3(15,1.1f,0),cyan,arena);
        Label("LAST WATCH / ISOLATION PROTOCOL",new Vector3(0,7,18),180,.11f,arena);
        var l=new GameObject("BossEmergencyLight").AddComponent<Light>();l.transform.SetParent(arena,false);l.transform.localPosition=new Vector3(0,10,0);l.type=LightType.Point;l.range=55;l.intensity=2;l.color=new Color(.45f,.65f,1);
    }
    public static void GenerateAndBuild()
    {
        automated=true;
        try {Generate();} finally {automated=false;}
        Directory.CreateDirectory("Builds/Windows");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Windows/LastLightStation.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        File.WriteAllText("Validation/build-result.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
        Debug.Log("LAST_LIGHT_BUILD_SUCCEEDED");
    }
    public static void CapturePreviews()
    {
        EditorSceneManager.OpenScene(ScenePath);
        Directory.CreateDirectory("Validation/Previews");
        var camera=new GameObject("PreviewCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.008f,.014f,.025f);camera.farClipPlane=1400;
        Capture(camera,new Vector3(180,125,190),new Vector3(-12,24,20),"Overview",true,110);
        Capture(camera,new Vector3(2.4f,1.9f,123.5f),new Vector3(0,1,126),"WhiteSwordsman",false);
        Capture(camera,new Vector3(0,3.8f,129),new Vector3(0,1.5f,113),"Teaching",false);
        Capture(camera,StationPlan.Point(1,34,-5,4),StationPlan.Point(1,49,1,1),"RationHall",false);
        Capture(camera,StationPlan.Point(13,34,-5,4),StationPlan.Point(13,48,1,1),"Hydroponics",false);
        Capture(camera,StationPlan.Point(20,85,0,5),StationPlan.Point(20,105,0,2),"BossArena",false);
        Capture(camera,StationPlan.Point(2,37,-5,2.8f),StationPlan.Point(2,47,-3,1),"EnemyCharger",false);
        Capture(camera,StationPlan.Point(8,48,3,5.2f),StationPlan.Point(8,54,8,4),"EnemySentry",false);
        Capture(camera,new Vector3(90,65,90),new Vector3(0,24,0),"VerticalRings",false);
        Capture(camera,StationPlan.Point(15,70,-10,8),StationPlan.Point(18,66,0,8),"ObservationLift",false);
        Capture(camera,StationPlan.Point(11,35,-5,5),StationPlan.Point(11,52,7,3),"UpperWalkway",false);
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
        Debug.Log("LAST_LIGHT_PREVIEWS_CAPTURED");
    }
    static void Capture(Camera camera,Vector3 position,Vector3 look,string name,bool orthographic,float size=100)
    {
        camera.orthographic=orthographic;camera.orthographicSize=size;camera.fieldOfView=65;camera.transform.position=position;camera.transform.LookAt(look);
        var rt=new RenderTexture(1600,1000,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
        File.WriteAllBytes("Validation/Previews/"+name+".png",tex.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
    }
}
