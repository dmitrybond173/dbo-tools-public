rem https://nssm.cc/commands

rem nssm set SrvcLstNjs Application "C:\Program Files\nodejs\node.exe" C:\inetpub\wwwroot\SrvcLstNjs\SrvcLstNjs.js 

set SvcName=SrvcLstNjs
set HomePath=C:\inetpub\wwwroot\%SvcName%

nssm stop %SvcName%
nssm remove %SvcName%

nssm install %SvcName% "C:\Program Files\nodejs\node.exe" SrvcLstNjs.js
nssm set %SvcName% AppDirectory %HomePath%
nssm set %SvcName% AppEnvironmentExtra NODE_ENV=production
nssm set %SvcName% DisplayName "Services List (NJS)"

if exist "logs" goto cont1
  md logs
:cont1
nssm set %SvcName% AppStdout %HomePath%\logs\service.log
nssm set %SvcName% AppStderr %HomePath%\logs\service-error.log

netsh advfirewall firewall add rule name="SrvcLstNjs_1001" dir=in action=allow protocol=TCP localport=1001

nssm start %SvcName%