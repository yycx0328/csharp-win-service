%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\InstallUtil WinService.exe
Net Start WinService
sc config WinService start= auto