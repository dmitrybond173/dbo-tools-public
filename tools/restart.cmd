set SvcName=SrvcLstNjs

net stop %SvcName%

if /i "%1" == "STOP" goto skipStart

net start %SvcName%

:skipStart
