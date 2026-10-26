# 打开工程与复现

## 直接试玩

从Release下载Windows包，解压后运行LastLightStation.exe，保留同目录Data、UnityPlayer.dll等文件。WASD移动，鼠标视角，左键挥刀，Shift闪避，Space跳跃，E交互，短按Q或长按1.2秒后松开，Tab地图，J回看档案，左右键翻阅，Esc关闭当前界面或暂停。

## Unity工程

1. 克隆仓库，或在Code菜单下载源码ZIP后解压。
2. 用Unity Hub打开`UnityProject`目录。已验证版本是2022.3.62f3c1，使用内置渲染管线和现有输入系统。其他补丁版未单独验证。
3. 等待Package Manager和模型导入完成。仓库包含Assets、Packages、ProjectSettings及所有.meta；Library由本机重建。
4. 打开`Assets/Scenes/LastLightStation.unity`，点击Play。
5. Windows构建使用File > Build Settings，目标Windows x86_64，场景列表已配置上述场景。输出到Assets以外的新目录。

当前保存场景即发布场景。`Last Light/Generate whitebox scene`用于重新生成，不是打开项目的必要步骤；生成器会重建关卡。配色相关工具和生成器都保留在Assets/Editor中。

## 代码入口

| 文件 | 职责 |
|---|---|
| StationPlan / StationTopology | 房间配置、两条代表路线、通道和解锁依赖 |
| StationDirector | 完成状态、旅行、桥转向、档案和路线选择 |
| StationRoom / StationInteractable | 本区条件与交互 |
| StationPlayer / StationHumanoid | 控制、技能、近战和角色动画 |
| StationEnemy / StationBoss / StationHazard | 威胁、预警、伤害和抑制 |
| StationGuidance / StationPassage | 实际路径引导与跨层通行 |
| StationArchive / StationHud | 档案数据、阅读、地图和状态界面 |

## 验证入口

Release对应构建的运行结果与查询回归证据见`docs/evidence`。仓库不包含本机Unity授权文件、缓存或自动录制运行副本。当前视频中的录制工具单独存在于制作工作区，并非打开工程后自动游玩的功能。
