using UnityEngine;

namespace LastLight
{
    public sealed class StationPulseFeedback : MonoBehaviour
    {
        public Material lineMaterial;
        StationPlayer player;
        LineRenderer ring,aura;
        AudioSource audioSource;
        AudioClip quick,heavy,ready;
        bool readyPlayed;
        void Awake()
        {
            player=GetComponent<StationPlayer>();
            ring=new GameObject("Q_RangePreview").AddComponent<LineRenderer>();
            ring.transform.SetParent(transform,false);ring.sharedMaterial=lineMaterial;
            ring.loop=true;ring.positionCount=96;ring.startWidth=ring.endWidth=.08f;
            ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            aura=new GameObject("Q_ChargeAura").AddComponent<LineRenderer>();aura.transform.SetParent(transform,false);aura.sharedMaterial=lineMaterial;
            aura.loop=true;aura.positionCount=48;aura.startWidth=aura.endWidth=.09f;aura.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;audioSource.volume=.3f;
            quick=Tone("QuickPulse",520,.2f);heavy=Tone("ChargedPulse",140,.55f);ready=Tone("ChargeReady",880,.14f);
        }
        static AudioClip Tone(string name,float frequency,float duration)
        {
            int count=(int)(22050*duration);var data=new float[count];
            for(int i=0;i<count;i++){float t=i/22050f;data[i]=Mathf.Sin(t*frequency*6.28318f)*(1-t/duration)*Mathf.Min(1,t*90)*.55f;}
            var clip=AudioClip.Create(name,count,1,22050,false);clip.SetData(data,0);return clip;
        }
        void Update()
        {
            ring.enabled=player.Charging&&!player.director.Frozen;aura.enabled=ring.enabled;
            if(!ring.enabled){readyPlayed=false;return;}
            float radius=player.ChargedReady?16:9;
            var color=player.ChargedReady?new Color(1,.65f,.12f):new Color(.1f,.85f,1);
            ring.startColor=ring.endColor=color;aura.startColor=aura.endColor=color;
            float inner=.7f+player.ChargeRatio*.25f+(player.ChargedReady?Mathf.Sin(Time.time*10)*.05f:0);
            for(int i=0;i<48;i++){float a=i*2*Mathf.PI/48;aura.SetPosition(i,transform.position+new Vector3(Mathf.Sin(a)*inner,.13f,Mathf.Cos(a)*inner));}
            for(int i=0;i<96;i++)
            {
                float a=i*2*Mathf.PI/96;
                ring.SetPosition(i,transform.position+new Vector3(Mathf.Sin(a)*radius,.12f,Mathf.Cos(a)*radius));
            }
            if(player.ChargedReady&&!readyPlayed){audioSource.PlayOneShot(ready);readyPlayed=true;}
        }
        public void Release(bool charged){audioSource.PlayOneShot(charged?heavy:quick);}
        void OnDestroy(){Destroy(quick);Destroy(heavy);Destroy(ready);}
    }
}
