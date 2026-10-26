using UnityEngine;
namespace LastLight
{
    public sealed class StationMachineVisual:MonoBehaviour
    {
        public StationEnemy enemy;
        public TextMesh statusLabel;
        string previousStatus;
        Vector3 home;
        Animator animator;
        void Start(){home=transform.localPosition;animator=GetComponent<Animator>();}
        void Update()
        {
            if(enemy.director.Frozen)return;
            string status=enemy.Suppressed?"SUPPRESSED":enemy.kind==EnemyKind.Sentry?(enemy.room.PowerB?"SENTRY / ARMED":"SENTRY / OFFLINE A"):enemy.kind==EnemyKind.Repair?"REPAIR / SUPPORT + DEFENCE":enemy.kind.ToString().ToUpper();
            if(statusLabel!=null&&status!=previousStatus){statusLabel.text=status;statusLabel.font.RequestCharactersInTexture(status,statusLabel.fontSize);previousStatus=status;}
            if(animator!=null)animator.speed=enemy.Suppressed?0:1;
            if(enemy.kind==EnemyKind.Repair||enemy.kind==EnemyKind.Sentry)
                transform.localPosition=home+Vector3.up*(enemy.Suppressed?-.3f:Mathf.Sin(Time.time*2+enemy.GetInstanceID())*.09f);
        }
    }
}
