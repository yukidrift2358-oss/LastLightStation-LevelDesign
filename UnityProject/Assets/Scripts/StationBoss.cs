using UnityEngine;

namespace LastLight
{
    public sealed class StationBoss : MonoBehaviour
    {
        public StationDirector director;
        public Renderer body;
        public Transform warning;
        public int Health {get;private set;}=18;
        public bool Active {get;private set;}
        public bool LeakSealed {get;private set;}
        public bool PowerB {get;private set;}
        public bool Shielded {get {return Health<=9&&Time.time>exposedUntil;}}
        public bool Telegraphing {get;private set;}
        float exposedUntil,nextAttack,resolveAt;
        int attackIndex;
        Vector3 attackDirection;
        public void Begin()
        {
            if(Active||director.Won)return;
            Active=true;nextAttack=Time.time+3;
            director.Notify("守门者：末班协议仍在执行。观察地面预警，用 Shift 闪避；靠近后点击左键攻击。");
        }
        public void ResetBoss()
        {
            Health=18;Active=false;LeakSealed=false;PowerB=false;exposedUntil=0;Telegraphing=false;
            warning.gameObject.SetActive(false);
        }
        public void SwitchPower()
        { PowerB=!PowerB; director.Notify(PowerB?"抑制器已通电。关闭泄漏后，靠近守门者使用 Q 解除护盾。":"抑制器已断电。再次交互可接入 B 支路。"); }
        public void SealLeak() {LeakSealed=true;director.Notify("Boss 舱泄漏侧已关闭。环境稳定，抑制器可以安全运行。");}
        public void Suppress()
        {
            if(!Active||Health>9||!PowerB||!LeakSealed)return;
            exposedUntil=Time.time+8;
            director.Notify("隔离护盾中断：8 秒攻击窗口。");
        }
        public void Hit()
        {
            if(!Active||Health<=0)return;
            if(Shielded) {director.Notify("护盾阻挡攻击。先关闭外围泄漏阀，为抑制器供电，再靠近使用 Q。");return;}
            Health--;
            if(Health==9)director.Notify("末班协议启动护盾。复用隔离、分流、抑制三种机制解除它。");
            if(Health<=0)
            {
                Active=false;warning.gameObject.SetActive(false);
                body.material.color=new Color(.13f,.2f,.25f);
                transform.rotation=Quaternion.Euler(0,0,18);
                director.Win();
            }
        }
        void Update()
        {
            if(!Active||director.Frozen)return;
            body.material.color=Shielded?new Color(.2f,.65f,.85f):new Color(.8f,.28f,.12f);
            if(!Telegraphing&&Time.time>=nextAttack)
            {
                Telegraphing=true;resolveAt=Time.time+1.4f;attackIndex++;
                attackDirection=director.player.transform.position-transform.position;attackDirection.y=0;attackDirection.Normalize();
                bool line=attackIndex%2==0;
                warning.gameObject.SetActive(true);
                warning.position=transform.position+(line?attackDirection*10:Vector3.zero)+Vector3.up*.07f;
                warning.rotation=line?Quaternion.LookRotation(attackDirection):Quaternion.identity;
                warning.localScale=line?new Vector3(5,.06f,24):new Vector3(22,.06f,22);
            }
            if(Telegraphing&&Time.time>=resolveAt)
            {
                Vector3 delta=director.player.transform.position-transform.position;delta.y=0;
                bool hit=attackIndex%2==0?Vector3.Dot(delta,attackDirection)>-2&&Vector3.Dot(delta,attackDirection)<22&&Mathf.Abs(Vector3.Dot(delta,Vector3.Cross(Vector3.up,attackDirection)))<2.5f:delta.magnitude<11;
                if(hit)director.player.TakeDamage(24);
                warning.gameObject.SetActive(false);Telegraphing=false;nextAttack=Time.time+(Health<=9?1.8f:2.7f);
            }
        }
    }
}
