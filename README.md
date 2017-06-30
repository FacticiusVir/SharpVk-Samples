# SharpVk Samples
This repo contains a set of sample applications showing how to use [SharpVk](https://github.com/FacticiusVir/SharpVk) to access the Vulkan API for rendering graphics.

## Building the samples
The current sample set is contained in the [SharpVk-Samples.sln](https://github.com/FacticiusVir/SharpVk-Samples/blob/master/SharpVk-Samples.sln) solution and will build in Visual Studio 2015; any SharpVk dependencies can be downloaded from NuGet ([SharpVk](https://www.nuget.org/packages/SharpVk) & [SharpVk.Shanq](https://www.nuget.org/packages/SharpVk.Shanq)).

## Vulkan Dependencies
Current AMD Radeon & nVidia Geforce drivers for Windows are Vulkan compliant and should require no additional downloads, and Intel have [published a driver](https://downloadcenter.intel.com/download/26563/Intel-Graphics-Driver-for-Windows-) for their HD graphics chips too.

### Note
These samples were ported from C++ originals published (and very well explained) at [Vulkan Tutorial](https://vulkan-tutorial.com/).
