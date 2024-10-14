set SvcName=SrvcLstNjs

sc stop %SvcName%

if /i "%1" == "STOP" goto skipStart

sc start %SvcName%