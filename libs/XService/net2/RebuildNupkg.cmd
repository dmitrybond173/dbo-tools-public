rem 1. nuget spec

rem 2. edit XService.Net2.nuspec

rem 3. build XService.Net2 project

rem 4. Run this...
nuget pack XService.Net2.csproj -Verbosity detailed -Prop Configuration=Release -Symbols
