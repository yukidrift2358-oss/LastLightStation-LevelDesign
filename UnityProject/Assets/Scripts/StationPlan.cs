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
        public static string Novelty(int id){string[] s={"读取值班表，再切到 B 支路开启通路","关闭泄漏阀，再读取配给记录","观察冲锋预警，侧移后反击","安全区：读取航迹，确认下一段通路","抑制环形扫描装置，再读取隔离授权","安全区：查看检修日志，恢复状态","关闭泄漏，再按 2 → 1 → 3 接通继电器","安全区：查看生日留言，选择支线顺序","利用掩体阻断射线，解除威胁后切到 B 支路","抑制扫描后，按标牌顺序操作高位继电器","安全区：读取电网记录，恢复状态","维修机在高处支援同伴，可沿坡道接近","安全区：读取停映通知，恢复状态","关闭泄漏，在 A 支路读档，再切到 B","密封并读档，在抑制结束前操作出口","安全区：查看坐标，经升降台进入隔离环","完成继电器接力，再抑制扫描并赶到出口","注意高位维修支援，结合抑制处理近战威胁","关闭泄漏 → 切到 B 支路 → 抑制 → 操作出口","观察牵引预警，借柱体阻断拉拽","安全区：读取交接单，准备进入控制舱","关闭泄漏并抑制扫描，再读取封存名单","关闭泄漏，按标牌接力，再切到 B 支路","安全区：沿观景步道读取撤离演算","处理维修支援与射线，完成抑制及出口供电","安全区：沿高架步道读取种子资料"};return s[id];}
        public static Vector3 Point(int sector, float radius, float tangent=0, float height=0)
        {
            float a=Angle(sector)*Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a)*radius+Mathf.Cos(a)*tangent,Height(sector)+height,Mathf.Cos(a)*radius-Mathf.Sin(a)*tangent);
        }
        public static bool Adjacent(int a,int b) { if(Floor(a)!=Floor(b))return false;int d=Mathf.Abs(Local(a)-Local(b)); return d==1||d==Counts[Floor(a)]-1; }
        public static bool Opposite(int a,int b) { return Floor(a)==Floor(b)&&Mathf.Abs(Local(a)-Local(b))==Counts[Floor(a)]/2; }
    }
}

