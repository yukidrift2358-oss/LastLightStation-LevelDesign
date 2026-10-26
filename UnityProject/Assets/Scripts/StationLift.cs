using UnityEngine;
namespace LastLight
{
    public sealed class StationLift:MonoBehaviour
    {
        public StationDirector director;
        public Transform platform;
        public Vector3 lower,upper;
        public bool Unlocked;
        public int destinationRoom;
        public StationGate[] gates;
        public bool Moving {get;private set;}
        bool toUpper=true,announced;
        float departure;
        bool mustLeave;
        public void Unlock(){Unlocked=true;foreach(var gate in gates)gate.SetOpen(true);}
        void Update()
        {
            if(!Unlocked||director.Frozen)return;
            foreach(var gate in gates)if(!gate.IsOpen)gate.SetOpen(true);
            Vector3 p=director.player.transform.position;
            bool aboard=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(platform.position.x,platform.position.z))<2.4f&&p.y-platform.position.y>=-.2f&&p.y-platform.position.y<1;
            if(!Moving)
            {
                if(!aboard)mustLeave=false;
                if(aboard&&!mustLeave){if(departure==0)departure=Time.time+1.5f;if(Time.time>=departure)Moving=true;}
                else {departure=0;Vector3 nearest=Mathf.Abs(p.y-lower.y)<Mathf.Abs(p.y-upper.y)?lower:upper;if(Vector3.Distance(p,nearest)<7&&Vector3.Distance(platform.position,nearest)>1){toUpper=nearest==upper;Moving=true;}}
            }
            if(!Moving)return;
            var target=toUpper?upper:lower;
            Vector3 delta=Vector3.MoveTowards(platform.position,target,3.2f*Time.deltaTime)-platform.position;
            Vector3 rider=director.player.transform.position;
            platform.position+=delta;
            Physics.SyncTransforms();
            // Preserve the rider's offset on this solid, ceiling-free lift. CharacterController.Move
            // also depenetrates against moving geometry and would otherwise apply deck motion twice.
            if(aboard)director.player.Warp(rider+delta);
            if(Vector3.Distance(platform.position,target)<.01f)
            {
                Moving=false;departure=0;toUpper=!toUpper;mustLeave=aboard;
                if(aboard&&target==upper&&!announced){announced=true;director.player.SetCheckpoint(StationPlan.Point(destinationRoom,60,0,.15f));director.player.Restore();director.Notify("抵达 "+StationPlan.FloorNames[StationPlan.Floor(destinationRoom)]+"。从这里可以回望刚才的路线。");}
            }
        }
    }
}
