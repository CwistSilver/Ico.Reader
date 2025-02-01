# Ico.Reader
<img width="192" height="auto" src="icon.png">

[![Ico.Reader](https://img.shields.io/nuget/vpre/Ico.Reader.svg?cacheSeconds=3600&label=Ico.Reader%20nuget)](https://www.nuget.org/packages/Ico.Reader)
[![NuGet](https://img.shields.io/nuget/dt/Ico.Reader.svg?cacheSeconds=3600&label=Downloads)](https://www.nuget.org/packages/Ico.Reader)

**`Ico.Reader`** is a cross-platform library designed for extracting icons and cursors from `.ico` and `.cur` **files**, as well as from **embedded resources** within `.exe` **and** `.dll` files.

## Key Features
- **Platform-Independent Design**: Extracts images from ICO, CUR, EXE, and DLL files without relying on Windows-specific functions, making it fully cross-platform.
- **Supports Both Icons and Cursors**: Reads both icons (.ico) and cursors (.cur) from standalone files and embedded resources within executables.
- **Format Conversion**: Converts BMP images to PNG format during extraction, supporting a more universally compatible image format across different platforms.
- **Efficient Memory Usage**: Implements a method to read icons that minimizes memory usage by delaying the loading of image data until it is needed.
- **Flexible Data Access**: Supports extracting ico's from file paths, byte arrays, and streams, accommodating various application scenarios.
- **Selective Image Extraction**: Detailed ICO information, including groupings and image references, is provided upfront.

## Getting Started

### Reading icoData
```cs
var IcoReader = new IcoReader();

// Reading from a file path (most memory-efficient)
IcoData iconFromPath = IcoReader.Read("path/to/your/icon.ico");
IcoData cursorFromPath = IcoReader.Read("path/to/your/cursor.cur");
IcoData icoFromPathDll = IcoReader.Read("path/to/your/user32.dll");
IcoData icoFromPathEXE = IcoReader.Read("path/to/your/regedit.exe");

// Reading from a byte array
byte[] icoBytes = File.ReadAllBytes("path/to/your/icon.ico");
IcoData icoFromBytes = IcoReader.Read(icoBytes);

// Reading from a stream (copies the stream for independent access)
using var stream = File.OpenRead("path/to/your/icon.ico")
IcoData icoFromStream = IcoReader.Read(stream: stream, copyStream: true);

// Reading from a stream without copying (as efficient as direct file reading)
using (var streamOrigin = File.OpenRead("path/to/your/icon.ico"))
{
    IcoData icoFromStreamDirect = IcoReader.Read(stream: streamOrigin, copyStream: false);
    // ✅ This is as memory-efficient as reading directly from a file.
    // 🔴 WARNING: All images must be accessed before closing the stream, 
    // otherwise an error will occur.
}
```
- `copyStream: true` → The stream is **copied**, allowing access to images even after the original stream is closed.
- `copyStream: false` → The stream is **used directly**, making it as **memory-efficient as reading from a file**, but the stream must remain open while accessing images.

### Retrieving Image from icoData

#### Retrieving Images by Index
Each image within an ico file is assigned a unique index, accessible through the ImageReferences collection within icoData. You can retrieve the image data by specifying this index.

```cs
// Synchronously retrieve image data by index
byte[] imageData = icoData.GetImage(0);

// Asynchronously retrieve image data by index
byte[] imageDataAsync = await icoData.GetImageAsync(0);
```

#### Retrieving Images by Group
ICO files, especially those embedded in executables (EXEs) or dynamic link libraries (DLLs), can organize images into groups.
`Ico.Reader` standardizes group handling by treating standalone ICO and CUR files as **single-group sources**, while DLLs and EXEs may contain **multiple groups** for icons and cursors.

Retrieving images by group involves specifying both the group object and the image index within that group.
The following examples illustrate synchronous and asynchronous retrieval methods:

##### Retrieving Images from Standalone ICO or CUR Files
ICO and CUR files contain only one image group.
To retrieve the first image in that group:
```cs
// Get the first image in the group as PNG data
var group = icoData.Groups[0];
byte[] groupImageData = icoData.GetImage(group, 0);
```

##### Retrieving Images from DLLs or EXEs
DLLs and EXEs may contain multiple image groups for both icons and cursors.
```cs
// Retrieve a cursor group (e.g., ID 105) and get the first image
var cursorGroup = icoData.GetGroup("105", IcoType.Cursor);
byte[] cursorImageData = icoData.GetImage(cursorGroup, 0);

// Retrieve an icon group (e.g., ID 32656) and get the first image
var iconGroup = icoData.GetGroup("32656", IcoType.Icon);
byte[] iconImageData = icoData.GetImage(iconGroup, 0);
```

##### Retrieving All Images Asynchronously by Groups
To iterate over all groups and retrieve all images asynchronously:
```cs
var imageDatas = new List<byte[]>();
foreach (var group in icoData.Groups)
{
    for (int i = 0; i < group.DirectoryEntries.Length; i++)
    {
        byte[] imageData = await icoData.GetImageAsync(group, i);
        imageDatas.Add(imageData);
    }
}
```

##### Retrieving All Images Asynchronously by image references
To iterate over all image references and retrieve all images asynchronously:
```cs
var imageDatas = new List<byte[]>();
foreach (var imageReference in icoData.ImageReferences)
{
    byte[] imageData = await icoData.GetImageAsync(imageReference);
    imageDatas.Add(imageData);
}
```


### Selecting the Preferred Image Based on Quality
To select the preferred image, `Ico.Reader` calculates a quality score for each image, taking into account its **dimensions and bit depth**.
This calculation applies a **weight factor to the bit depth** to adjust its influence on the overall quality score.
The preferred image is determined as the one with the **highest calculated quality score**.

You can retrieve the preferred image either **globally** (from all groups) or **from a specific group**:

```cs
// Selecting the preferred image globally from all groups
int preferredIndex = icoData.PreferredImageIndex(colorBitWeight: 2f);

// Selecting the preferred image from a specific group
int preferredGroupIndex = icoData.PreferredImageIndex(selectedGroup, colorBitWeight: 2f);
```

## Dependency Injection Support
For applications utilizing Dependency Injection, Ico.Reader provides an extension method to seamlessly register its services with the DI container. This enables easy configuration and integration into your projects, ensuring that all necessary components are available for ico reading and decoding tasks.

To add Ico.Reader services to your project's service collection:
```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddIcoReader();
}
```

## Dependencies
**'Ico.Reader'** is designed with minimal external dependencies to ensure lightweight integration into your projects. 

For projects utilizing **'Ico.Reader'**, the primary dependency to be aware of is:
- [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/)
