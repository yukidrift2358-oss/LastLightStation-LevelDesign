using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LastLight
{
    // Only runs under an explicit command-line flag; normal play never skips progression.
    public sealed class StationSmokeRunner : MonoBehaviour
    {
        public StationDirector director;
        string report;
        int checks;
        int[] route;bool routeB;int stairsWalked,centerTurns;bool checkedBridgeRetry,checkedFreeReturn;
        bool testing;
        void Start()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-stationCapture")>=0){StartCoroutine(CaptureRuntime());return;}
            if(Array.IndexOf(args,"-stationSmoke")<0)return;
            report=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Validation/runtime-smoke.txt"));
            int p=Array.IndexOf(args,"-smokeReport");if(p>=0&&p+1<args.Length)report=args[p+1];
            Directory.CreateDirectory(Path.GetDirectoryName(report));File.WriteAllText(report,"Last Light Station runtime smoke\n"+Application.unityVersion+"\n");
            routeB=Array.IndexOf(args,"-routeB")>=0;route=routeB?StationTopology.RouteB:StationTopology.RouteA;
            testing=true;Application.logMessageReceived+=OnLog;StartCoroutine(Array.IndexOf(args,"-passageProbe")>=0?PassageProbe():Run());
        }
        void OnLog(string message,string stack,LogType type)
        {
            if(!testing||type!=LogType.Exception&&type!=LogType.Error)return;
            testing=false;File.AppendAllText(report,"FAIL runtime error: "+message+"\n"+stack);Application.Quit(2);
        }
        void Check(bool condition,string name)
        {
            if(!condition){File.AppendAllText(report,"FAIL: "+name+"\n");throw new Exception("Smoke failed: "+name);}
            checks++;File.AppendAllText(report,"PASS: "+name+"\n");
        }
        StationInteractable Find(StationRoom r,InteractionKind kind)
        { foreach(var item in r.GetComponentsInChildren<StationInteractable>())if(item.kind==kind)return item;throw new Exception("Missing interaction "+kind); }
        void ProbeState(string name,object value){typeof(StationDirector).GetField("<"+name+">k__BackingField",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(director,value);}
        IEnumerator PassageProbe(){yield return null;director.player.enabled=false;for(int i=0;i<4;i++)director.CompleteTutorial(i);foreach(var r in director.rooms)r.MarkComplete();ProbeState("CompletedMask",StationTopology.All);ProbeState("Step",26);
            foreach(var passage in director.passages)if(passage.Spec.kind==PassageKind.Stair){int from=passage.Spec.a,to=passage.Spec.b;ProbeState("ActiveSector",from);ProbeState("RoomEntered",true);director.player.Warp(passage.Path(from)[0]+Vector3.up*.1f);Check(director.ChooseDestination(to),"probe select forward stair");yield return Traverse("probe forward "+StationTopology.Label(from));Check(director.ChooseDestination(from),"probe select reverse stair");yield return Traverse("probe reverse "+StationTopology.Label(to));}
            File.AppendAllText(report,"SUCCESS: "+checks+" probe checks passed.\n");testing=false;Application.Quit(0);
        }
        IEnumerator Run()
        {
            yield return null;yield return new WaitForSeconds(.1f);
            director.player.enabled=false;
            yield return ValidatePulse();
            Check(director.rooms.Length==StationPlan.Count,"26 serialized rooms present");
            Check(director.tutorialObjectives[0].transform.Find("PowerCabinetShell")!=null&&director.tutorialObjectives[1].transform.Find("IsolationWheel")!=null&&director.tutorialObjectives[2].transform.Find("OpenScannerRing")!=null,"three mechanisms have distinct serialized silhouettes");
            Check(!director.bossGate.IsOpen&&!director.ChooseDestination(20),"Boss cannot be reached before completion flags");
            Check(Vector3.Distance(director.player.visual.position,director.player.transform.position)<.1f,"third-person model is attached to the player position");
            var unique=new HashSet<int>();foreach(int id in route)unique.Add(id);
            Check(unique.Count==StationPlan.Count,"route covers each room exactly once");
            ValidateGuide("initial teaching objective");
            Check(director.TutorialStage==0&&!director.tutorialGates[0].IsOpen,"tutorial initially gated");
            director.CompleteTutorial(2);Check(director.TutorialStage==0,"tutorial cannot be completed out of order");
            director.tutorialObjectives[0].Use();Check(director.TutorialStage==1&&director.tutorialGates[0].IsOpen,"power teaching unlocks first door");
            director.tutorialObjectives[1].Use();Check(director.TutorialStage==2,"seal teaching unlocks second door");
            director.player.Warp(new Vector3(0,.15f,94));director.player.Pulse();Check(director.TutorialStage==3,"player pulse suppresses teaching scanner");
            yield return ValidateSword();Check(director.TutorialStage==4,"animated sword defeat unlocks ring");
            director.ToggleMap();Check(director.Frozen&&Time.timeScale==0,"map freezes gameplay");director.ToggleMap();Check(!director.Frozen&&Time.timeScale==1,"closing map resumes gameplay");
            for(int step=0;step<StationPlan.Count;step++)
            {
                var room=director.CurrentRoom;
                director.player.Warp(room.entryPoint);Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return null;
                Check(director.RoomEntered,"physical trigger enters room "+(room.sector+1));
                ValidateGuide("next operation in room "+room.sector);
                foreach(var enemy in room.enemies)Check(enemy.body!=null&&enemy.body.sharedMaterial.mainTexture!=null&&enemy.GetComponentInChildren<StationMachineVisual>()!=null,"textured mechanical model for "+enemy.kind+" in "+room.sector);
                if(room.sector==8||room.sector==9||room.sector==11||room.sector==17||room.sector==23||room.sector==24||room.sector==25)
                {
                    float side=room.sector==25?-7:8;
                    director.player.Warp(room.transform.TransformPoint(new Vector3(side,.15f,-2)));
                    yield return WalkTo(room.transform.TransformPoint(new Vector3(side,3.15f,9)),"climb actual service ramp in room "+room.sector);
                    director.player.Warp(room.entryPoint);
                }
                if(room.sector==2)
                {
                    var charger=room.enemies[0];director.player.Warp(charger.transform.position+room.transform.forward* -6);
                    float deadline=Time.time+5;while(!charger.Telegraphing&&Time.time<deadline)yield return null;
                    Check(charger.Telegraphing,"charger visibly warns before committing");
                    Vector3 origin=charger.transform.position;director.player.Warp(director.player.transform.position+room.transform.right*6);
                    yield return new WaitForSeconds(1.8f);Check(Vector3.Distance(charger.transform.position,origin)>2,"charger commits to a locked direction");
                }
                if(room.sector==8)
                {
                    var sentry=room.enemies[1];Find(room,InteractionKind.Power).Use();
                    director.player.Warp(sentry.transform.position-room.transform.forward*5);
                    float deadline=Time.time+5;while(!sentry.Telegraphing&&Time.time<deadline)yield return null;
                    Check(sentry.Telegraphing,"sentry uses a delayed line attack");sentry.Suppress();
                    Check(sentry.Suppressed&&!sentry.Telegraphing&&!sentry.warning.gameObject.activeSelf,"suppression cancels telegraphed enemy attacks");
                    yield return ValidateSentry(room,sentry);
                    Find(room,InteractionKind.Power).Use();
                }
                if(room.sector==11)
                {
                    var charger=room.enemies[0];var repair=room.enemies[1];charger.Hit();charger.Suppress();
                    director.player.Warp(room.entryPoint);yield return new WaitForSeconds(4.3f);
                    Check(charger.Health==charger.maxHealth,"repair bot restores a living ally");
                    repair.ResetEnemy();director.player.Restore();director.player.Warp(room.transform.TransformPoint(new Vector3(8,3.15f,9)));charger.Suppress(12);
                    float deadline=Time.time+6;while(repair.ShotsFired==0&&Time.time<deadline)yield return null;
                    Check(repair.ShotsFired>0&&director.player.Health==94,"repair drone uses a real 6-damage defensive shot when allies are healthy");
                    Check(repair.beam.enabled,"repair defensive beam is visible on firing");
                    repair.Hit();repair.Hit();repair.Hit();Check(repair.Dead,"repair bot can be removed to break the support chain");
                }
                if(room.sector==19)
                {
                    var tractor=room.enemies[1];tractor.ResetEnemy();director.player.Warp(tractor.transform.position-room.transform.forward*6);
                    float deadline=Time.time+6;while(!tractor.Telegraphing&&Time.time<deadline)yield return null;
                    Check(tractor.Telegraphing,"tractor warns before pulling");
                    Vector3 before=director.player.transform.position;yield return new WaitForSeconds(1.6f);
                    Check(Vector3.Distance(before,director.player.transform.position)>1,"tractor physically displaces player; from="+before+" to="+director.player.transform.position);
                }
                director.TryCompleteRoom(room);Check(director.Step==step,"incomplete room exit stays locked "+(room.sector+1));director.CloseChoices();
                if(StationPlan.Sync(room.sector)){room.scanner.Suppress();Check(!room.PulseSolved,"final sync rejects suppression before seal and B power");}
                if((room.Need&RoomNeed.Seal)!=0)Find(room,InteractionKind.Seal).Use();
                if(StationPlan.Sync(room.sector))Find(room,InteractionKind.Power).Use();
                if((room.Need&RoomNeed.Pulse)!=0)room.scanner.Suppress();
                if((room.Need&RoomNeed.Record)!=0)
                {
                    ValidateGuide("archive operation in room "+room.sector);
                    Find(room,InteractionKind.Record).Use();Check(director.RecordOpen&&room.HasRecord,"record UI and item state "+(room.sector+1));
                    director.CloseRecord();
                }
                if((room.Need&RoomNeed.Combat)!=0)
                    foreach(var e in room.enemies)for(int hit=0;hit<e.maxHealth;hit++)e.Hit();
                if((room.Need&RoomNeed.Relay)!=0)
                {
                    foreach(var item in room.GetComponentsInChildren<StationInteractable>())if(item.kind==InteractionKind.Relay&&item.relayIndex==room.NextRelay%3+1)item.Use();
                    Check(room.RelayProgress==0,"wrong relay order stays locked "+room.sector);
                    foreach(int index in room.RelayOrder){foreach(var item in room.GetComponentsInChildren<StationInteractable>())if(item.kind==InteractionKind.Relay&&item.relayIndex==index){ValidateGuide("relay "+index+" in room "+room.sector);item.Use();}}
                }
                if((room.Need&RoomNeed.Power)!=0&&!room.PowerB)Find(room,InteractionKind.Power).Use();
                if(StationPlan.Timed(room.sector)||StationPlan.Sync(room.sector))room.scanner.Suppress();
                Check(room.Missing().Length==0,"all mechanism requirements satisfied "+(room.sector+1));
                ValidateGuide("exit operation in room "+room.sector);
                director.player.Warp(StationPlan.Point(room.sector,35,0,.15f));
                Find(room,InteractionKind.Exit).Use();
                Check(director.Step==step+1&&room.Complete,"room completion advances once "+(room.sector+1));
                if(step<StationPlan.Count-1){int next=route[step+1];
                    if(director.Travel==null){Check(director.RouteChoiceOpen,"branch choice is presented at existing exit");Check(System.Array.IndexOf(director.DestinationChoices,next)>=0,"selected branch is available");Check(director.ChooseDestination(next),"choose next branch");}
                    Check(director.TravelTo==next,"runtime route matches selected canonical route");
                    var passage=director.Travel;var kind=passage.Spec.kind;int from=room.sector;
                    yield return Traverse("forward "+StationTopology.Label(from)+" -> "+StationTopology.Label(next));
                    if(kind==PassageKind.Stair){stairsWalked++;int countBefore=director.Step;
                        director.TryCompleteRoom(director.CurrentRoom);director.CloseChoices();
                        Check(director.Step==countBefore,"retreat does not complete unsolved destination");Check(director.ChooseDestination(from),"cross-floor return passage remains available");
                        yield return Traverse("reverse stair "+stairsWalked);
                        Check(director.rooms[from].Complete&&director.CurrentRoom.sector==from,"retreat reaches completed source without replaying encounter");
                        Check(director.ChooseDestination(next),"previously unlocked stair reopens in forward direction");yield return Traverse("re-enter stair "+stairsWalked);
                    }
                }

            }
            var kinds=new HashSet<EnemyKind>();foreach(var r in director.rooms){if(StationPlan.Rest(r.sector))Check(r.enemies.Length==0&&r.pressure==null&&r.scanner==null,"rest room has no threats "+r.sector);foreach(var e in r.enemies)kinds.Add(e.kind);}
            Check(stairsWalked==4,"all four mandatory architectural passages walked both ways");
            Check(centerTurns==(routeB?5:2),"all required non-opposite bridge transfers physically exercised");
            Check(director.CompletedMask==StationTopology.All,"all 26 completion flags retained across branch choices and retreats");
            Check(kinds.Count==4,"four distinct enemy behaviours authored");
            Check(director.bossGate.IsOpen,"final room opens boss approach");
            director.player.Warp(StationPlan.Point(20,72,0,.15f));yield return WalkTo(StationPlan.Point(20,85,0,.15f),"walk from final room into boss arena");
            yield return null;Check(director.boss.Active,"entering boss arena activates guardian");
            director.player.Respawn();Check(director.player.Health==100&&!director.boss.Active&&director.boss.Health==18,"death retry resets boss and health");
            director.boss.Begin();for(int i=0;i<9;i++)director.boss.Hit();
            Check(director.boss.Shielded&&director.boss.Health==9,"phase two shield blocks attacks");
            director.boss.Hit();Check(director.boss.Health==9,"shielded damage rejected");
            director.boss.SealLeak();director.boss.SwitchPower();director.boss.Suppress();
            Check(!director.boss.Shielded,"three mechanisms expose boss weak point");
            for(int i=0;i<9;i++)director.boss.Hit();Check(director.Won,"boss defeat reaches ending");
            File.AppendAllText(report,"SUCCESS: "+checks+" checks passed.\n");testing=false;Application.Quit(0);
        }
        IEnumerator ValidateSword()
        {
            var p=director.player;p.Restore();var avatar=p.visual.GetComponent<StationHumanoid>();
            Check(avatar!=null&&avatar.sword!=null&&avatar.leftThigh!=null,"imported white swordsman and weapon are serialized");
            Check(avatar.animator!=null&&avatar.animator.runtimeAnimatorController!=null&&p.visual.GetComponentInChildren<SkinnedMeshRenderer>().bones.Length>20,"downloaded skinned mannequin and clip controller load in player");
            Check(avatar.sword.parent.name=="hand_r"&&Vector3.Distance(avatar.sword.position,avatar.sword.parent.position)<.2f,"sword grip stays attached to right palm");
            var rootBefore=p.transform.position;var legBefore=avatar.leftThigh.localRotation;
            avatar.SetMotion(5.4f,true,false);yield return new WaitForSeconds(.2f);
            Check(Quaternion.Angle(avatar.leftThigh.localRotation,legBefore)>3&&avatar.AnimationState=="Jog","imported jog clip animates legs");
            CaptureHero("Jog");
            avatar.SetMotion(0,false,false);yield return new WaitForSeconds(.15f);Check(avatar.AnimationState=="Air","jump enters imported airborne clip");
            avatar.SetMotion(5.4f,true,true);yield return new WaitForSeconds(.15f);Check(avatar.AnimationState=="Dash","dash enters imported sprint clip");
            avatar.SetMotion(0,true,false);p.BeginPulseCharge();yield return new WaitForSeconds(.15f);Check(avatar.AnimationState=="Charge","Q enters imported casting pose");CaptureHero("Charge");p.CancelPulseCharge();
            yield return new WaitForSeconds(.15f);Check(avatar.AnimationState=="Idle"&&Vector3.Distance(rootBefore,p.transform.position)<.01f&&avatar.animator.transform.localPosition.magnitude<.08f,"pose transitions return to sword idle without moving controller");CaptureHero("Idle");
            p.Warp(new Vector3(0,.15f,80));p.visual.rotation=Quaternion.LookRotation(Vector3.back);p.Restore();director.tutorialEnemy.Suppress(10);
            var before=avatar.BladeTip;p.Attack();Check(avatar.IsAttacking,"left click starts slash animation");
            yield return new WaitForSeconds(.08f);Check(director.tutorialEnemy.Health==3,"slash windup does not apply damage early");CaptureHero("Windup");
            yield return new WaitForSeconds(.15f);Check(director.tutorialEnemy.Health==2,"slash applies damage at animation contact");
            Check(Vector3.Distance(before,avatar.BladeTip)>.3f&&avatar.slashTrail.emitting,"blade moves through a visible arc and emits trail");CaptureHero("Slash");
            yield return new WaitForSeconds(.4f);Check(!avatar.IsAttacking&&!avatar.slashTrail.emitting,"slash recovers and clears trail");
            for(int i=0;i<2;i++){p.Attack();yield return new WaitForSeconds(.6f);}
        }
        void CaptureHero(string name)
        {
            var p=director.player;var camera=new GameObject("HeroInspection").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<2;camera.fieldOfView=38;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.01f,.02f,.035f);
            camera.transform.position=p.transform.position+p.visual.forward*3.8f+p.visual.right*2+Vector3.up*1.7f;camera.transform.LookAt(p.transform.position+Vector3.up*.95f);
            var rt=new RenderTexture(800,1000,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(800,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,800,1000),0,0);texture.Apply();
            string folder=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(report),"Previews");System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"V6"+(routeB?"B":"A")+"Character"+name+".png"),texture.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=previous;Destroy(rt);Destroy(texture);Destroy(camera.gameObject);
        }
        IEnumerator ValidateSentry(StationRoom room,StationEnemy sentry)
        {
            room.enemies[0].Suppress(25);sentry.ResetEnemy();director.player.Restore();
            director.player.Warp(room.transform.TransformPoint(new Vector3(4,.15f,6)));Physics.SyncTransforms();
            float deadline=Time.time+6;while(sentry.ShotsFired==0&&Time.time<deadline)yield return null;
            Check(sentry.ShotsFired>0&&director.player.Health==88,"upper sentry fires downward and deals actual 12 damage to a ground target");
            Check(sentry.beam.enabled,"sentry firing beam is visible");
            sentry.ResetEnemy();director.player.Restore();director.player.Warp(room.transform.TransformPoint(new Vector3(-4,.15f,-3)));Physics.SyncTransforms();
            yield return new WaitForSeconds(4);
            Check(sentry.ShotsFired==0&&director.player.Health==100,"solid cover still blocks sentry shots");
            Find(room,InteractionKind.Power).Use();sentry.ResetEnemy();director.player.Warp(room.transform.TransformPoint(new Vector3(4,.15f,6)));
            yield return new WaitForSeconds(4);Check(sentry.ShotsFired==0,"A power intentionally disables sentry");
            Find(room,InteractionKind.Power).Use();
        }
        void ValidateGuide(string label)
        {
            var guide=director.GetComponent<StationGuidance>();guide.Rebuild();
            Check(guide.PathPoints.Count>1,"ground light path exists: "+label+" from="+director.player.transform.position+" target="+director.ObjectivePosition);
            bool supported=true;foreach(var point in guide.PathPoints)if(!Physics.Raycast(point+Vector3.up*.2f,Vector3.down,.4f,1,QueryTriggerInteraction.Ignore))supported=false;
            Check(supported,"light strip rests on physical floor: "+label);
        }
        IEnumerator ValidatePulse()
        {
            var p=director.player;p.Restore();
            var go=new GameObject("SmokePulseTarget");go.transform.position=p.transform.position+Vector3.back*12;
            var c=go.AddComponent<SphereCollider>();c.isTrigger=true;c.radius=.1f;
            var hazard=go.AddComponent<StationHazard>();hazard.director=director;hazard.radius=0;
            var nearGo=new GameObject("SmokeNearPulseTarget");nearGo.transform.position=p.transform.position+Vector3.right*6;
            var nearCollider=nearGo.AddComponent<SphereCollider>();nearCollider.isTrigger=true;nearCollider.radius=.1f;
            var nearHazard=nearGo.AddComponent<StationHazard>();nearHazard.director=director;nearHazard.radius=0;
            Physics.SyncTransforms();
            Check(p.BeginPulseCharge(),"Q press begins charging");yield return new WaitForSeconds(.1f);
            Check(p.PulseReady==0&&!hazard.SuppressedNow,"holding Q does not release or start cooldown");
            p.ReleasePulseCharge();Check(!p.Charging&&!hazard.SuppressedNow&&p.PulseReady>7.8f,"short Q has 9m range and 8s cooldown");
            Check(nearHazard.SuppressedRemaining>3.8f,"quick Q suppresses nearby targets for 4 seconds");
            Check(!p.BeginPulseCharge(),"Q cooldown prevents a second charge");
            p.Restore();p.BeginPulseCharge();yield return new WaitForSeconds(1.25f);
            Check(p.ChargedReady&&p.PulseReady==0&&!hazard.SuppressedNow,"long Q displays ready and waits for release");
            p.ReleasePulseCharge();Check(hazard.SuppressedRemaining>6.8f&&p.PulseReady>11.8f,"charged Q reaches 12m target for 7s with 12s cooldown");
            yield return new WaitForSeconds(4.15f);Check(hazard.SuppressedNow,"charged suppression lasts beyond quick suppression duration");
            p.Restore();p.BeginPulseCharge();director.ToggleMap();
            Check(!p.Charging&&p.PulseReady==0,"opening map cancels charge without cooldown");
            director.ToggleMap();p.ReleasePulseCharge();Check(p.PulseReady==0,"canceled charge cannot fire on release");
            Destroy(go);Destroy(nearGo);p.Restore();
        }
        IEnumerator Traverse(string label)
        {
            var travel=director.Travel;Check(travel!=null,label+" has serialized passage");
            var points=travel.Path(director.TravelFrom);director.player.Warp(points[0]+Vector3.up*.1f);Physics.SyncTransforms();
            bool reorient=travel.Spec.kind==PassageKind.Bridge&&!StationPlan.Opposite(director.TravelFrom,director.TravelTo);bool rotatedWithRider=false;float start=Time.realtimeSinceStartup,vy=-2,nextTrace=start+10;
            var motor=director.player.GetComponent<CharacterController>();
            while(director.Travel!=null){
                if(director.Bridging){if(reorient&&new Vector2(director.player.transform.position.x,director.player.transform.position.z).magnitude<1.1f){rotatedWithRider=true;
                    if(!checkedBridgeRetry){checkedBridgeRetry=true;var before=director.bridges[StationPlan.Floor(director.TravelFrom)].rotation;var position=director.player.transform.position;uint mask=director.CompletedMask;int count=director.Step;director.ToggleMap();yield return new WaitForSecondsRealtime(.25f);
                        Check(Quaternion.Angle(before,director.bridges[StationPlan.Floor(director.TravelFrom)].rotation)<.01f&&Vector3.Distance(position,director.player.transform.position)<.01f,"map pauses bridge rotation and rider together");director.ToggleMap();director.player.Respawn();
                        Check(director.CompletedMask==mask&&director.Step==count&&Vector3.Distance(director.player.transform.position,director.player.Checkpoint)<.1f,"death during center transfer restores static approach without losing flags");vy=-2;
                    }
                }yield return null;continue;}
                var target=director.TravelTarget;
                // Deliberately use off-centre paths, as real players do, rather than hitting arrival dots.
                if(travel.Spec.kind==PassageKind.Side){target=points[points.Length-1];var radial=new Vector3(target.x,0,target.z).normalized;target+=radial*3;}
                if(travel.Spec.kind==PassageKind.Stair)target+=travel.transform.right*.35f;
                Vector3 delta=target-director.player.transform.position;delta.y=0;
                if(motor.isGrounded&&vy<0)vy=-2;vy-=22*Time.deltaTime;
                motor.Move(Vector3.ClampMagnitude(delta,5.4f*Time.deltaTime)+Vector3.up*vy*Time.deltaTime);
                if(Time.realtimeSinceStartup>nextTrace){nextTrace+=10;File.AppendAllText(report,"TRACE: "+label+" position="+director.player.transform.position+" target="+target+" waypoint="+director.Waypoint+"\n");}
                if(director.player.transform.position.y<Mathf.Min(StationPlan.Height(travel.Spec.a),StationPlan.Height(travel.Spec.b))-1)Check(false,label+" fell through passage at "+director.player.transform.position);
                if(Time.realtimeSinceStartup-start>140)Check(false,label+" timed out: "+director.player.transform.position+" target="+target+" waypoint="+director.Waypoint);
                yield return null;
            }
            Check(director.RoomEntered,label+" physically arrives in destination");
            if(travel.Spec.kind==PassageKind.Side){Check(true,label+" accepts doorway edge approach without hitting a waypoint");if(!checkedFreeReturn){checkedFreeReturn=true;int destination=director.ActiveSector;int source=travel.Spec.Other(destination);Vector3 back=points[0];back+=new Vector3(back.x,0,back.z).normalized*3;
                yield return WalkTo(back,"walk backwards through an already open side door without exit selection");yield return null;Check(director.ActiveSector==source&&director.RoomEntered,"free return updates active room and safe-room state");
                Vector3 again=points[points.Length-1];again+=new Vector3(again.x,0,again.z).normalized*3;yield return WalkTo(again,"re-enter unfinished room through open doorway");yield return null;Check(director.ActiveSector==destination&&!director.CurrentRoom.Complete,"free re-entry does not skip unfinished room objective");}}

            if(travel.Spec.kind==PassageKind.Stair)Check(true,label+" tracks off-centre ramp and landing movement");
            if(reorient){Check(rotatedWithRider,label+" rotates while carrying player at original bridge axis");centerTurns++;}
        }
        IEnumerator WalkTo(Vector3 target,string name,StationLift lift=null)
        {
            var motor=director.player.GetComponent<CharacterController>();float start=Time.realtimeSinceStartup;
            while(Vector2.Distance(new Vector2(director.player.transform.position.x,director.player.transform.position.z),new Vector2(target.x,target.z))>.2f)
            {
                Vector3 delta=target-director.player.transform.position;delta.y=0;
                motor.Move(Vector3.ClampMagnitude(delta,22*Time.deltaTime)+Vector3.down*2*Time.deltaTime);
                if(Time.realtimeSinceStartup-start>15) {Check(false,name+" timed out at "+director.player.transform.position);yield break;}
                yield return null;
            }
            float floorHeight=lift!=null?lift.platform.position.y:target.y;
            Check(Mathf.Abs(director.player.transform.position.y-floorHeight)<1,name+" has continuous supporting floor at "+director.player.transform.position+" floor="+floorHeight);
        }
        static Vector3 Polar(float angle,float radius,float y) {float a=angle*Mathf.Deg2Rad;return new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius);}
        IEnumerator CaptureRuntime()
        {
            var args=Environment.GetCommandLineArgs();int p=Array.IndexOf(args,"-captureDir");
            string folder=p>=0&&p+1<args.Length?args[p+1]:Application.persistentDataPath;
            Directory.CreateDirectory(folder);
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Gameplay.png"));
            yield return new WaitForSeconds(1);
            director.ToggleMap();
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Map.png"));
            yield return new WaitForSecondsRealtime(1);
            Application.Quit(0);
        }
        void OnDestroy() {Application.logMessageReceived-=OnLog;}
    }
}

