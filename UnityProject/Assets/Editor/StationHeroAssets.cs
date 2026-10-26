using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using LastLight;

public static class StationHeroAssets
{
    const string ModelPath="Assets/ThirdParty/QuaterniusAnimation/UAL1_Standard.fbx";
    public static void Build(Transform visual,StationPlayer player,Material white,Material joints,Material trailMaterial,Mesh bladeMesh)
    {
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        // All motions are authored on this exact skeleton. Generic preserves the finger and weapon poses.
        importer.animationType=ModelImporterAnimationType.Generic;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;
        var settings=importer.defaultClipAnimations;
        foreach(var c in settings)
        {
            c.loopTime=c.name.EndsWith("_Loop")||c.name.EndsWith("Sword_Idle");
            c.loopPose=c.loopTime;c.lockRootRotation=true;c.lockRootPositionXZ=true;c.lockRootHeightY=true;
            c.keepOriginalOrientation=true;c.keepOriginalPositionXZ=true;c.keepOriginalPositionY=true;
        }
        importer.clipAnimations=settings;importer.SaveAndReimport();
        var clips=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
        AnimationClip Clip(string name){return clips.First(c=>c.name.EndsWith("|"+name)||c.name==name);}
        string path="Assets/Generated/WhiteMannequinV5.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine=controller.layers[0].stateMachine;
        foreach(var state in machine.states)machine.RemoveState(state.state);
        AnimatorState Add(string name,string clip,float speed=1)
        {
            var state=machine.AddState(name);state.motion=Clip(clip);state.speed=speed;return state;
        }
        machine.defaultState=Add("Idle","Sword_Idle");
        Add("Walk","Walk_Loop");Add("Jog","Jog_Fwd_Loop");Add("Air","Jump_Loop");Add("Dash","Sprint_Loop",1.4f);
        Add("Charge","Spell_Simple_Idle_Loop");Add("Attack","Sword_Attack",Clip("Sword_Attack").length/StationHumanoid.AttackDuration);
        EditorUtility.SetDirty(controller);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        model.name="Quaternius_WhiteMannequin";model.transform.SetParent(visual,false);
        model.transform.localScale=Vector3.one*.95f;
        model.transform.localRotation=Quaternion.Euler(0,180,0);
        var animator=model.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        foreach(var r in model.GetComponentsInChildren<Renderer>())
        {
            var materials=r.sharedMaterials;
            for(int i=0;i<materials.Length;i++)materials[i]=materials[i]!=null&&materials[i].name.Contains("Joints")?joints:white;
            r.sharedMaterials=materials;
            if(r is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
        }
        Transform Bone(string name){return model.GetComponentsInChildren<Transform>().First(t=>t.name==name);}
        var avatar=visual.gameObject.AddComponent<StationHumanoid>();avatar.player=player;avatar.animator=animator;
        avatar.torso=Bone("spine_01");avatar.leftArm=Bone("upperarm_l");avatar.rightArm=Bone("upperarm_r");
        avatar.leftElbow=Bone("lowerarm_l");avatar.rightElbow=Bone("lowerarm_r");avatar.leftThigh=Bone("thigh_l");avatar.rightThigh=Bone("thigh_r");avatar.leftKnee=Bone("calf_l");avatar.rightKnee=Bone("calf_r");
        var hand=Bone("hand_r");var index=Bone("index_01_r");var pinky=Bone("pinky_01_r");var middle=Bone("middle_01_r");
        var sword=new GameObject("SwordGrip").transform;sword.SetParent(hand,false);
        var direction=(index.position-pinky.position).normalized;var palmForward=(middle.position-hand.position).normalized;
        sword.position=(middle.position+hand.position)*.5f-direction*.07f;sword.rotation=Quaternion.LookRotation(direction,palmForward);
        var scale=hand.lossyScale;sword.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);avatar.sword=sword;
        void Part(string name,Vector3 pos,Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(sword,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
        }
        Part("SwordHandle",new Vector3(0,0,.07f),new Vector3(.045f,.045f,.22f),joints);
        Part("SwordGuard",new Vector3(0,0,.19f),new Vector3(.22f,.045f,.055f),joints);
        var blade=new GameObject("PointedSwordBlade");blade.transform.SetParent(sword,false);blade.AddComponent<MeshFilter>().sharedMesh=bladeMesh;blade.AddComponent<MeshRenderer>().sharedMaterial=white;
        var tip=new GameObject("BladeTip");tip.transform.SetParent(sword,false);tip.transform.localPosition=new Vector3(0,0,1.15f);
        var trail=tip.AddComponent<TrailRenderer>();trail.sharedMaterial=trailMaterial;trail.startColor=new Color(.2f,.85f,1);trail.endColor=Color.white;trail.startWidth=.16f;trail.endWidth=0;trail.time=.12f;trail.minVertexDistance=.03f;trail.emitting=false;avatar.slashTrail=trail;
        foreach(var c in model.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
        foreach(var t in visual.GetComponentsInChildren<Transform>())t.gameObject.layer=2;
        Clip("Sword_Idle").SampleAnimation(model,0);
    }
}
