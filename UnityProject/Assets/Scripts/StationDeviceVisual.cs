using UnityEngine;
namespace LastLight {
 // Presentation only: the original room and hazard remain the sole sources of state.
 public sealed class StationDeviceVisual:MonoBehaviour {
  public StationInteractable item;public StationHazard hazard;public Renderer[] status;
  MaterialPropertyBlock block;int last=-1;
  void Awake(){block=new MaterialPropertyBlock();}
  void Update(){int state=0;
   if(hazard!=null)state=hazard.SuppressedNow||!hazard.Powered?1:0;
   else if(item!=null){bool power=item.kind==InteractionKind.Power||item.kind==InteractionKind.TutorialPower||item.kind==InteractionKind.BossPower;
    if(power)state=(item.room!=null?item.room.PowerB:item.kind==InteractionKind.BossPower?item.director.boss.PowerB:item.director.TutorialStage>0)?1:0;
    else state=(item.room!=null?item.room.Sealed:item.kind==InteractionKind.BossSeal?item.director.boss.LeakSealed:item.director.TutorialStage>1)?1:0;
   }
   if(last==state)return;last=state;
   for(int i=0;i<status.Length;i++){Color c=hazard!=null?(state==1?new Color(.1f,.8f,.85f):new Color(1,.16f,.06f)):status.Length==2?(i==state?(i==0?new Color(1,.6f,.1f):Color.cyan):new Color(.055f,.09f,.11f)):(state==1?new Color(.2f,.8f,.65f):new Color(1,.6f,.1f));block.SetColor("_Color",c);block.SetColor("_EmissionColor",c*.65f);status[i].SetPropertyBlock(block);}
  }
 }
}