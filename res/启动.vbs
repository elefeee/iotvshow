Set WshShell = CreateObject("WScript.Shell")
strExe = Chr(34) & WshShell.CurrentDirectory & "\IOTV.exe" & Chr(34)

' ========== 参数说明 ==========
' 命令行格式：IOTV.exe x y w h
' x = 窗口左边距（默认 305）
' y = 窗口上边距（默认 0）
' w = 窗口宽度（默认 屏幕宽度 - x）
' h = 窗口高度（默认 屏幕高度 - 41）
' 按需修改下面四个值即可
' ==============================

intX = 305
intY = 0
intW = -1
intH = -1

' 用 WMI 直接取屏幕分辨率，不弹任何窗口
Set objWMIService = GetObject("winmgmts:\\.\root\cimv2")
Set colItems = objWMIService.ExecQuery("SELECT * FROM Win32_VideoController")

For Each objItem in colItems
    If Not IsNull(objItem.CurrentHorizontalResolution) Then
        If intW = -1 Then intW = objItem.CurrentHorizontalResolution - intX
        If intH = -1 Then intH = objItem.CurrentVerticalResolution - 41
        Exit For
    End If
Next

WshShell.Run strExe & " " & intX & " " & intY & " " & intW & " " & intH, 0, False