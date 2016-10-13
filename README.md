# SharpVk Samples
This repo contains a set of sample applications showing how to use [SharpVk](https://github.com/FacticiusVir/SharpVk) to access the Vulkan API for rendering graphics.

## Building the samples
The current sample set is contained in the [SharpVk-Samples.sln](https://github.com/FacticiusVir/SharpVk-Samples/blob/master/SharpVk-Samples.sln) solution and will build in Visual Studio 2015; any SharpVk dependencies can be downloaded from NuGet ([SharpVk](https://www.nuget.org/packages/SharpVk) & [SharpVk.Shanq](https://www.nuget.org/packages/SharpVk.Shanq)).

## Vulkan Dependencies
Current AMD Radeon & nVidia Geforce drivers for Windows are Vulkan compliant and should require no additional downloads, and Intel have [published a beta driver](https://software.intel.com/en-us/blogs/2016/03/14/new-intel-vulkan-beta-1540204404-graphics-driver-for-windows-78110-1540) for their HD graphics chips.

### Note
These samples were ported from C++ originals published (and very well explained) at [Vulkan Tutorial](https://vulkan-tutorial.com/).
