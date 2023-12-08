rmdir /s /q .vs

pushd app
rmdir /s /q bin
rmdir /s /q obj
popd

pushd plugins\Plugin.CslmonCallsVisualization
rmdir /s /q bin
rmdir /s /q obj
popd

pushd plugins\Plugin.CslmonSyncVisualization
rmdir /s /q bin
rmdir /s /q obj
popd

pushd plugins\Plugin.TcpGwVisualization
rmdir /s /q bin
rmdir /s /q obj
popd


