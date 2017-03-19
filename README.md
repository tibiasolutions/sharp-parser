SharpParser
[![NuGet](https://img.shields.io/nuget/v/TibiaSolutions.SharpParser.svg?style=flat-square)](https://www.nuget.org/packages/TibiaSolutions.SharpParser)
===============

A C# crawler module to get tibia.com parsed data.  

## Installation

Download via [Nuget](https://www.nuget.org/packages/TibiaSolutions.SharpParser/) on `Package Manager Console`:
```
PM> Install-Package TibiaSolutions.SharpParser
```

## Basic Usage

### Player
```csharp
var player = new Player("Kharsek");
if (player.Data["exists"])
{
    Console.Write(player.Data["name"]);
}
else
{
    Console.Write("Character does not exist.");
}
```
