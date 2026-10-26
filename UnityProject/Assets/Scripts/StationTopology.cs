using System.Collections.Generic;
namespace LastLight {
 public enum PassageKind { Side, Bridge, Stair, Lift }
 [System.Serializable] public sealed class StationLink {
  public int a,b;public PassageKind kind;public uint required; public float angle;
  public StationLink(int a,int b,PassageKind k,uint r,float angle=0){this.a=a;this.b=b;kind=k;required=r;this.angle=angle;}
  public int Other(int id){return id==a?b:a;}
 }
 public static class StationTopology {
  public static readonly int[] RouteA={0,1,5,2,3,6,7,8,4,9,10,11,17,12,13,14,15,18,16,19,23,22,21,25,24,20};
  public static readonly int[] RouteB={0,1,5,2,3,6,7,11,17,12,8,4,9,10,13,14,15,18,16,19,23,22,21,25,24,20};
  public const uint All=(1u<<26)-1;
  public static readonly StationLink[] Links=Build();
  static StationLink[] Build(){var l=new List<StationLink>();
   int[] prefix={0,1,5,2,3,6,7},x={8,4,9,10},y={11,17,12},tail={13,14,15,18,16,19,23,22,21,25,24,20};
   Chain(l,prefix);Add(l,7,8);Add(l,7,11);Chain(l,x);Chain(l,y);Add(l,10,11);Add(l,12,8);
   uint both=0;foreach(int id in x)both|=1u<<id;foreach(int id in y)both|=1u<<id;
   Add(l,10,13,both);Add(l,12,13,both);Chain(l,tail);return l.ToArray();}
  static void Chain(List<StationLink> l,int[] r){for(int i=1;i<r.Length;i++)Add(l,r[i-1],r[i]);}
  static void Add(List<StationLink> l,int a,int b,uint req=0){PassageKind k=PassageKind.Bridge;float angle=0;
   if((a==3&&b==6)||(a==15&&b==18))k=PassageKind.Lift;
   else if(StationPlan.Floor(a)!=StationPlan.Floor(b)){k=PassageKind.Stair;angle=a==8?240:a==4?262.5f:a==18?108.75f:123.75f;}
   else if(StationPlan.Adjacent(a,b))k=PassageKind.Side;
   l.Add(new StationLink(a,b,k,req==0?1u<<a:req,angle));}
  public static string Label(int id){return "L"+(StationPlan.Floor(id)+1)+"-"+(StationPlan.Local(id)+1).ToString("00")+" "+StationPlan.Names[id];}
 }
}