using UnityEngine;
using System.Collections;

namespace LastLight
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class StationPlayer : MonoBehaviour
    {
        public StationDirector director;
        public Camera view;
        public Transform visual;
        public int Health {get;private set;}=100;
        public float PulseReady {get {return Mathf.Max(0,nextPulse-Time.time);}}
        public float DashReady {get {return Mathf.Max(0,nextDash-Time.time);}}
        public StationInteractable Focus {get;private set;}
        public Vector3 Checkpoint {get;private set;}
        public const float ChargeSeconds=1.2f;
        public bool Charging {get;private set;}
        public float ChargeRatio {get {return Charging?Mathf.Clamp01((Time.time-chargeStarted)/ChargeSeconds):0;}}
        public bool ChargedReady {get {return Charging&&ChargeRatio>=1;}}
        float chargeStarted;
        StationHumanoid avatar;
        int attackGeneration;
        CharacterController motor;
        float yaw=180,pitch=24,vertical,nextPulse,nextAttack,nextDash,dashingUntil,invincibleUntil;
        Vector3 dashDirection;
        Collider[] nearby=new Collider[64];
        void Awake() {motor=GetComponent<CharacterController>(); Checkpoint=transform.position;}
        void Start() {avatar=visual.GetComponent<StationHumanoid>();Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;}
        public void Restore(){CancelMelee();CancelPulseCharge();Health=100;nextPulse=0;nextDash=0;}
        public void Carry(Vector3 delta){motor.Move(delta);vertical=-2;}
        public void SetCheckpoint(Vector3 p) {Checkpoint=p;}
        public void Warp(Vector3 p) {motor.enabled=false;transform.position=p;motor.enabled=true;vertical=0;}
        public void Respawn()
        {
            CancelMelee();CancelPulseCharge();Health=100; invincibleUntil=Time.time+2; nextPulse=0; nextDash=0;
            Warp(Checkpoint); director.OnRespawn();
        }
        public void TakeDamage(int damage)
        {
            if(director.Frozen||Time.time<invincibleUntil) return;
            Health=Mathf.Max(0,Health-damage); invincibleUntil=Time.time+.6f;
            if(Health<=0) Respawn();
        }
        void Update()
        {
            if(director.Frozen||director.Bridging) {CancelPulseCharge();if(avatar!=null)avatar.SetMotion(0,true,false);return;}
            yaw+=Input.GetAxisRaw("Mouse X")*2.4f;
            pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*1.7f,8,65);
            Vector3 input=new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"));
            input=Vector3.ClampMagnitude(input,1);
            var move=Quaternion.Euler(0,yaw,0)*input;
            if(move.sqrMagnitude>.01f) visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.LookRotation(move),Time.deltaTime*14);
            if(Input.GetKeyDown(KeyCode.LeftShift)&&Time.time>=nextDash)
            {dashDirection=move.sqrMagnitude>.01f?move:visual.forward;dashingUntil=Time.time+.2f;invincibleUntil=Time.time+.3f;nextDash=Time.time+1.5f;}
            if(motor.isGrounded&&vertical<0) vertical=-2;
            if(motor.isGrounded&&Input.GetKeyDown(KeyCode.Space)) vertical=7;
            vertical-=22*Time.deltaTime;
            motor.Move(((Time.time<dashingUntil?dashDirection*16:move*(Charging?3.2f:5.4f))+Vector3.up*vertical)*Time.deltaTime);
            if(avatar!=null)avatar.SetMotion(move.magnitude*(Charging?3.2f:5.4f),motor.isGrounded,Time.time<dashingUntil);
            float fallLimit=director.TutorialStage>=4?director.FallLimit:-8;
            if(transform.position.y<fallLimit) Respawn();
            FindFocus();
            if(Input.GetKeyDown(KeyCode.E)&&Focus!=null) Focus.Use();
            if(Input.GetKeyDown(KeyCode.Q)) BeginPulseCharge();
            if(Input.GetKeyUp(KeyCode.Q)) ReleasePulseCharge();
            if(Input.GetMouseButtonDown(0)&&Time.time>=nextAttack) Attack();
        }
        void FindFocus()
        {
            Focus=null;float best=3.5f;
            int count=Physics.OverlapSphereNonAlloc(transform.position+Vector3.up,3.8f,nearby,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)
            {
                var item=nearby[i].GetComponent<StationInteractable>();
                if(item==null)continue;
                float d=Vector3.Distance(transform.position+Vector3.up,item.transform.position);
                if(d<best) {best=d;Focus=item;}
            }
        }
        public bool BeginPulseCharge()
        {
            if(director.Frozen||Time.time<nextPulse||Charging)return false;
            Charging=true;chargeStarted=Time.time;return true;
        }
        public void CancelPulseCharge(){Charging=false;}
        public void ReleasePulseCharge()
        {
            if(!Charging)return;
            bool charged=ChargedReady;Charging=false;
            if(!director.Frozen)Pulse(charged);
        }
        void OnDisable(){CancelPulseCharge();CancelMelee();}
        void CancelMelee(){attackGeneration++;if(avatar!=null)avatar.CancelAttack();}
        public void Pulse(bool charged=false)
        {
            if(director.Frozen||Time.time<nextPulse)return;
            Charging=false;
            nextPulse=Time.time+(charged?12:8);
            float radius=charged?16:9,duration=charged?7:4;
            int count=QueryPulseTargets(radius);
            for(int i=0;i<count;i++)
            {
                var hazard=nearby[i].GetComponent<StationHazard>(); if(hazard!=null)hazard.Suppress(duration);
                var enemy=nearby[i].GetComponent<StationEnemy>(); if(enemy!=null)enemy.Suppress(duration);
            }
            if(director.boss!=null&&Vector3.Distance(transform.position,director.boss.transform.position)<14) director.boss.Suppress();
            director.PulseVisual(transform.position,charged);
            var feedback=GetComponent<StationPulseFeedback>();if(feedback!=null)feedback.Release(charged);
        }
        int QueryPulseTargets(float radius)
        {
            // Scenery colliders also enter this query. A full buffer may omit the actual device.
            int count=Physics.OverlapSphereNonAlloc(transform.position,radius,nearby,~0,QueryTriggerInteraction.Collide);
            while(count==nearby.Length)
            {
                if(nearby.Length>=4096){nearby=Physics.OverlapSphere(transform.position,radius,~0,QueryTriggerInteraction.Collide);return nearby.Length;}
                System.Array.Resize(ref nearby,nearby.Length*2);
                count=Physics.OverlapSphereNonAlloc(transform.position,radius,nearby,~0,QueryTriggerInteraction.Collide);
            }
            return count;
        }
        public void Attack()
        {
            if(director.Frozen||Time.time<nextAttack)return;
            nextAttack=Time.time+StationHumanoid.AttackDuration;
            if(avatar!=null)avatar.PlayAttack();
            StartCoroutine(ResolveSlash(attackGeneration));
        }
        IEnumerator ResolveSlash(int generation)
        {
            yield return new WaitForSeconds(StationHumanoid.ContactTime);
            if(generation!=attackGeneration)yield break;
            int count=Physics.OverlapSphereNonAlloc(transform.position,4.8f,nearby,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)
            {
                if(!CanStrike(nearby[i].transform.position))continue;
                var enemy=nearby[i].GetComponent<StationEnemy>(); if(enemy!=null)enemy.Hit();
                var boss=nearby[i].GetComponent<StationBoss>(); if(boss!=null)boss.Hit();
            }
            director.AttackVisual(transform.position+visual.forward*2+Vector3.up);
        }
        bool CanStrike(Vector3 point)
        {
            Vector3 d=point-transform.position;d.y=0;
            return Vector3.Angle(visual.forward,d)<100&&!Physics.Linecast(transform.position+Vector3.up,point+Vector3.up,1,QueryTriggerInteraction.Ignore);
        }
        void LateUpdate()
        {
            var pivot=transform.position+Vector3.up*1.65f;
            var rotation=Quaternion.Euler(pitch,yaw,0);
            Vector3 direction=rotation*Vector3.back;
            float distance=6;
            if(Physics.SphereCast(pivot,.3f,direction,out var hit,6,1,QueryTriggerInteraction.Ignore))distance=Mathf.Max(.6f,hit.distance-.15f);
            view.transform.position=pivot+direction*distance;
            view.transform.rotation=rotation;
        }
    }
}
