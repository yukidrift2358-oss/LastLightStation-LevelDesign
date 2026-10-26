using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LastLight;
public static partial class StationWhiteboxBuilder {
 static readonly float[] ShaftAngles={240,262.5f,108.75f,123.75f};
 static readonly float[] ShaftLower={0,0,24,24};
 static Vector3 ShaftPoint(int k,Vector3 p){return Polar(ShaftAngles[k],48,ShaftLower[k])+Quaternion.Euler(0,ShaftAngles[k],0)*p;}
 static void CutShafts(List<Vector3> v,List<int> t){
  var nv=new List<Vector3>();var nt=new List<int>();
  for(int i=0;i<t.Count;i+=3){var polys=new List<List<Vector3>>{new List<Vector3>{v[t[i]],v[t[i+1]],v[t[i+2]]}};
   for(int k=0;k<4;k++)if(v[t[i]].y>ShaftLower[k]+.1f&&v[t[i]].y<=ShaftLower[k]+24.5f){var next=new List<List<Vector3>>();
    foreach(var original in polys){var remain=original;for(int plane=0;plane<4&&remain.Count>=3;plane++){var inside=new List<Vector3>();var outside=new List<Vector3>();
     for(int j=0;j<remain.Count;j++){Vector3 a=remain[j],b=remain[(j+1)%remain.Count];float da=HoleDistance(a,k,plane),db=HoleDistance(b,k,plane);bool ia=da<=0,ib=db<=0;
      (ia?inside:outside).Add(a);if(ia!=ib){var point=Vector3.Lerp(a,b,da/(da-db));inside.Add(point);outside.Add(point);}}
     if(outside.Count>=3)next.Add(outside);remain=inside;
    }}polys=next;
   }
   foreach(var poly in polys){int start=nv.Count;nv.AddRange(poly);for(int j=1;j<poly.Count-1;j++)nt.AddRange(new[]{start,start+j,start+j+1});}
  }v.Clear();v.AddRange(nv);t.Clear();t.AddRange(nt);
 }
 static float HoleDistance(Vector3 point,int shaft,int plane){var p=Quaternion.Euler(0,-ShaftAngles[shaft],0)*(point-Polar(ShaftAngles[shaft],48,ShaftLower[shaft]));return plane==0?p.x-2.65f:plane==1?-p.x-2.65f:plane==2?p.z-4.35f:-p.z-4.35f;}
 static void BuildConnections(){var all=new List<StationPassage>();int stair=0;
  for(int i=0;i<StationTopology.Links.Length;i++){var spec=StationTopology.Links[i];var p=new GameObject("PASSAGE_"+StationTopology.Label(spec.a).Split(' ')[0]+"_"+StationTopology.Label(spec.b).Split(' ')[0]).AddComponent<StationPassage>();p.transform.parent=root;p.linkIndex=i;all.Add(p);
   if(spec.kind==PassageKind.Side){float a=StationPlan.Angle(spec.a),d=Mathf.DeltaAngle(a,StationPlan.Angle(spec.b)),mid=a+d*.5f,h=StationPlan.Height(spec.a);int boundary=(StationPlan.Local(spec.a)+1)%StationPlan.Counts[StationPlan.Floor(spec.a)]==StationPlan.Local(spec.b)?spec.a:spec.b;p.gates=new[]{director.sideGates[boundary]};p.points=new[]{Polar(mid-3*Mathf.Sign(d),42,h),Polar(mid+3*Mathf.Sign(d),42,h)};}
   else if(spec.kind==PassageKind.Bridge){p.points=StationPlan.Opposite(spec.a,spec.b)?new[]{StationPlan.Point(spec.a,34),StationPlan.Point(spec.b,34)}:new[]{StationPlan.Point(spec.a,34),Vector3.up*StationPlan.Height(spec.a),StationPlan.Point(spec.b,34)};}
   else if(spec.kind==PassageKind.Lift){int f=StationPlan.Floor(spec.a);p.lift=director.lifts[f];p.points=new[]{StationPlan.Point(spec.a,54),StationPlan.Point(spec.a,63),p.lift.lower,p.lift.upper,StationPlan.Point(spec.b,63),StationPlan.Point(spec.b,54)};}
   else {BuildShaft(p,stair++);}
  }director.passages=all.ToArray();
 }
 static void BuildShaft(StationPassage p,int k){
  var holder=p.transform;holder.position=Polar(ShaftAngles[k],48,ShaftLower[k]);holder.rotation=Quaternion.Euler(0,ShaftAngles[k],0);
  var pts=new List<Vector3>{new Vector3(0,0,-6),new Vector3(-1.1f,0,-3.2f),new Vector3(-1.1f,0,-2.4f)};
  Box("StairBottomDeck",new Vector3(0,-.15f,-3.9f),new Vector3(4.6f,.3f,3.4f),floor,holder);
  for(int n=0;n<8;n++){float lane=n%2==0?-1.1f:1.1f,sign=n%2==0?1:-1;Vector3 a=new Vector3(lane,n*3,-sign*2.4f),b=new Vector3(lane,(n+1)*3,sign*2.4f);
   SlopeDeck("S"+(k+1)+"_Flight"+n,a,b,2,holder);
   for(int side=-1;side<=1;side+=2){var center=(a+b)*.5f+new Vector3(side*1.04f,1,0);var rail=Box("FlightGuard",center,new Vector3(.1f,.15f,Vector3.Distance(a,b)),wall,holder);rail.transform.localRotation=Quaternion.LookRotation(b-a);}
   float landingZ=sign*3.2f;Box("StairLandingDeck",new Vector3(0,(n+1)*3-.15f,landingZ),new Vector3(4.6f,.3f,1.6f),floor,holder);
   Box("LandingOuterGuard",new Vector3(0,(n+1)*3+1,sign*4),new Vector3(4.6f,1.2f,.12f),wall,holder);
   pts.Add(b);pts.Add(new Vector3(lane,(n+1)*3,landingZ));
   if(n<7){pts.Add(new Vector3(-lane,(n+1)*3,landingZ));pts.Add(new Vector3(-lane,(n+1)*3,sign*2.4f));}
  }
  // Final landing opens inward onto the upper ring; rails remain outside the mouth.
  Box("StairTopDeck",new Vector3(0,23.85f,-4.3f),new Vector3(4.6f,.3f,2.6f),floor,holder);
  // The last landing's outward guard is beyond the approach opening and would obstruct it.
  foreach(Transform child in holder)if(child.name=="LandingOuterGuard"&&child.localPosition.y>24)UnityEngine.Object.DestroyImmediate(child.gameObject);
  for(int side=-1;side<=1;side+=2)Box("ShaftSideWall",new Vector3(side*2.6f,12,0),new Vector3(.2f,24,8.6f),trim,holder);
  Box("ShaftOuterWall",new Vector3(0,12,4.4f),new Vector3(5.4f,24,.2f),trim,holder);
  Box("ShaftInnerWall",new Vector3(0,13.5f,-4.4f),new Vector3(5.4f,20.4f,.2f),trim,holder);
  pts.Add(new Vector3(0,24,-6));
  var low=Gate("S"+(k+1)+"_LowerGate",new Vector3(0,0,-4.8f),0,4.6f,holder);var high=Gate("S"+(k+1)+"_UpperGate",new Vector3(0,24,-4.8f),0,4.6f,holder);p.gates=new[]{low,high};
  for(int n=0;n<pts.Count;n++)pts[n]=holder.TransformPoint(pts[n]);if(StationPlan.Height(p.Spec.a)>ShaftLower[k])pts.Reverse();p.points=pts.ToArray();
  Label("S"+(k+1)+" / "+StationTopology.Label(p.Spec.a).Split(' ')[0]+" <> "+StationTopology.Label(p.Spec.b).Split(' ')[0],new Vector3(0,2.6f,-5.2f),180,.045f,holder);
  Label("S"+(k+1)+" / VERTICAL SERVICE PASSAGE",new Vector3(0,26.6f,-5.2f),180,.04f,holder);
  for(int n=0;n<8;n++){var l=new GameObject("StairEmergencyLight").AddComponent<Light>();l.transform.SetParent(holder,false);l.transform.localPosition=new Vector3(0,n*3+2,0);l.type=LightType.Point;l.range=5;l.intensity=.7f;l.color=new Color(.2f,.7f,1);}
 }
 static void SlopeDeck(string name,Vector3 a,Vector3 b,float width,Transform parent){var verts=new[]{a+Vector3.left*width/2,a+Vector3.right*width/2,b+Vector3.right*width/2,b+Vector3.left*width/2,a+Vector3.left*width/2-Vector3.up*.25f,a+Vector3.right*width/2-Vector3.up*.25f,b+Vector3.right*width/2-Vector3.up*.25f,b+Vector3.left*width/2-Vector3.up*.25f};
  var tri=new List<int>();int[] top=Vector3.Cross(verts[1]-verts[0],verts[2]-verts[0]).y>0?new[]{0,1,2,0,2,3}:new[]{0,2,1,0,3,2};tri.AddRange(top);tri.AddRange(new[]{4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7});
  var mesh=new Mesh{name=name};mesh.vertices=verts;mesh.triangles=tri.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();string path="Assets/Generated/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old!=null){EditorUtility.CopySerialized(mesh,old);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
  var o=new GameObject(name);o.transform.SetParent(parent,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=floor;o.AddComponent<MeshCollider>().sharedMesh=mesh;o.AddComponent<StationWalkSurface>();
 }
 static void DeviceSilhouettes(){
  foreach(var item in UnityEngine.Object.FindObjectsOfType<StationInteractable>()){
   bool power=item.kind==InteractionKind.Power||item.kind==InteractionKind.BossPower||item.kind==InteractionKind.TutorialPower;
   bool seal=item.kind==InteractionKind.Seal||item.kind==InteractionKind.BossSeal||item.kind==InteractionKind.TutorialSeal;
   if((!power&&!seal)||item.GetComponent<Renderer>()==null)continue;var t=item.transform;item.GetComponent<Renderer>().enabled=false;
   var children=new List<Transform>();foreach(Transform c in t)children.Add(c);foreach(var c in children)c.SetParent(null,true);Vector3 oldScale=t.localScale;t.localScale=Vector3.one;item.GetComponent<BoxCollider>().size=oldScale;foreach(var c in children)c.SetParent(t,true);
   var screen=t.Find("Screen");if(screen!=null)screen.gameObject.SetActive(false);
   if(power){Box("PowerCabinetShell",Vector3.zero,new Vector3(1.1f,1.7f,.7f),wall,t,false);Box("CabinetPedestal",new Vector3(0,-.9f,0),new Vector3(1.25f,.15f,.85f),trim,t,false);
    Box("ChannelA",new Vector3(-.25f,.25f,-.38f),new Vector3(.32f,.95f,.07f),amber,t,false);Box("ChannelB",new Vector3(.25f,.25f,-.38f),new Vector3(.32f,.95f,.07f),cyan,t,false);
    Label("A     B",new Vector3(0,.86f,-.4f),180,.055f,t);
   }else{var pipe=Shape("SealPipe",PrimitiveType.Cylinder,new Vector3(0,-.4f,0),new Vector3(.48f,.7f,.48f),wall,t,false);pipe.transform.localRotation=Quaternion.Euler(0,0,90);
    Shape("ValveNeck",PrimitiveType.Cylinder,new Vector3(0,-.05f,0),new Vector3(.22f,.3f,.22f),trim,t,false);
    var wheel=Ring("IsolationWheel",.42f,.055f,new Vector3(0,.25f,.15f),amber,t);wheel.localRotation=Quaternion.Euler(75,0,0);
    for(int j=0;j<3;j++){var spoke=Box("WheelSpoke",new Vector3(0,.25f,.15f),new Vector3(.78f,.055f,.055f),amber,t,false);spoke.transform.rotation=wheel.rotation*Quaternion.Euler(0,j*60,0);}
    Box("PipeFoot",new Vector3(0,-.85f,0),new Vector3(.65f,.12f,.65f),trim,t,false);
   }
  }
  foreach(var item in UnityEngine.Object.FindObjectsOfType<StationInteractable>())if(item.transform.Find("PowerCabinetShell")!=null||item.transform.Find("IsolationWheel")!=null){var visual=item.gameObject.AddComponent<StationDeviceVisual>();visual.item=item;visual.status=item.transform.Find("PowerCabinetShell")!=null?new[]{item.transform.Find("ChannelA").GetComponent<Renderer>(),item.transform.Find("ChannelB").GetComponent<Renderer>()}:new[]{item.transform.Find("IsolationWheel").GetComponent<Renderer>()};}
  foreach(var h in UnityEngine.Object.FindObjectsOfType<StationHazard>())if(h.suppressible){var old=h.transform.Find(h.tutorial?"ScannerBody":"DamagedUnit");if(old!=null)old.gameObject.SetActive(false);
   var t=h.transform;Ring("OpenScannerRing",.65f,.07f,new Vector3(0,.1f,0),red,t).localRotation=Quaternion.Euler(90,0,0);
   Box("ScannerStand",new Vector3(0,-.75f,0),new Vector3(.12f,.8f,.12f),trim,t,false);for(int j=-1;j<=1;j++){var leg=Box("ScannerTripod",new Vector3(j*.3f,-1.14f,0),new Vector3(.15f,.1f,.6f),wall,t,false);}
   Shape("FloatingScannerLens",PrimitiveType.Sphere,new Vector3(0,.1f,0),Vector3.one*.25f,cyan,t,false);
   var state=h.gameObject.AddComponent<StationDeviceVisual>();state.hazard=h;state.status=new[]{t.Find("OpenScannerRing").GetComponent<Renderer>()};
  }
 }
 static Transform Ring(string name,float radius,float tube,Vector3 pos,Material material,Transform parent){string path="Assets/Generated/Meshes/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh{name=name};var v=new List<Vector3>();var t=new List<int>();for(int j=0;j<=40;j++)for(int k=0;k<=8;k++){float a=j*Mathf.PI*2/40,b=k*Mathf.PI*2/8;v.Add(new Vector3(Mathf.Cos(a)*(radius+tube*Mathf.Cos(b)),tube*Mathf.Sin(b),Mathf.Sin(a)*(radius+tube*Mathf.Cos(b))));}for(int j=0;j<40;j++)for(int k=0;k<8;k++){int i=j*9+k;t.AddRange(new[]{i,i+9,i+1,i+1,i+9,i+10});}mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,path);}
  var o=new GameObject(name);o.transform.SetParent(parent,false);var inv=new Vector3(1/parent.lossyScale.x,1/parent.lossyScale.y,1/parent.lossyScale.z);o.transform.localPosition=Vector3.Scale(pos,inv);o.transform.localScale=inv;o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=material;return o.transform;
 }
}
