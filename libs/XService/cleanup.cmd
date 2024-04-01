if exist .vs rd /s /q .vs

pushd net2
if exist .vs rd /s /q .vs
if exist bin rd /s /q bin
if exist obj rd /s /q obj
popd

pushd ui
if exist .vs rd /s /q .vs
if exist bin rd /s /q bin
if exist obj rd /s /q obj
popd
