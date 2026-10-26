using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastLight
{
    public sealed class StationDirector : MonoBehaviour
    {
        public StationPlayer player;
        public StationPassage[] passages;
        public uint CompletedMask {get;private set;}
        public int ActiveSector {get;private set;}
        public bool RouteChoiceOpen {get;private set;}
        public int[] DestinationChoices {get;private set;}=new int[0];
        public StationPassage Travel {get;private set;}
        public int TravelFrom {get;private set;}
        public int TravelTo {get;private set;}
        public int Waypoint {get;private set;}
        Vector3[] travelPoints;
        Coroutine connection;
        Vector3 previousPlayerPosition;
        public Vector3 TravelTarget {get {return travelPoints!=null?travelPoints[Mathf.Min(Waypoint,travelPoints.Length-1)]:player.transform.position;}}
        public float FallLimit {get {return Travel!=null?Mathf.Min(StationPlan.Height(TravelFrom),StationPlan.Height(TravelTo))-7:StationPlan.Height(ActiveSector)-7;}}
        public StationRoom[] rooms;
        public StationGate[] sideGates;
        public StationGate[] dockGates;
        public StationGate[] tutorialGates;
        public StationGate bossGate;
        public Transform bridge;
        public Transform[] bridges;
        public StationLift[] lifts;
        public StationBoss boss;
        public StationEnemy tutorialEnemy;
        public StationHazard teachingLeak;
        public StationInteractable[] tutorialObjectives;
        public Material pulseMaterial,attackMaterial;
        public int TutorialStage {get;private set;}
        public int Step {get;private set;}
        public bool Paused {get;private set;}
        public bool Won {get;private set;}
        public bool MapVisible {get;private set;}
        public bool Bridging {get;private set;}
        public bool RoomEntered {get;private set;}
        public string Notification {get;private set;}
        public float NoticeUntil {get;private set;}
        public string RecordTitle {get;private set;}
        public string RecordText {get;private set;}
        public bool RecordOpen {get;private set;}
        public int RecordSector {get;private set;}=-1;
        public bool Frozen {get {return Paused||MapVisible||RecordOpen||RouteChoiceOpen||Won;}}
        public int RecordCount {get;private set;}
        public StationRoom CurrentRoom {get {return rooms[ActiveSector];}}
        public Vector3 ObjectivePosition
        {
            get
            {
                if(TutorialStage<4)return tutorialObjectives[TutorialStage].transform.position;
                if(Travel!=null)return TravelTarget;
                if(Step>=StationPlan.Count)return boss.transform.position;
                if(!RoomEntered)return rooms[ActiveSector].entryPoint;
                var r=CurrentRoom;
                if(r.Complete){foreach(var exit in r.GetComponentsInChildren<StationInteractable>())if(exit.kind==InteractionKind.Exit)return exit.transform.position;}
                // Guide to the actual next operation, not just the final locked exit.
                InteractionKind next=InteractionKind.Exit;
                if((r.Need&RoomNeed.Seal)!=0&&!r.Sealed)next=InteractionKind.Seal;
                else if(StationPlan.Sync(r.sector)&&!r.PowerB)next=InteractionKind.Power;
                else if(StationPlan.Timed(r.sector)&&(r.Need&RoomNeed.Relay)!=0&&r.RelayProgress<3){foreach(var i in r.GetComponentsInChildren<StationInteractable>())if(i.kind==InteractionKind.Relay&&i.relayIndex==r.NextRelay)return AccessibleObjective(r,i.transform.position);}
                else if((r.Need&RoomNeed.Pulse)!=0&&!r.PulseSolved&&r.scanner!=null)return r.scanner.transform.position;
                else if((r.Need&RoomNeed.Record)!=0&&!r.HasRecord)next=r.PowerB?InteractionKind.Power:InteractionKind.Record;
                else if((r.Need&RoomNeed.Combat)!=0&&!r.Cleared)
                {foreach(var e in r.enemies)if(e!=null&&!e.Dead)return AccessibleObjective(r,e.transform.position);}
                else if((r.Need&RoomNeed.Relay)!=0&&r.RelayProgress<3){foreach(var i in r.GetComponentsInChildren<StationInteractable>())if(i.kind==InteractionKind.Relay&&i.relayIndex==r.NextRelay)return AccessibleObjective(r,i.transform.position);}
                else if((r.Need&RoomNeed.Power)!=0&&!r.PowerB)next=InteractionKind.Power;
                else if((StationPlan.Timed(r.sector)||StationPlan.Sync(r.sector))&&r.scanner!=null&&!r.scanner.SuppressedNow)return r.scanner.transform.position;
                foreach(var i in r.GetComponentsInChildren<StationInteractable>())if(i.kind==next)return AccessibleObjective(r,i.transform.position);
                return r.transform.position;
            }
        }
        Vector3 AccessibleObjective(StationRoom room,Vector3 target) {return target;}
        void Start()
        {
            Step=0;TutorialStage=0;
            Notify("调查任务：居民曾主动将守望站放逐深空，以隔离感染。恢复维护通路、收集事件档案，并把姓名传回家园。沿流动灯带学习操作；J 可回看已读档案。");
        }
        public void Notify(string message) {Notification=message;NoticeUntil=Time.unscaledTime+6;}
        public bool IsCurrentRoom(StationRoom room) {return TutorialStage>=4&&room!=null&&room==CurrentRoom&&RoomEntered&&!room.Complete;}
        public bool CanUseRoom(StationRoom room) {return TutorialStage>=4&&Travel==null&&room==CurrentRoom&&RoomEntered;}
        public void CompleteTutorial(int stage)
        {
            if(TutorialStage!=stage)return;
            tutorialGates[stage].SetOpen(true);TutorialStage++;
            if(stage==1&&teachingLeak!=null)teachingLeak.Powered=false;
            player.SetCheckpoint(new Vector3(0,.15f,108-stage*14));
            string[] messages={"电力分流：已切换到通路支路。前方气密门开启。","气密隔离：泄漏已关闭。前往红色扫描装置附近，按 Q 抑制。","抑制有效：短按暂停 4 秒；长按 Q 蓄满后松开，范围更大、暂停 7 秒。下一段靠近机械体，左键攻击，Shift 闪避。","教学完成。进入圆环 01 区，沿任务提示恢复维护权限。"};
            Notify(messages[stage]);
        }
        public void EnterRoom(StationRoom room)
        {
            if(TutorialStage<4||Travel!=null||room!=CurrentRoom||RoomEntered)return;
            RoomEntered=true;player.SetCheckpoint(player.transform.position);
            if(StationPlan.Rest(room.sector))player.Restore();
            Notify("进入 "+StationTopology.Label(room.sector)+"。"+(room.Complete?"维护已完成，可回看记录或选择通路。":StationPlan.Novelty(room.sector)));
        }
        public int[] AvailableDestinations(bool retreatOnly=false)
        {
            var options=new System.Collections.Generic.List<int>();
            foreach(var link in StationTopology.Links)if((link.a==ActiveSector||link.b==ActiveSector)&&(CompletedMask&link.required)==link.required){int other=link.Other(ActiveSector);if(!retreatOnly||rooms[other].Complete)options.Add(other);}
            return options.ToArray();
        }
        public void TryCompleteRoom(StationRoom room)
        {
            if(Travel!=null||!RoomEntered||room!=CurrentRoom)return;
            if(!room.Complete){string missing=room.Missing();if(missing.Length>0){Notify("出口未就绪："+missing+"。可以退回已完成的区域。");OpenChoices(true);return;}
                room.MarkComplete();CompletedMask|=1u<<room.sector;Step++;
            }
            if(CompletedMask==StationTopology.All&&room.sector==20){if(bossGate.IsOpen)OpenChoices(false);else {bossGate.SetOpen(true);Notify("全部26区已留档。外侧航行控制舱开启；再次使用出口可以返回已完成区域。");}return;}
            var choices=AvailableDestinations();int single=-1,count=0;foreach(int id in choices)if(!rooms[id].Complete){single=id;count++;}
            if(count==1)ChooseDestination(single);else OpenChoices(false);
        }
        void OpenChoices(bool retreatOnly){DestinationChoices=AvailableDestinations(retreatOnly);if(DestinationChoices.Length==0)return;RouteChoiceOpen=true;ApplyPause();}
        public void CloseChoices(){RouteChoiceOpen=false;ApplyPause();}
        public bool ChooseDestination(int to)
        {
            if(Travel!=null)return false;
            StationPassage chosen=null;foreach(var p in passages)if((p.Spec.a==ActiveSector||p.Spec.b==ActiveSector)&&p.Spec.Other(ActiveSector)==to&&(CompletedMask&p.Spec.required)==p.Spec.required){chosen=p;break;}
            if(chosen==null)return false;
            if(chosen.Spec.kind==PassageKind.Bridge&&new Vector2(player.transform.position.x,player.transform.position.z).magnitude<31){Notify("请在扇区内选择桥的对接目的地。");return false;}
            CloseChoices();TravelFrom=ActiveSector;TravelTo=to;Travel=chosen;RoomEntered=false;Waypoint=0;
            travelPoints=chosen.Path(ActiveSector);chosen.Unlock();player.SetCheckpoint(travelPoints[0]+Vector3.up*.12f);
            Notify("前往 "+StationTopology.Label(to)+"。"+(chosen.Spec.kind==PassageKind.Stair?"沿建筑折返通道跨层，留意另一高度的入口。":chosen.Spec.kind==PassageKind.Bridge&&!StationPlan.Opposite(TravelFrom,to)?"先到原桥轴心，站稳等待转向，再走向目标。":"沿流动灯带前进。"));
            if(chosen.Spec.kind==PassageKind.Bridge)connection=StartCoroutine(BridgeTrip());
            return true;
        }
        void CloseDocks(int floor){for(int i=StationPlan.Offsets[floor];i<StationPlan.Offsets[floor]+StationPlan.Counts[floor];i++)dockGates[i].SetOpen(false);}
        IEnumerator Rotate(Quaternion target,bool rider)
        {
            Bridging=true;var start=bridge.rotation;Vector3 local=bridge.InverseTransformPoint(player.transform.position);
            for(float t=0;t<2.5f;t+=Time.deltaTime){bridge.rotation=Quaternion.Slerp(start,target,Mathf.SmoothStep(0,1,t/2.5f));if(rider)player.Warp(bridge.TransformPoint(local));Physics.SyncTransforms();yield return null;}
            bridge.rotation=target;if(rider)player.Warp(bridge.TransformPoint(local));Physics.SyncTransforms();Bridging=false;
        }
        IEnumerator BridgeTrip()
        {
            int f=StationPlan.Floor(TravelFrom);bridge=bridges[f];CloseDocks(f);
            yield return Rotate(Quaternion.Euler(0,StationPlan.Angle(TravelFrom),0),false);
            dockGates[TravelFrom].SetOpen(true);
            bool middle=!StationPlan.Opposite(TravelFrom,TravelTo);
            if(!middle)dockGates[TravelTo].SetOpen(true);
            if(middle){Waypoint=1;while(new Vector2(player.transform.position.x,player.transform.position.z).magnitude>1||Mathf.Abs(player.transform.position.y-f*24)>.6f)yield return null;
                CloseDocks(f);player.CancelPulseCharge();Notify("桥轴心锁定，正在转向。站稳后沿金色端点离桥。");
                yield return Rotate(Quaternion.Euler(0,StationPlan.Angle(TravelTo),0),true);dockGates[TravelTo].SetOpen(true);Waypoint=2;
            }else Waypoint=1;
            connection=null;
        }
        bool InsideSector(int id,Vector3 p)
        {
            float radius=new Vector2(p.x,p.z).magnitude;
            return Mathf.Abs(p.y-StationPlan.Height(id))<.65f&&radius>29&&radius<56&&Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg,StationPlan.Angle(id)))<StationPlan.Width(id)/2-.35f;
        }
        void FollowOpenPassages()
        {
            Vector3 p=player.transform.position,delta=p-previousPlayerPosition;float radius=new Vector2(p.x,p.z).magnitude;
            float radialMove=Vector3.Dot(new Vector3(p.x,0,p.z).normalized,delta);
            foreach(var passage in passages){var spec=passage.Spec;if((spec.a!=ActiveSector&&spec.b!=ActiveSector)||(CompletedMask&spec.required)!=spec.required)continue;int other=spec.Other(ActiveSector);
                if(spec.kind==PassageKind.Side&&passage.gates[0].IsOpen&&InsideSector(other,p)){ActiveSector=other;RoomEntered=false;EnterRoom(CurrentRoom);return;}
                if(spec.kind==PassageKind.Stair&&passage.gates[0].IsOpen){Vector3 local=passage.transform.InverseTransformPoint(p);if(Mathf.Abs(p.y-StationPlan.Height(ActiveSector))<2.5f&&Mathf.Abs(local.x)<2.3f&&local.z> -5.15f&&local.z< -1.5f&&passage.transform.InverseTransformPoint(previousPlayerPosition).z< -4.2f&&Vector3.Dot(delta,passage.transform.forward)>.001f){ChooseDestination(other);return;}}
                if(spec.kind==PassageKind.Lift&&passage.lift.Unlocked&&radius>56.5f&&radius<63&&radialMove>.001f&&Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg,StationPlan.Angle(ActiveSector)))<5){ChooseDestination(other);return;}
                if(spec.kind==PassageKind.Bridge&&StationPlan.Opposite(ActiveSector,other)&&dockGates[ActiveSector].IsOpen&&dockGates[other].IsOpen&&radius>27&&radius<33.5f&&radialMove< -.001f&&Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg,StationPlan.Angle(ActiveSector)))<5){Travel=passage;TravelFrom=ActiveSector;TravelTo=other;RoomEntered=false;travelPoints=passage.Path(ActiveSector);Waypoint=1;player.SetCheckpoint(travelPoints[0]+Vector3.up*.12f);return;}
            }
        }
        bool AtDestination()
        {
            Vector3 p=player.transform.position;float radius=new Vector2(p.x,p.z).magnitude;
            if(Mathf.Abs(p.y-StationPlan.Height(TravelTo))>.65f||radius<29||radius>56)return false;
            if(Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg,StationPlan.Angle(TravelTo)))>StationPlan.Width(TravelTo)/2-.35f)return false;
            if(Travel.Spec.kind==PassageKind.Bridge)return connection==null&&radius>32;
            if(Travel.Spec.kind==PassageKind.Stair)return Travel.transform.InverseTransformPoint(p).z< -4.85f;
            return true;
        }
        void FollowStairPosition()
        {
            float best=2.6f;int next=-1;Vector3 playerPoint=player.transform.position;
            for(int i=0;i<travelPoints.Length-1;i++){Vector3 a=travelPoints[i],d=travelPoints[i+1]-a;float t=Mathf.Clamp01(Vector3.Dot(playerPoint-a,d)/d.sqrMagnitude);float distance=Vector3.Distance(playerPoint,a+d*t);if(distance<best-.005f||(Mathf.Abs(distance-best)<.005f&&i+1>next)){best=distance;next=i+1;}}
            // A capsule rides slightly above sloped contact planes. Allow a small landing approach,
            // otherwise nearest-segment projection can keep pointing at the end of a descending ramp.
            for(int i=0;i<travelPoints.Length-1;i++){Vector3 d=playerPoint-travelPoints[i];if(Mathf.Abs(d.y)<.55f&&new Vector2(d.x,d.z).magnitude<.6f)next=Mathf.Max(next,i+1);}
            if(next>=0)Waypoint=next;
        }
        void FinishTravel(){ActiveSector=TravelTo;Travel=null;travelPoints=null;Waypoint=0;EnterRoom(CurrentRoom);}
        public bool AppendTravelGuide(System.Collections.Generic.List<Vector3> path)
        {
            if(Travel==null||Travel.Spec.kind!=PassageKind.Stair)return false;
            if(Waypoint==0&&Vector3.Distance(player.transform.position,travelPoints[0])>2)return false;
            path.Add(player.transform.position+Vector3.up*.08f);for(int i=Waypoint;i<travelPoints.Length;i++)path.Add(travelPoints[i]+Vector3.up*.08f);return true;
        }
        public void ShowRecord(string title,string body)
        {
            RecordTitle=title;RecordText=body;RecordSector=System.Array.IndexOf(StationPlan.Records,title);RecordOpen=true;
            RecordCount=0;foreach(var r in rooms)if(r.HasRecord)RecordCount++;
            ApplyPause();
        }
        public void BrowseRecord(int direction)
        {
            for(int n=1;n<=StationPlan.Count;n++){int i=(RecordSector+direction*n+StationPlan.Count*2)%StationPlan.Count;if(rooms[i].HasRecord){ShowRecord(StationPlan.Records[i],StationPlan.Stories[i]);return;}}
        }
        public void ToggleArchive()
        {
            if(RecordOpen){CloseRecord();return;}if(Frozen)return;
            for(int i=0;i<rooms.Length;i++)if(rooms[i].HasRecord){ShowRecord(StationPlan.Records[i],StationPlan.Stories[i]);return;}
            Notify("尚无已留档文件。接近档案终端按 E 读取后，可随时用 J 回看。");
        }
        public string ObjectiveName
        {
            get{
                if(Travel!=null)return "前往 "+StationArchive.Location(TravelTo)+(StationPlan.Floor(TravelTo)>StationPlan.Floor(TravelFrom)?" ↑上行":StationPlan.Floor(TravelTo)<StationPlan.Floor(TravelFrom)?" ↓下行":"");
                if(TutorialStage<4)return "学习 "+new[]{"电力分流","气密隔离","Q 抑制","近战与闪避"}[TutorialStage];
                if(Step>=StationPlan.Count)return "航行控制舱 / 解除守门者";
                return StationArchive.Location(ActiveSector);
            }
        }
        public void CloseRecord() {RecordOpen=false;ApplyPause();}
        void ApplyPause()
        {
            if(Frozen)player.CancelPulseCharge();
            bool pause=Paused||MapVisible||RecordOpen||RouteChoiceOpen||Won;
            Time.timeScale=pause?0:1;Cursor.lockState=pause?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=pause;
        }
        public void TogglePause() {Paused=!Paused;ApplyPause();}
        public void ToggleMap() {MapVisible=!MapVisible;ApplyPause();}
        public void Restart() {Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
        public void OnRespawn()
        {
            if(TutorialStage==3)tutorialEnemy.ResetEnemy();
            if(Travel!=null){if(connection!=null)StopCoroutine(connection);Bridging=false;Waypoint=0;if(Travel.Spec.kind==PassageKind.Bridge)connection=StartCoroutine(BridgeTrip());}
            else if(CurrentRoom!=null&&!CurrentRoom.Complete)CurrentRoom.ResetThreats();
            if(Step==StationPlan.Count)boss.ResetBoss();
            Notify("已返回本段检查点。已取得的任务记录保留。");
        }
        public void Win()
        {
            Won=true;ApplyPause();
            Notify("守望站，最后一次交接完成。");
        }
        public void PulseVisual(Vector3 point,bool charged=false) {StartCoroutine(Effect(point+Vector3.up*.15f,charged?attackMaterial:pulseMaterial,charged?32:18,charged?.8f:.45f));}
        public void AttackVisual(Vector3 point) {StartCoroutine(Effect(point,attackMaterial,2,.16f));}
        IEnumerator Effect(Vector3 point,Material material,float size,float duration)
        {
            var effect=GameObject.CreatePrimitive(PrimitiveType.Sphere);effect.name="PulseFeedback";
            Destroy(effect.GetComponent<Collider>());effect.GetComponent<Renderer>().sharedMaterial=material;
            effect.transform.position=point;
            for(float t=0;t<duration;t+=Time.deltaTime){effect.transform.localScale=Vector3.one*Mathf.Lerp(.2f,size,t/duration);yield return null;}
            Destroy(effect);
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)) {if(RouteChoiceOpen)CloseChoices();else if(RecordOpen)CloseRecord();else if(MapVisible)ToggleMap();else TogglePause();}
            if(Input.GetKeyDown(KeyCode.Tab)&&!RecordOpen&&!Won)ToggleMap();
            if(Input.GetKeyDown(KeyCode.J)&&!Won)ToggleArchive();
            if(RecordOpen&&Input.GetKeyDown(KeyCode.LeftArrow))BrowseRecord(-1);
            if(RecordOpen&&Input.GetKeyDown(KeyCode.RightArrow))BrowseRecord(1);
            if(TutorialStage>=4&&!Frozen){
                if(Travel==null&&RoomEntered)FollowOpenPassages();
                if(Travel!=null&&!Bridging){
                    if(Travel.Spec.kind==PassageKind.Stair)FollowStairPosition();
                    else if(Travel.Spec.kind!=PassageKind.Bridge&&Waypoint<travelPoints.Length-1&&Vector3.Distance(player.transform.position,TravelTarget)<.75f)Waypoint++;
                    if(Travel.Spec.kind==PassageKind.Lift&&Mathf.Abs(player.transform.position.y-StationPlan.Height(TravelTo))<.5f&&Waypoint<4)Waypoint=4;
                    if(AtDestination())FinishTravel();
                }else if(Travel==null&&!RoomEntered){var p=player.transform.position;float a=Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg;if(Mathf.Abs(p.y-StationPlan.Height(ActiveSector))<4&&new Vector2(p.x,p.z).magnitude>29&&new Vector2(p.x,p.z).magnitude<56&&Mathf.Abs(Mathf.DeltaAngle(a,StationPlan.Angle(ActiveSector)))<StationPlan.Width(ActiveSector)/2)EnterRoom(CurrentRoom);}
            }
            previousPlayerPosition=player.transform.position;
            if(Step==StationPlan.Count&&bossGate.IsOpen&&!Won&&!boss.Active&&Vector3.Distance(player.transform.position,boss.transform.position)<21)
            {player.SetCheckpoint(StationPlan.Point(20,72,0,.15f));boss.Begin();}
        }
        void OnDestroy() {Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
}

