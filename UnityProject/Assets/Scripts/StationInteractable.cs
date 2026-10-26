using UnityEngine;

namespace LastLight
{
    public enum InteractionKind { Power,Seal,Record,Exit,TutorialPower,TutorialSeal,BossPower,BossSeal,Relay }
    public sealed class StationInteractable : MonoBehaviour
    {
        public StationDirector director;
        public StationRoom room;
        public InteractionKind kind;
        public int relayIndex;
        public string title;
        public void Use()
        {
            if(kind==InteractionKind.TutorialPower) { director.CompleteTutorial(0); return; }
            if(kind==InteractionKind.TutorialSeal) { director.CompleteTutorial(1); return; }
            if(kind==InteractionKind.BossPower) { director.boss.SwitchPower(); return; }
            if(kind==InteractionKind.BossSeal) { director.boss.SealLeak(); return; }
            if(room==null) return;
            if(!director.CanUseRoom(room)) { director.Notify("这一区尚未接入维护权限。请沿当前目标前进。"); return; }
            switch(kind)
            {
                case InteractionKind.Relay:room.Relay(relayIndex);break;
                case InteractionKind.Power: room.TogglePower(); break;
                case InteractionKind.Seal: room.Seal(); break;
                case InteractionKind.Record: room.ReadRecord(); break;
                case InteractionKind.Exit: director.TryCompleteRoom(room); break;
            }
        }
    }
}
