# iotvshow
打开电脑看电视 Windows 桌面电视墙/动态壁纸风格播放器，支持网页电视与本地/在线视频（WebView2 + WinForms）

## 为什么做这个

起因很简单：想在电脑上边干活边"看电视"，但又不想开一堆浏览器标签占地方、切来切去。

试过 DreamScene2 当动态壁纸，效果不错但只能播视频文件，没法直接挂网页直播源。于是基于 DreamScene2 的架构改了一版，把 WebView2 嵌进去当"桌面电视墙"——CCTV、B站、本地视频都能铺在桌面上，还能多窗口切大小屏。

纯个人折腾，没有商业目的，能用就行。代码和配置都开源在这，有同样需求的人拿去改改就能用。


## 版权与许可

本仓库代码发布采用 MIT License，见仓库根 `LICENSE`。

因本软件基于 [DreamScene2](https://github.com/he55/DreamScene2) 修改，相关原始版权与许可声明保留在 `res/LICENSE.txt`，请勿移除。  
`res/LICENSE.txt` 中包含原项目 MIT 声明、本修改版说明、命令行调用说明及第三方/直播页免责声明。

`res/DS2Native.dll` 及相关资源如来自第三方/原项目资源，请以其附带声明和原项目说明为准。

## 运行效果截图

<img width="960" height="540" alt="2026-09-06_220014_456" src="https://github.com/user-attachments/assets/a1f20199-30f6-42c9-83fb-5e5b485e174d" />
<img width="960" height="540" alt="2026-09-06_220022_226" src="https://github.com/user-attachments/assets/424b7b37-e60f-4a2c-97ba-55794915da86" />


##更新（2026-xx-xx）：

修复浙江卫视嵌入大屏模式视频不全屏的问题。Release 包里的 res/ok.js 仍为旧版，需要手动替换：

打开仓库 → res/ok.js → 点 Raw → 右键另存为
替换你本地运行包里的 res/ok.js（同名覆盖）
