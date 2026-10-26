using UnityEngine;
using System.Collections;
namespace LastLight
{
    public enum EnemyKind {Charger,Sentry,Repair,Tractor}
    public sealed class StationEnemy:MonoBehaviour
    {
        public StationDirector director;
        public StationRoom room;
        public bool tutorial,turret;
        public EnemyKind kind;
        public Renderer body;
        public Transform warning;
        public LineRenderer beam;
        public int ShotsFired {get;private set;}
        public int maxHealth=3;
        int health;
        float suppressedUntil,nextAttack,windupUntil,chargeUntil,nextDefense;
        Vector3 origin,lockedTarget,chargeDirection;
        Quaternion originRotation;
        public bool Dead {get{return health<=0;}}
        public bool Suppressed {get{return Time.time<suppressedUntil;}}
        public bool Telegraphing {get{return windupUntil>Time.time;}}
        public int Health {get{return health;}}
        void Awake(){origin=transform.position;originRotation=transform.rotation;health=maxHealth;nextAttack=Time.time+2;nextDefense=Time.time+2;}
        public void ResetEnemy(){health=maxHealth;transform.SetPositionAndRotation(origin,originRotation);gameObject.SetActive(true);suppressedUntil=0;windupUntil=chargeUntil=0;nextAttack=Time.time+2;nextDefense=Time.time+2;ShotsFired=0;if(beam!=null)beam.enabled=false;if(warning!=null)warning.gameObject.SetActive(false);}
        public void Suppress(float duration=4){suppressedUntil=Time.time+duration;windupUntil=chargeUntil=0;if(warning!=null)warning.gameObject.SetActive(false);if(room!=null)room.Suppressed();}
        public void Hit(){if(Dead)return;health--;if(Dead){if(warning!=null)warning.gameObject.SetActive(false);gameObject.SetActive(false);if(tutorial)director.CompleteTutorial(3);}}
        public bool RepairOne(){if(Dead||health>=maxHealth)return false;health++;return true;}
        bool ClearLine(Vector3 target){return !Physics.Linecast(transform.position+Vector3.up,target+Vector3.up,1,QueryTriggerInteraction.Ignore);}
        void Move(Vector3 direction,float speed)
        {
            if(room!=null&&origin.y-room.transform.position.y>2.5f)return;
            var proposed=transform.position+direction*speed*Time.deltaTime;
            var local=room!=null?room.transform.InverseTransformPoint(proposed):Vector3.zero;
            bool inside=room==null||(Mathf.Abs(local.x)<9&&Mathf.Abs(local.z)<11);
            if(inside&&!Physics.SphereCast(transform.position+Vector3.up,.5f,direction,out _,.8f,1,QueryTriggerInteraction.Ignore))transform.position=proposed;
        }
        void ShowWarning(Vector3 target)
        {
            if(warning==null)return;warning.gameObject.SetActive(true);
            Vector3 d=target-transform.position;d.y=0;
            float length=kind==EnemyKind.Charger?7:kind==EnemyKind.Tractor?13:18;
            warning.position=transform.position+d.normalized*length*.5f+Vector3.up*.07f;
            warning.rotation=Quaternion.LookRotation(d.sqrMagnitude>.01f?d:transform.forward);
            warning.localScale=new Vector3(kind==EnemyKind.Tractor?3:1.6f,.07f,length);
        }
        void ShowBeam(Vector3 point,bool healing)
        {if(beam!=null)StartCoroutine(BeamFlash(point,healing));}
        IEnumerator BeamFlash(Vector3 point,bool healing)
        {
            beam.enabled=true;beam.startColor=beam.endColor=healing?new Color(.1f,1,.9f):new Color(1,.25f,.05f);
            beam.SetPosition(0,transform.position+Vector3.up);beam.SetPosition(1,point);
            yield return new WaitForSeconds(.22f);beam.enabled=false;
        }
        void OnDisable(){if(beam!=null)beam.enabled=false;}
        void Update()
        {
            bool active=tutorial?director.TutorialStage==3:director.IsCurrentRoom(room);
            if(!active||Dead){windupUntil=chargeUntil=0;if(warning!=null)warning.gameObject.SetActive(false);return;}if(director.Frozen)return;
            if(Suppressed||(kind==EnemyKind.Sentry&&room!=null&&!room.PowerB)){windupUntil=0;if(warning!=null)warning.gameObject.SetActive(false);return;}
            var target=director.player.transform.position;var delta=target-transform.position;delta.y=0;float distance=delta.magnitude;
            if(distance>24||Mathf.Abs(target.y-transform.position.y)>5)return;
            if(Time.time<chargeUntil){Move(chargeDirection,9);if(distance<2)director.player.TakeDamage(14);return;}
            if(windupUntil>0)
            {
                if(Time.time<windupUntil)return;
                windupUntil=0;if(warning!=null)warning.gameObject.SetActive(false);
                Vector3 aim=lockedTarget-transform.position;aim.y=0;
                if(kind==EnemyKind.Charger){chargeDirection=aim.normalized;chargeUntil=Time.time+.65f;}
                else if(ClearLine(target)&&Vector3.Angle(aim,delta)<(kind==EnemyKind.Tractor?16:8))
                {if(kind==EnemyKind.Tractor){director.player.Carry(-delta.normalized*2.2f);director.player.TakeDamage(8);}else {ShowBeam(target+Vector3.up,false);ShotsFired++;director.player.TakeDamage(kind==EnemyKind.Repair?6:12);}}
                return;
            }
            if(kind==EnemyKind.Repair)
            {
                if(Time.time>=nextAttack)
                {
                    nextAttack=Time.time+.3f;
                    foreach(var ally in room.enemies)if(ally!=null&&ally!=this&&!ally.Dead&&Vector3.Distance(ally.transform.position,transform.position)<15&&ally.RepairOne())
                    {ShowBeam(ally.transform.position+Vector3.up,true);nextAttack=Time.time+4;return;}
                }
                if(Time.time>=nextDefense&&distance<16&&ClearLine(target))
                {lockedTarget=target;windupUntil=Time.time+1.4f;nextDefense=Time.time+6;ShowWarning(target);}
                else if(distance<5&&distance>.1f)Move(-delta.normalized,1.7f);
                return;
            }
            if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(delta);
            if(kind==EnemyKind.Charger&&distance>5)Move(delta.normalized,2.1f);
            if(Time.time>=nextAttack&&distance<(kind==EnemyKind.Charger?7:19)&&ClearLine(target))
            {lockedTarget=target;windupUntil=Time.time+(kind==EnemyKind.Charger?1:1.4f);nextAttack=Time.time+(kind==EnemyKind.Tractor?5.5f:3.6f);ShowWarning(target);}
        }
    }
}
