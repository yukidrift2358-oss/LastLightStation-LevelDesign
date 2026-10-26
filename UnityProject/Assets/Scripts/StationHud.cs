using UnityEngine;

namespace LastLight
{
    public sealed class StationHud : MonoBehaviour
    {
        public StationDirector director;
        Font font;
        GUIStyle label,title,small,button,mapLabel;
        Texture2D white;
        Vector2 recordScroll;int lastRecord=-2;GUIStyle archiveBody,archiveInk,archiveMeta,archiveTitle,archiveButton;
        void Init()
        {
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},20);
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true,normal={textColor=new Color(.85f,.92f,.95f)}};
            small=new GUIStyle(label){fontSize=14};title=new GUIStyle(label){fontSize=30,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=18};white=Texture2D.whiteTexture;
            archiveBody=new GUIStyle(label){fontSize=21,normal={textColor=new Color(.17f,.23f,.22f)}};
            archiveInk=new GUIStyle(label){normal={textColor=new Color(.17f,.23f,.22f)}};
            archiveMeta=new GUIStyle(small){normal={textColor=new Color(.30f,.40f,.37f)}};
            archiveTitle=new GUIStyle(title){fontSize=32,normal={textColor=new Color(.17f,.23f,.22f)}};
            archiveButton=new GUIStyle(label){fontSize=18,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.94f,.94f,.88f)}};
            mapLabel=new GUIStyle(small){fontSize=12,alignment=TextAnchor.MiddleCenter,wordWrap=false,padding=new RectOffset(0,0,0,0)};
        }
        void Panel(Rect r,Color c) {GUI.color=c;GUI.DrawTexture(r,white);GUI.color=Color.white;}
        void Line(Vector2 a,Vector2 b,Color c,float width=2)
        {
            var old=GUI.matrix;Vector2 d=b-a;
            GUI.matrix=old*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg),Vector3.one);
            Panel(new Rect(0,0,d.magnitude,width),c);GUI.matrix=old;
        }
        void OnGUI()
        {
            if(label==null)Init();
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale;
            Panel(new Rect(24,24,430,152),new Color(.025f,.045f,.065f,.94f));
            GUI.Label(new Rect(42,34,380,42),"长夜泊站  /  守望站",title);
            string where=director.TutorialStage<4?"线性教学 "+(director.TutorialStage+1)+" / 4":director.Step<StationPlan.Count?StationTopology.Label(director.ActiveSector)+" · "+director.Step+"/26 已完成":"航行控制舱 · 末班协议";
            GUI.Label(new Rect(42,81,390,30),where,label);
            Panel(new Rect(42,126,245,9),new Color(.13f,.2f,.25f));Panel(new Rect(42,126,245*director.player.Health/100f,9),new Color(.2f,.8f,.85f));
            GUI.Label(new Rect(304,116,140,26),"生命 "+director.player.Health,small);
            GUI.Label(new Rect(42,143,390,24),"抑制 "+(director.player.PulseReady<=0?"就绪":director.player.PulseReady.ToString("0.0")+"s")+"   /   闪避 "+(director.player.DashReady<=0?"就绪":director.player.DashReady.ToString("0.0")+"s")+"   /   记录 "+director.RecordCount,small);
            Panel(new Rect(24,h-84,w-48,60),new Color(.025f,.045f,.065f,.9f));
            GUI.Label(new Rect(42,h-70,w-84,35),"WASD 移动   鼠标 视角   E 交互   Q 短按 / 长按蓄能   左键 攻击   Shift 闪避   Space 跳跃   Tab 地图   J 档案   Esc 暂停",label);
            if(!director.MapVisible&&!director.RecordOpen&&!director.Paused&&!director.Won&&!director.RouteChoiceOpen)
            {
                var room=director.CurrentRoom;
                if(director.TutorialStage>=4&&room!=null)
                {
                    Panel(new Rect(w-454,24,430,152),new Color(.025f,.045f,.065f,.94f));
                    GUI.Label(new Rect(w-436,34,394,28),StationPlan.Rest(room.sector)?"安全休息 · 生命与技能已恢复":"本区操作提示",label);
                    string task=StationPlan.Novelty(room.sector);
                    if((room.Need&RoomNeed.Relay)!=0)task+="\n标牌顺序："+room.RelayHint;
                    task+="\n"+room.Missing();
                    if(room.Complete)task="维护已完成，可回看记录。出口可选择已解锁通路。";
                    if(director.Travel!=null)task="前往 "+StationTopology.Label(director.TravelTo)+"\n"+(director.Bridging?"桥面正在对接，请站稳等待。":"沿流动灯带前进；跨层通道中没有额外遭遇。");
                    if(!room.Complete&&director.Travel==null&&room.scanner!=null&&(StationPlan.Timed(room.sector)||StationPlan.Sync(room.sector)))task+="\n出口窗口："+room.scanner.SuppressedRemaining.ToString("0.0")+"s";
                    GUI.Label(new Rect(w-436,68,394,100),task,small);
                }
                if(Time.unscaledTime<director.NoticeUntil)
                {Panel(new Rect(w/2-370,190,740,86),new Color(.025f,.045f,.065f,.94f));GUI.Label(new Rect(w/2-350,204,700,70),director.Notification,label);}
                if(director.player.Focus!=null)
                {Panel(new Rect(w/2-240,h-150,480,48),new Color(.1f,.2f,.23f,.96f));GUI.Label(new Rect(w/2-220,h-140,440,35),"[ E ]  "+director.player.Focus.title,label);}
                DrawObjective(w,h);
                Panel(new Rect(w/2-210,h-222,420,56),new Color(.025f,.045f,.065f,.92f));
                var p=director.player;
                string skill=p.Charging?(p.ChargedReady?"重抑制就绪 · 松开 Q 释放 16m / 7秒":"长按蓄能 "+(p.ChargeRatio*100).ToString("0")+"% · 松开为快速抑制"):"短按 Q：9m / 4秒   长按 1.2秒松开：16m / 7秒";
                GUI.Label(new Rect(w/2-198,h-216,396,30),skill,small);
                Panel(new Rect(w/2-198,h-184,396,5),new Color(.12f,.22f,.25f));
                Panel(new Rect(w/2-198,h-184,396*p.ChargeRatio,5),p.ChargedReady?new Color(1,.65f,.15f):Color.cyan);
            }
            if(director.boss.Active)
            {
                Panel(new Rect(w/2-260,140,520,52),new Color(.04f,.06f,.08f,.95f));
                GUI.Label(new Rect(w/2-242,144,480,28),"守门者 · 末班协议  "+(director.boss.Shielded?"[护盾]":"[可攻击]"),label);
                Panel(new Rect(w/2-242,177,484*director.boss.Health/18f,5),new Color(1,.4f,.2f));
            }
            if(director.RouteChoiceOpen){
                int n=director.DestinationChoices.Length;float top=h/2-100-n*24;
                Panel(new Rect(w/2-370,top,740,180+n*58),new Color(.025f,.05f,.07f,.99f));
                GUI.Label(new Rect(w/2-340,top+20,680,42),"维护通路 / 选择目的地",title);
                GUI.Label(new Rect(w/2-340,top+66,680,49),director.ActiveSector==7?"X：跨层档案 → 抑制接力 → 配电休息。Y：维修支援交火 → 温室维护 → 影院休息。两条均需完成，自选先后，返回时进度保留。":"选择已解锁通路。回访保留完成状态；未完成目标仍需在本区解决。",small);
                for(int i=0;i<n;i++){int id=director.DestinationChoices[i];string branch=director.ActiveSector==7?(id==8?"系统X · ":id==11?"货场Y · ":""):"";if(GUI.Button(new Rect(w/2-330,top+115+i*58,660,48),(director.rooms[id].Complete?"[回访] ":"[未完成] ")+branch+StationTopology.Label(id),button))director.ChooseDestination(id);}
                if(GUI.Button(new Rect(w/2-90,top+120+n*58,180,38),"留在本区 [Esc]",button))director.CloseChoices();
            }
            if(director.MapVisible)DrawMap(w,h);
            if(director.RecordOpen)DrawArchive(w,h);
            if(director.Paused&&!director.RecordOpen&&!director.MapVisible&&!director.Won&&!director.RouteChoiceOpen)
            {
                Panel(new Rect(w/2-200,h/2-150,400,300),new Color(.035f,.065f,.085f,.99f));
                GUI.Label(new Rect(w/2-160,h/2-120,320,45),"暂停 / 值班间隙",title);
                if(GUI.Button(new Rect(w/2-150,h/2-35,300,44),"继续",button))director.TogglePause();
                if(GUI.Button(new Rect(w/2-150,h/2+25,300,44),"从教学重新开始",button))director.Restart();
            }
            if(director.Won)
            {
                Panel(new Rect(w/2-360,h/2-210,720,420),new Color(.035f,.065f,.085f,.99f));
                GUI.Label(new Rect(w/2-320,h/2-175,640,55),"最后一次交接完成",title);
                GUI.Label(new Rect(w/2-320,h/2-95,640,160),"隔离事件档案与居民姓名已传回家园。\n发送不携带人员或机械载体；守望站继续留在深空，隔离边界仍然有效。\n\n他们曾亲手切断归途。现在，终于有人完成了交接。",label);
                if(GUI.Button(new Rect(w/2-130,h/2+120,260,48),"重新进入守望站",button))director.Restart();
            }
            GUI.matrix=Matrix4x4.identity;
        }
        void DrawArchive(float w,float h)
        {
            int id=director.RecordSector;if(lastRecord!=id){recordScroll=Vector2.zero;lastRecord=id;}
            float x=w/2-580,y=h/2-345;
            Panel(new Rect(0,0,w,h),new Color(.015f,.03f,.035f,.82f));
            Panel(new Rect(x,y,1160,690),new Color(.95f,.93f,.87f,1));
            Panel(new Rect(x,y,1160,6),new Color(.25f,.44f,.41f));
            GUI.Label(new Rect(x+32,y+23,950,27),"守望站  /  STATION ARCHIVE  /  离线只读档案",archiveMeta);
            GUI.Label(new Rect(x+32,y+68,1080,50),director.RecordTitle,archiveTitle);
            Panel(new Rect(x+32,y+132,1096,1),new Color(.72f,.76f,.70f));
            string code=id>=0?StationArchive.Code(id):"SW-SYS";
            GUI.Label(new Rect(x+32,y+156,250,60),code+"\n已解码  /  本地副本",archiveInk);
            if(id>=0){
                GUI.Label(new Rect(x+32,y+235,250,70),"来源\n"+StationArchive.Owners[id],archiveMeta);
                GUI.Label(new Rect(x+32,y+320,250,70),"事件时间\n隔离纪元 D+"+StationArchive.Days[id],archiveMeta);
                GUI.Label(new Rect(x+32,y+405,250,70),"索引位置\n"+StationArchive.Location(id),archiveMeta);
                GUI.Label(new Rect(x+32,y+490,250,55),"分类  /  "+StationArchive.Tags[id],archiveMeta);
                GUI.Label(new Rect(x+320,y+151,800,68),StationArchive.Summaries[id],archiveInk);
            }
            Panel(new Rect(x+294,y+153,1,418),new Color(.72f,.76f,.70f));
            string body=director.RecordText;float bodyHeight=Mathf.Max(340,archiveBody.CalcHeight(new GUIContent(body),768));
            recordScroll=GUI.BeginScrollView(new Rect(x+320,y+233,808,342),recordScroll,new Rect(0,0,770,bodyHeight));
            GUI.Label(new Rect(0,0,768,bodyHeight),body,archiveBody);GUI.EndScrollView();
            Panel(new Rect(x+32,y+594,1096,1),new Color(.72f,.76f,.70f));
            GUI.Label(new Rect(x+32,y+613,360,43),"本地已留档 "+director.RecordCount+" 件\n← / → 回看已读文件 · J 关闭",archiveMeta);
            if(ArchiveAction(new Rect(x+440,y+614,150,43),"← 上一份"))director.BrowseRecord(-1);
            if(ArchiveAction(new Rect(x+606,y+614,150,43),"下一份 →"))director.BrowseRecord(1);
            if(ArchiveAction(new Rect(x+788,y+614,340,43),"留档并继续  [Esc]"))director.CloseRecord();
        }
        bool ArchiveAction(Rect r,string text)
        {
            Panel(r,r.Contains(Event.current.mousePosition)?new Color(.29f,.48f,.43f):new Color(.20f,.36f,.32f));
            return GUI.Button(r,text,archiveButton);
        }
        void DrawObjective(float w,float h)
        {
            var target=director.ObjectivePosition;
            var screen=director.player.view.WorldToViewportPoint(target+Vector3.up*1.5f);
            float x=screen.x*w,y=(1-screen.y)*h;
            if(screen.z<0){x=w-x;y=h*.55f;}
            x=Mathf.Clamp(x,80,w-245);y=Mathf.Clamp(y,Time.unscaledTime<director.NoticeUntil?300:220,h-300);
            Panel(new Rect(x-4,y-4,8,8),new Color(1,.7f,.3f));
            float dy=target.y-director.player.transform.position.y;
            string altitude=Mathf.Abs(dy)>2?(dy>0?" ↑":" ↓")+Mathf.Abs(dy).ToString("0")+"m":"";
            GUI.Label(new Rect(x+12,y-14,210,70),director.ObjectiveName+"\n"+Vector3.Distance(director.player.transform.position,target).ToString("0")+"m"+altitude,small);
        }
        void DrawMap(float w,float h)
        {
            Panel(new Rect(0,0,w,h),new Color(.01f,.02f,.03f,.96f));
            Panel(new Rect(w/2-660,h/2-320,1320,640),new Color(.025f,.045f,.065f,.99f));
            GUI.Label(new Rect(w/2-630,h/2-300,1260,48),"守望站 / 三层维护路线",title);
            GUI.Label(new Rect(w/2-630,h/2-250,1260,40),"金色：目标   青色：完成   绿色：安全休息   白点：玩家。S1—S4为跨层建筑通道，E1/E2为观景升降台。出口可选择已解锁目的地。",small);
            int current=director.Travel!=null?director.TravelTo:director.ActiveSector;
            for(int f=0;f<3;f++)
            {
                Vector2 center=new Vector2(w/2-435+f*435,h/2+20);int count=StationPlan.Counts[f],offset=StationPlan.Offsets[f];
                GUI.Label(new Rect(center.x-190,h/2-198,380,44),StationPlan.FloorNames[f],label);
                var points=new Vector2[count];
                for(int j=0;j<count;j++){float a=StationPlan.Angle(offset+j)*Mathf.Deg2Rad;points[j]=center+new Vector2(Mathf.Sin(a),-Mathf.Cos(a))*150;}
                foreach(var link in StationTopology.Links){int a=link.a,b=link.b;if(StationPlan.Floor(a)==f&&StationPlan.Floor(b)==f)Line(points[a-offset],points[b-offset],(director.CompletedMask&link.required)==link.required?new Color(.2f,.65f,.7f):new Color(.12f,.2f,.24f),2);}
                foreach(var p in director.passages)if(p.Spec.kind==PassageKind.Stair||p.Spec.kind==PassageKind.Lift){int id=StationPlan.Floor(p.Spec.a)==f?p.Spec.a:StationPlan.Floor(p.Spec.b)==f?p.Spec.b:-1;if(id>=0){Vector2 at=points[id-offset];GUI.Label(new Rect(at.x-45,at.y-45,110,20),(p.Spec.kind==PassageKind.Stair?"跨层 S":"升降 E")+StationTopology.Label(p.Spec.Other(id)).Split(' ')[0],mapLabel);}}
                for(int j=0;j<count;j++)
                {
                    int id=offset+j;Color c=StationPlan.Rest(id)?new Color(.1f,.28f,.2f):new Color(.1f,.16f,.2f);
                    if(director.rooms[id].Complete)c=new Color(.1f,.5f,.6f);if(current==id)c=new Color(.65f,.4f,.12f);
                    Panel(new Rect(points[j].x-36,points[j].y-24,72,48),c);
                    GUI.Label(new Rect(points[j].x-36,points[j].y-24,72,48),(j+1).ToString("00")+"\n"+StationPlan.Names[id],mapLabel);
                }
                if(Mathf.Abs(director.player.transform.position.y-f*24)<12){var p=director.player.transform.position;var dot=center+new Vector2(p.x,-p.z)*150/42;dot.x=Mathf.Clamp(dot.x,center.x-205,center.x+205);dot.y=Mathf.Clamp(dot.y,center.y-185,center.y+185);Panel(new Rect(dot.x-4,dot.y-4,8,8),Color.white);}
                GUI.Label(new Rect(center.x-180,h/2+218,360,35),"高度 "+f*24+"m  /  "+count+" 扇区",small);
            }
            if(GUI.Button(new Rect(w/2-110,h/2+260,220,40),"返回关卡  [Tab]",button))director.ToggleMap();
        }
        void OnDestroy() {if(font!=null)Destroy(font);}
    }
}
