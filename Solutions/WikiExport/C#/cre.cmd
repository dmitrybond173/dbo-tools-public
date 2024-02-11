rem dotnet new console --name WikiExport --target-framework-override net472

dotnet new console --name ExportWiki

pushd ExportWiki

dotnet add package Microsoft.Extensions.Configuration
dotnet add package System.Configuration.ConfigurationManager

dotnet add package Selenium.Support
dotnet add package Selenium.WebDriver

dotnet add package XService.Net2

popd
