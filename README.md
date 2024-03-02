# Ico.Reader
<img width="192" height="auto" src="icon.png">

**'Ico.Reader'** is a cross-platform library specifically designed for extracting ICO icons from `.ico` files as well as embedded within `.exe` and `.dll` files.

## Key Features
- **Platform-Independent Design**: Operates independently of Windows-specific functions, enabling the extraction of ICO, EXE, and DLL images on any platform without reliance on platform-specific features.
- **Format Conversion**: Converts BMP images to PNG format during extraction, supporting a more universally compatible image format across different platforms.
- **Efficient Memory Usage**: Implements a method to read icons that minimizes memory usage by delaying the loading of image data until it is needed.
- **Flexible Data Access**: Supports extracting ico's from file paths, byte arrays, and streams, accommodating various application scenarios.
- **Selective Image Extraction**: Detailed ICO information, including groupings and image references, is provided upfront.

## Getting Started

### Reading icoData
```cs
var IcoReader = new IcoReader();

// Reading from a file path (most memory-efficient)
IcoData icoFromPath = IcoReader.Read("path/to/your/icon.ico");

// Reading from a byte array
byte[] icoBytes = File.ReadAllBytes("path/to/your/icon.ico");
IcoData icoFromBytes = IcoReader.Read(icoBytes);

// Reading from a stream
using var stream = File.OpenRead("path/to/your/icon.ico")
IcoData icoFromStream = IcoReader.Read(stream);
```

The method utilizing a file path is the most memory-efficient, as it allows the library to dynamically load image data from the filesystem only when needed. In contrast, when using byte arrays or streams, IcoReader stores all byte content in memory within the icoData instance to ensure image data can be accessed later.

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
Ico files, especially those embedded in executables (EXEs) or dynamic link libraries (DLLs), can organize images into groups. While ICO files naturally lack such groupings, **'Ico.Reader'** treats them as having a single default group to standardize the API across different ico sources.

Retrieving images by group involves specifying both the group name and the image index within that group. The following example illustrates synchronous and asynchronous retrieval within the same context for convenience:

```cs
// Synchronously retrieve image data by group and index
byte[] groupImageData = icoData.GetImage("1", 0);

// Asynchronously retrieve image data by group and index
byte[] groupImageDataAsync = await icoData.GetImageAsync("1", 0);

// Iterating over all groups to retrieve all images asynchronously
var imageDatas = new List<byte[]>();
foreach (var group in icoData.Groups)
{
    for (int i = 0; i < group.DirectoryEntries.Length; i++)
    {
        byte[] imageData = await icoData.GetImageAsync(group.Name, i);
        imageDatas.Add(imageData);
    }
}
```

### Selecting the Preferred Image Based on Quality
To select the preferred image, **'Ico.Reader'** calculates a quality score for each image, taking into account its dimensions and bit depth. This calculation uses a specified weight for the bit depth to adjust its influence on the overall quality score. The preferred image is then determined by the highest quality score.

```cs
// Selecting the preferred image globally
int preferredIndex = icoData.PreferredImageIndex(colorBitWeight: 2f);

// Selecting the preferred image within a specific group
int preferredGroupIndex = icoData.PreferredImageIndex(groupName: "1", colorBitWeight: 2f);
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
