using System.Collections.Generic;
using UnityEngine;

namespace LastLight
{
    // A short-lived physics grid follows the actual open doors and rotated bridge.
    // Every segment is checked against the floor and walls, including service ramps.
    public sealed class StationGuidance : MonoBehaviour
    {
        public StationDirector director;
        public Material material;
        public Mesh arrowMesh;
        public readonly List<Vector3> PathPoints=new List<Vector3>();
        readonly List<Transform> arrows=new List<Transform>();
        readonly List<Renderer> arrowRenderers=new List<Renderer>();
        readonly Dictionary<Vector2Int,Vector3> cells=new Dictionary<Vector2Int,Vector3>();
        readonly HashSet<Vector2Int> invalid=new HashSet<Vector2Int>();
        MaterialPropertyBlock properties;
        LineRenderer line;
        readonly RaycastHit[] floorHits=new RaycastHit[24];
        float nextUpdate,baseY;
        int serial;
        const float Grid=1.25f;
        struct Entry {public Vector2Int cell;public float score;public int serial;}
        sealed class EntryComparer:IComparer<Entry>
        {public int Compare(Entry a,Entry b){int c=a.score.CompareTo(b.score);return c!=0?c:a.serial.CompareTo(b.serial);}}
        void Awake()
        {
            properties=new MaterialPropertyBlock();
            line=new GameObject("ActiveRouteLightStrip").AddComponent<LineRenderer>();
            line.transform.SetParent(transform,false);line.sharedMaterial=material;
            line.startWidth=line.endWidth=.16f;line.startColor=new Color(.18f,.40f,.42f);line.endColor=new Color(.60f,.39f,.16f);
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            for(int i=0;i<112;i++)
            {
                var go=new GameObject("ForwardLightArrow");go.transform.SetParent(transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=arrowMesh;
                var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;
                r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
                arrows.Add(go.transform);arrowRenderers.Add(r);go.SetActive(false);
            }
        }
        void Update()
        {
            if(director.Frozen)return;
            if(Time.time>=nextUpdate){nextUpdate=Time.time+.85f;Rebuild();}
            for(int i=0;i<arrows.Count;i++)if(arrows[i].gameObject.activeSelf)
            {
                float glow=.4f+.6f*Mathf.Pow(.5f+.5f*Mathf.Sin(Time.time*5-i*.65f),3);
                Color color=i>=ActiveArrowCount-2?new Color(.65f,.40f,.16f):new Color(.16f,.43f,.46f);
                properties.SetColor("_Color",color*glow);arrowRenderers[i].SetPropertyBlock(properties);
            }
        }
        public int ActiveArrowCount {get;private set;}
        public void Rebuild()
        {
            PathPoints.Clear();cells.Clear();invalid.Clear();serial=0;
            var player=director.player.transform.position;
            int floor=player.y>40?2:player.y>16?1:0;baseY=floor*24;
            // During the safe lift ride, fixed vertical beacons take over until landing.
            bool riding=false;
            foreach(var lift in director.lifts)if(lift!=null&&Mathf.Abs(player.y-baseY)>5&&Vector2.Distance(new Vector2(player.x,player.z),new Vector2(lift.lower.x,lift.lower.z))<3.5f)riding=true;
            if(!riding&&!director.Bridging&&!director.AppendTravelGuide(PathPoints))FindPath(player,director.ObjectivePosition);
            DrawPath();
        }
        Vector2Int Key(Vector3 p){return new Vector2Int(Mathf.RoundToInt(p.x/Grid),Mathf.RoundToInt(p.z/Grid));}
        bool Cell(Vector2Int key,out Vector3 position)
        {
            if(cells.TryGetValue(key,out position))return true;
            if(invalid.Contains(key))return false;
            Vector3 top=new Vector3(key.x*Grid,baseY+5.5f,key.y*Grid);
            if(Mathf.Abs(top.x)>80||top.z< -135||top.z>140){invalid.Add(key);return false;}
            int hits=Physics.RaycastNonAlloc(top,Vector3.down,floorHits,7,1,QueryTriggerInteraction.Ignore);
            bool found=false;float highest=float.MinValue;Vector3 floorPoint=Vector3.zero;
            for(int i=0;i<hits;i++)
            {
                var hit=floorHits[i];
                if(hit.normal.y>.65f&&hit.collider.GetComponent<StationWalkSurface>()!=null&&hit.point.y>highest)
                {highest=hit.point.y;floorPoint=hit.point;found=true;}
            }
            if(!found){invalid.Add(key);return false;}
            position=floorPoint;
            if(Physics.CheckCapsule(position+Vector3.up*.52f,position+Vector3.up*1.4f,.32f,1,QueryTriggerInteraction.Ignore))
            {invalid.Add(key);return false;}
            cells[key]=position;return true;
        }
        bool Near(Vector3 target,out Vector2Int key)
        {
            var center=Key(target);key=center;float best=float.MaxValue;bool found=false;
            for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++)
            {
                var k=center+new Vector2Int(x,z);
                if(Cell(k,out var p)&&p.y<=target.y+.25f)
                {
                    float distance=Vector3.Distance(p,target);
                    if(distance<best){best=distance;key=k;found=true;}
                }
            }
            return found&&best<3.5f;
        }
        bool Edge(Vector3 a,Vector3 b)
        {
            if(Mathf.Abs(a.y-b.y)>1.05f)return false;
            var d=b-a;
            return !Physics.SphereCast(a+Vector3.up*.85f,.3f,d.normalized,out _,d.magnitude,1,QueryTriggerInteraction.Ignore);
        }
        void FindPath(Vector3 start,Vector3 target)
        {
            if(!Near(start,out var from))return;
            var to=Key(target);var goals=new HashSet<Vector2Int>();
            bool upper=target.y>baseY+2.5f;
            for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++)
            {
                var key=to+new Vector2Int(x,z);
                if(Cell(key,out var point)&&Vector3.Distance(point,target)<3.2f&&(upper?point.y>baseY+2.5f:point.y<baseY+1.1f))goals.Add(key);
            }
            if(goals.Count==0)return;
            Vector2Int reached=from;
            var open=new SortedSet<Entry>(new EntryComparer());
            var cost=new Dictionary<Vector2Int,float>();var parent=new Dictionary<Vector2Int,Vector2Int>();var closed=new HashSet<Vector2Int>();
            cost[from]=0;open.Add(new Entry{cell=from,score=Vector2Int.Distance(from,to),serial=serial++});
            bool found=false;
            while(open.Count>0&&closed.Count<6000)
            {
                var entry=open.Min;open.Remove(entry);var current=entry.cell;
                if(!closed.Add(current))continue;
                if(goals.Contains(current)){found=true;reached=current;break;}
                Cell(current,out var a);
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                {
                    if(x==0&&z==0)continue;var next=current+new Vector2Int(x,z);
                    if(closed.Contains(next)||!Cell(next,out var b)||!Edge(a,b))continue;
                    float candidate=cost[current]+Vector3.Distance(a,b)/Grid;
                    if(cost.TryGetValue(next,out var old)&&old<=candidate)continue;
                    cost[next]=candidate;parent[next]=current;
                    open.Add(new Entry{cell=next,score=candidate+Vector2Int.Distance(next,to),serial=serial++});
                }
            }
            if(!found)return;
            var cursor=reached;PathPoints.Add(cells[cursor]+Vector3.up*.08f);
            while(cursor!=from){cursor=parent[cursor];PathPoints.Add(cells[cursor]+Vector3.up*.08f);}
            PathPoints.Reverse();
        }
        void DrawPath()
        {
            line.positionCount=PathPoints.Count;
            for(int i=0;i<PathPoints.Count;i++)line.SetPosition(i,PathPoints[i]);
            int index=0;float carry=0;
            for(int i=1;i<PathPoints.Count;i++)
            {
                Vector3 a=PathPoints[i-1],d=PathPoints[i]-a;float length=d.magnitude;
                for(float t=carry;t<length&&index<arrows.Count;t+=2)
                {
                    var arrow=arrows[index++];arrow.gameObject.SetActive(true);arrow.position=a+d.normalized*t+Vector3.up*.015f;
                    arrow.rotation=Quaternion.LookRotation(d.normalized,Vector3.up);
                }
                carry=(carry-length)%2;if(carry<0)carry+=2;
            }
            ActiveArrowCount=index;
            for(;index<arrows.Count;index++)arrows[index].gameObject.SetActive(false);
        }
    }
}
