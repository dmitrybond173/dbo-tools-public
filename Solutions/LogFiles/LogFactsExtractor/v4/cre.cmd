rem dotnet new winforms --name LogFactsExtractor4a --target-framework-override net48

::pushd LogFactExtractor4a
    dotnet add package XService.Net2
    dotnet add package XService.UI
    dotnet add package System.Configuration.ConfigurationManager
    dotnet add package System.Data.Sqlite
::popd