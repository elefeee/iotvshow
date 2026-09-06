Set WshShell = CreateObject("WScript.Shell")
strExe = Chr(34) & WshShell.CurrentDirectory & "\IOTV.exe" & Chr(34)
WshShell.Run strExe & " --quit", 0, False