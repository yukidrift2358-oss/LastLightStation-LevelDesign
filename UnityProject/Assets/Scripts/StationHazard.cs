using UnityEngine;

namespace LastLight
{
    public sealed class StationHazard : MonoBehaviour
    {
        public StationDirector director;
        public StationRoom room;
        public bool tutorial;
        public bool suppressible=true;
        public bool Powered=true;
        public Renderer visual;
        public float radius=3.4f;
        float suppressedUntil;
        float nextDamage;
        public bool SuppressedNow {get{return Time.time<suppressedUntil;}}
        public float SuppressedRemaining {get{return Mathf.Max(0,suppressedUntil-Time.time);}}
        public bool Live {get {return Powered&&Time.time>=suppressedUntil;}}
        public void Suppress(float duration=4)
        {
            if(!suppressible||!Powered) return;
            suppressedUntil=Time.time+duration;
            if(room!=null) room.Suppressed();
            if(tutorial) director.CompleteTutorial(2);
        }
        void Update()
        {
            if(visual!=null) visual.enabled=Live;
            if(!Live||director.player==null||Time.time<nextDamage) return;
            if(room!=null&&!director.IsCurrentRoom(room)) return;
            if(Vector3.Distance(transform.position,director.player.transform.position+Vector3.up)<radius)
            { director.player.TakeDamage(suppressible?9:5); nextDamage=Time.time+1; }
        }
    }
}
