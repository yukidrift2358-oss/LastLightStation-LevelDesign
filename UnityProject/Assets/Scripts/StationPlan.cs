using UnityEngine;

namespace LastLight
{
    [System.Flags] public enum RoomNeed { None=0, Power=1, Seal=2, Pulse=4, Combat=8, Record=16, Relay=32 }
    public static class StationPlan
    {
        public const int Count=26;
        public static readonly int[] Counts={6,12,8},Offsets={0,6,18};
        public static readonly float[] Rotations={0,180,90};
        public static int[] Route {get{return StationTopology.RouteA;}}
        public static readonly string[] FloorNames={"01 生活环 · 基础验证","02 工业环 · 强化验证","03 隔离环 · 最终考验"};
        public static readonly string[] Names={"归航登记","最后配给","防卫试验间","迁徙观景台","气密档案","机械体检修", "净水接力","休眠宿舍","监视廊","转子校准","配电总线","空箱货场","旧日影院","无声温室","应急医疗","星图升降台","回声天线","装卸交火", "隔离前室","磁轨回廊","最后交接","被封存的姓名","三路稳压","撤离演算","断链守卫","种子冷库"};
        public static readonly RoomNeed[] Needs={
            RoomNeed.Power|RoomNeed.Record,RoomNeed.Seal|RoomNeed.Record,RoomNeed.Combat,RoomNeed.Record,RoomNeed.Pulse|RoomNeed.Record,RoomNeed.Record,
            RoomNeed.Relay|RoomNeed.Seal,RoomNeed.Record,RoomNeed.Power|RoomNeed.Combat,RoomNeed.Relay|RoomNeed.Pulse,RoomNeed.Record,RoomNeed.Combat|RoomNeed.Power,
            RoomNeed.Record,RoomNeed.Seal|RoomNeed.Power|RoomNeed.Record,RoomNeed.Seal|RoomNeed.Pulse|RoomNeed.Record,RoomNeed.Record,RoomNeed.Relay|RoomNeed.Pulse,RoomNeed.Combat|RoomNeed.Pulse,
            RoomNeed.Power|RoomNeed.Seal|RoomNeed.Pulse,RoomNeed.Combat|RoomNeed.Pulse,RoomNeed.Record,RoomNeed.Seal|RoomNeed.Pulse|RoomNeed.Record,RoomNeed.Relay|RoomNeed.Power|RoomNeed.Seal,
            RoomNeed.Record,RoomNeed.Combat|RoomNeed.Power|RoomNeed.Pulse,RoomNeed.Record};
        public static readonly string[] Records={"未签退的值班表","最后一张配给牌","旧靶场","迁徙轨迹","全员隔离授权","检修日志","滤芯记录","生日录音","安全指令","转子日记","应急电网","空箱货单","电影散场","密封种子匣","不再增长的名单","窗边的家园坐标","信号回声","最后一批物资","隔离计时","磁轨维护","最后一次交接","二十六个名字","配电遗嘱","撤离演算结果","禁止离站命令","下一季"};
        public static readonly string[] Stories=StationArchive.Bodies;
        public static int Floor(int id){return id<6?0:id<18?1:2;}
        public static int Local(int id){return id-Offsets[Floor(id)];}
        public static float Height(int id){return Floor(id)*24;}
        public static float Angle(int id){return Rotations[Floor(id)]+Local(id)*360f/Counts[Floor(id)];}
        public static float Width(int id){return 360f/Counts[Floor(id)];}
        public static string Address(int id){return (Floor(id)+1)+"层 / "+(Local(id)+1).ToString("00")+"区";}
        public static bool Rest(int id){return id==5||id==3||id==7||id==10||id==12||id==15||id==23||id==25||id==20;}
        public static bool Timed(int id){return id==14||id==16;}
        public static bool Sync(int id){return id==18;}
        public static string Novelty(int id){string[] s={"先读档，再把电力交给通路","气密阀与最后一份配给","观察冲锋预告，侧移反击","安全休息 · 回望放逐轨迹","首次把抑制用于档案读取","安全休息 · 静默的检修机械体","按 2 → 1 → 3 接回净水系统","安全休息 · 阅读生日录音稿","掩体阻断蓄能射线","抑制后登上高台完成转子接力","安全休息 · 阅读工程师留下的电网遗嘱","登上步道，先处理会修复同伴的维修机","安全休息 · 无人影院","在枯萎温室重新分配电力","短时抑制后及时登记出口","安全休息 · 观景升降准备","继电器与短时出口窗口","维修机与近战机械体交叉施压","先密封、再接 B 电、最后抑制","看磁场预告，利用柱体挡住牵引","Boss 前安全休息 · 最后交接","隔离与抑制后取回被封存的姓名","先隔离，再按顺序接通三条支路","安全休息 · 从观景步道阅读撤离演算","断电关闭射线，优先拆除修复链","从高架步道取回最后的种子"};return s[id];}
        public static Vector3 Point(int sector, float radius, float tangent=0, float height=0)
        {
            float a=Angle(sector)*Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a)*radius+Mathf.Cos(a)*tangent,Height(sector)+height,Mathf.Cos(a)*radius-Mathf.Sin(a)*tangent);
        }
        public static bool Adjacent(int a,int b) { if(Floor(a)!=Floor(b))return false;int d=Mathf.Abs(Local(a)-Local(b)); return d==1||d==Counts[Floor(a)]-1; }
        public static bool Opposite(int a,int b) { return Floor(a)==Floor(b)&&Mathf.Abs(Local(a)-Local(b))==Counts[Floor(a)]/2; }
    }
}

