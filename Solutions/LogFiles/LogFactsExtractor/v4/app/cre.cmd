::set name=Plugin.CslmonCallsVisualization
::set name=Plugin.TcpGwVisualization
set name=LogFactsExtractor4a
dotnet new classlib --name %name% --target-framework-override net48

pushd %name%
    dotnet add package XService.Net2
    dotnet add package XService.UI
    dotnet add package System.Configuration.ConfigurationManager
    ::dotnet add package System.Data.Sqlite
popd

:end