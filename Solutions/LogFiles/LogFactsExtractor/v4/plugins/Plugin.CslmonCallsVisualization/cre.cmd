set name=Plugin.CslmonCallsVisualization
dotnet new classlib --name %name% --target-framework-override net48

pushd %name%
    dotnet add package XService.Net2
    dotnet add package XService.UI
    dotnet add package System.Configuration.ConfigurationManager
    ::dotnet add package System.Data.Sqlite
popd

:end