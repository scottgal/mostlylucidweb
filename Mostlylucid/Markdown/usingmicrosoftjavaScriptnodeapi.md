
Microsoft.JavaScript.NodeApi

https://github.com/microsoft/node-api-dotnet

https://github.com/metacall/libnode/releases/tag/v22.9.0

```xml
    <PropertyGroup>
      <!-- Ensure that the OS property is set -->
      <OS Condition="'$(OS)' == ''">$(MSBuildThisFileFullPath)</OS>
    </PropertyGroup>

    <ItemGroup Condition="'$(OS)' == 'Windows_NT'">
      <None Update="EmailSubscription\libnode-amd64-windows\libnode.dll">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="EmailSubscription\libnode-amd64-windows\libnode.exp">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="EmailSubscription\libnode-amd64-windows\libnode.ilk">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="EmailSubscription\libnode-amd64-windows\libnode.lib">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="EmailSubscription\libnode-amd64-windows\node.def">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="EmailSubscription\libnode-amd64-windows\node.exe">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="EmailSubscription\libnode-amd64-windows\node.exp">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="EmailSubscription\libnode-amd64-windows\node.lib">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="EmailSubscription\libnode-amd64-windows\node.map">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
    </ItemGroup>

    <ItemGroup Condition="'$(OS)' != 'Windows_NT'">
      <None Update="EmailSubscription\libnode-amd64-linux\libnode.so">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="EmailSubscription\libnode-amd64-linux\libnode.so.127">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="EmailSubscription\libnode-amd64-linux\node">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
    </ItemGroup>

```