using UnityEngine;
namespace LastLight
{
    // Imported Quaternius skeleton and clips; movement and damage stay in StationPlayer.
    public sealed class StationHumanoid:MonoBehaviour
    {
        public StationPlayer player;
        public Animator animator;
        public Transform torso,leftArm,rightArm,leftElbow,rightElbow,leftThigh,rightThigh,leftKnee,rightKnee,sword;
        public TrailRenderer slashTrail;
        public const float AttackDuration=.55f,ContactTime=.16f;
        public bool IsAttacking {get{return Time.time<attackStart+AttackDuration;}}
        public Vector3 BladeTip {get{return sword.TransformPoint(new Vector3(0,0,1.15f));}}
        float attackStart=-10,speed;
        bool grounded=true,dashing;
        string currentState;
        public string AnimationState {get{return currentState;}}
        public void SetMotion(float value,bool onGround,bool dash){speed=value;grounded=onGround;dashing=dash;}
        public void PlayAttack(){attackStart=Time.time;slashTrail.Clear();animator.CrossFadeInFixedTime("Attack",.025f,0,0);currentState="Attack";}
        public void CancelAttack(){attackStart=-10;slashTrail.emitting=false;slashTrail.Clear();if(animator!=null&&animator.isInitialized)ChangeState("Idle",.08f);}
        void Start(){animator.Play("Idle",0,0);currentState="Idle";}
        void Update()
        {
            if(player.director.Frozen||IsAttacking)return;
            ChangeState(dashing?"Dash":!grounded?"Air":player.Charging?"Charge":speed>3.8f?"Jog":speed>.1f?"Walk":"Idle",.1f);
        }
        void ChangeState(string state,float blend)
        {
            if(state==currentState)return;
            animator.CrossFadeInFixedTime(state,blend,0,0);currentState=state;
        }
        void LateUpdate()
        {
            float t=(Time.time-attackStart)/AttackDuration;
            slashTrail.emitting=IsAttacking&&t>.23f&&t<.7f;
        }
        void OnDisable(){if(slashTrail!=null){slashTrail.emitting=false;slashTrail.Clear();}}
    }
}
