using System.Collections.Generic;
using UnityEngine;

namespace LastLight
{
    public sealed class StationRoom : MonoBehaviour
    {
        public int sector;
        public StationDirector director;
        public Renderer powerIndicator;
        public Transform movingPlatform;
        public Transform mechanicalArm;
        public StationHazard pressure;
        public StationHazard scanner;
        public StationEnemy[] enemies;
        public Vector3 entryPoint;
        public int RelayProgress {get;private set;}
        public int[] RelayOrder {get{return sector%3==0?new[]{2,1,3}:sector%3==1?new[]{3,1,2}:new[]{1,3,2};}}
        public string RelayHint {get{return string.Join(" → ",RelayOrder);}}
        public int NextRelay {get{return RelayOrder[Mathf.Min(RelayProgress,2)];}}
        public void Relay(int index){if((Need&RoomNeed.Seal)!=0&&!Sealed){director.Notify("先关闭泄漏阀，再接通继电器。");return;}if(RelayProgress>=3)return;if(index==NextRelay)RelayProgress++;else RelayProgress=0;director.Notify(RelayProgress==3?"三路接力完成。":"继电器 "+RelayProgress+" / 3；顺序 "+RelayHint+"。");}
        public bool PowerB {get;private set;}
        public bool Sealed {get;private set;}
        public bool PulseSolved {get;private set;}
        public bool HasRecord {get;private set;}
        public bool Complete {get;private set;}
        public RoomNeed Need {get {return StationPlan.Needs[sector];}}
        public bool Cleared {get {foreach(var e in enemies) if(e!=null&&!e.Dead) return false; return true;}}
        Vector3 platformOrigin;
        void Start() { if(movingPlatform!=null) platformOrigin=movingPlatform.localPosition; }
        public void TogglePower()
        {
            PowerB=!PowerB;
            powerIndicator.material.color=PowerB?new Color(.15f,.8f,1):new Color(1,.55f,.15f);
            if(scanner!=null) scanner.Powered=PowerB;
            director.Notify(PowerB?"B 支路：通路与机械装置通电。档案终端暂停。":"A 支路：档案与照明通电。先读取记录，再切换通路。");
        }
        public void Seal()
        {
            Sealed=true;
            if(pressure!=null) pressure.Powered=false;
            director.Notify("泄漏侧已隔离。舱压稳定，隔离区现在可以安全通过。");
        }
        public void Suppressed() { if(StationPlan.Sync(sector)&&(!PowerB||!Sealed)){director.Notify("同步失败：先密封，再接入 B 电，最后抑制。");return;} PulseSolved=true; }
        public void ReadRecord()
        {
            if((Need&RoomNeed.Power)!=0&&PowerB) { director.Notify("档案终端无电。请先切回 A 支路。"); return; }
            if((Need&RoomNeed.Seal)!=0&&!Sealed) { director.Notify("读取接口位于低压区。请先关闭泄漏侧隔离阀。"); return; }
            if((Need&RoomNeed.Pulse)!=0&&!PulseSolved) { director.Notify("扫描装置干扰接口。靠近红色装置，按 Q 短时抑制。"); return; }
            HasRecord=true;
            director.ShowRecord(StationPlan.Records[sector],StationPlan.Stories[sector]);
        }
        public string Missing()
        {
            var list=new List<string>();
            if((Need&RoomNeed.Relay)!=0&&RelayProgress<3)list.Add("继电器 "+RelayHint+"（已完成 "+RelayProgress+"）");
            if((StationPlan.Timed(sector)||StationPlan.Sync(sector))&&(!PulseSolved||scanner==null||!scanner.SuppressedNow))list.Add("出口需在当前抑制窗口内登记（短按4秒 / 蓄满7秒）");
            if((Need&RoomNeed.Seal)!=0&&!Sealed) list.Add("关闭泄漏侧隔离阀");
            if((Need&RoomNeed.Pulse)!=0&&!PulseSolved) list.Add("靠近扫描装置使用 Q 抑制");
            if((Need&RoomNeed.Record)!=0&&!HasRecord) list.Add("在 A 支路读取任务记录");
            if((Need&RoomNeed.Combat)!=0&&!Cleared) list.Add("解除活动机械威胁");
            if((Need&RoomNeed.Power)!=0&&!PowerB) list.Add("切换 B 支路为出口供电");
            return string.Join(" · ",list);
        }
        public void MarkComplete() {Complete=true;}
        public void ResetThreats() { foreach(var e in enemies) if(e!=null) e.ResetEnemy(); }
        void OnTriggerEnter(Collider other) { var p=other.GetComponent<StationPlayer>(); if(p!=null) director.EnterRoom(this); }
        void Update()
        {
            if(movingPlatform!=null)
                movingPlatform.localPosition=Vector3.MoveTowards(movingPlatform.localPosition,platformOrigin+Vector3.up*(PowerB?2.8f:0),Time.deltaTime*1.4f);
            if(mechanicalArm!=null&&scanner!=null&&scanner.Live)
                mechanicalArm.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*1.2f)*60,0);
        }
    }
}
