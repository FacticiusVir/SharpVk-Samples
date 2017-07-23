//The MIT License (MIT)
//
//Copyright (c) Andrew Armstrong/FacticiusVir 2016
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using GlmSharp;
using SharpVk.Khronos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SharpVk.HelloTriangle
{
    public class Program
    {
        private const int SurfaceWidth = 800;
        private const int SurfaceHeight = 600;

        private Form window;
        private Instance instance;
        private Surface surface;
        private PhysicalDevice physicalDevice;
        private Device device;
        private Queue graphicsQueue;
        private Queue presentQueue;
        private Swapchain swapChain;
        private Image[] swapChainImages;
        private ImageView[] swapChainImageViews;
        private RenderPass renderPass;
        private PipelineLayout pipelineLayout;
        private Pipeline pipeline;
        private ShaderModule fragShader;
        private ShaderModule vertShader;
        private Framebuffer[] frameBuffers;
        private CommandPool commandPool;
        private CommandBuffer[] commandBuffers;
        private Semaphore imageAvailableSemaphore;
        private Semaphore renderFinishedSemaphore;

        private Format swapChainFormat;
        private Extent2D swapChainExtent;

        public static void Main(string[] args)
        {
            new Program().Run();
        }

        public void Run()
        {
            this.InitialiseWindow();
            this.InitialiseVulkan();
            this.MainLoop();
            this.TearDown();
        }

        private void InitialiseWindow()
        {
            this.window = new Form
            {
                Text = "Vulkan",
                ClientSize = new System.Drawing.Size(SurfaceWidth, SurfaceHeight)
            };

            this.window.ClientSizeChanged += (x, y) => this.RecreateSwapChain();
        }

        private void InitialiseVulkan()
        {
            this.CreateInstance();
            this.CreateSurface();
            this.PickPhysicalDevice();
            this.CreateLogicalDevice();
            this.CreateSwapChain();
            this.CreateImageViews();
            this.CreateRenderPass();
            this.CreateShaderModules();
            this.CreateGraphicsPipeline();
            this.CreateFrameBuffers();
            this.CreateCommandPool();
            this.CreateCommandBuffers();
            this.CreateSemaphores();
        }

        private void MainLoop()
        {
            this.window.Show();

            while (!this.window.IsDisposed)
            {
                this.DrawFrame();

                Application.DoEvents();
            }
        }

        private void RecreateSwapChain()
        {
            this.device.WaitIdle();

            this.commandPool.FreeCommandBuffers(commandBuffers);

            foreach (var frameBuffer in this.frameBuffers)
            {
                frameBuffer.Destroy();
            }
            this.frameBuffers = null;

            this.pipeline.Destroy();
            this.pipeline = null;

            this.pipelineLayout.Destroy();
            this.pipelineLayout = null;

            foreach (var imageView in this.swapChainImageViews)
            {
                imageView.Destroy();
            }
            this.swapChainImageViews = null;

            this.renderPass.Destroy();
            this.renderPass = null;

            this.swapChain.Destroy();
            this.swapChain = null;

            this.CreateSwapChain();
            this.CreateImageViews();
            this.CreateRenderPass();
            this.CreateGraphicsPipeline();
            this.CreateFrameBuffers();
            this.CreateCommandBuffers();
        }

        private void TearDown()
        {
            device.WaitIdle();

            this.renderFinishedSemaphore.Destroy();
            this.renderFinishedSemaphore = null;

            this.imageAvailableSemaphore.Destroy();
            this.imageAvailableSemaphore = null;

            this.commandPool.Destroy();
            this.commandPool = null;

            foreach (var frameBuffer in this.frameBuffers)
            {
                frameBuffer.Destroy();
            }
            this.frameBuffers = null;

            this.fragShader.Destroy();
            this.fragShader = null;

            this.vertShader.Destroy();
            this.vertShader = null;

            this.pipeline.Destroy();
            this.pipeline = null;

            this.pipelineLayout.Destroy();
            this.pipelineLayout = null;

            foreach (var imageView in this.swapChainImageViews)
            {
                imageView.Destroy();
            }
            this.swapChainImageViews = null;

            this.renderPass.Destroy();
            this.renderPass = null;

            this.swapChain.Destroy();
            this.swapChain = null;

            this.device.Destroy();
            this.device = null;

            this.surface.Destroy();
            this.surface = null;

            this.instance.Destroy();
            this.instance = null;
        }

        private void DrawFrame()
        {
            uint nextImage = this.swapChain.AcquireNextImage(uint.MaxValue, this.imageAvailableSemaphore, null);

            this.graphicsQueue.Submit(new SubmitInfo[]
            {
                new SubmitInfo
                {
                    CommandBuffers = new CommandBuffer[] { this.commandBuffers[nextImage] },
                    SignalSemaphores = new[] { this.renderFinishedSemaphore },
                    WaitDestinationStageMask = new [] { PipelineStageFlags.ColorAttachmentOutput },
                    WaitSemaphores = new [] { this.imageAvailableSemaphore }
                }
            }, null);

            this.presentQueue.Present(new[] { this.renderFinishedSemaphore }, new[] { this.swapChain }, new uint[] { nextImage }, new Result[1]);
        }

        private void CreateInstance()
        {
            this.instance = Instance.Create(null, new[]
                {
                    "VK_KHR_surface",
                    "VK_KHR_win32_surface"
                },
                applicationInfo: new ApplicationInfo
                {
                    ApplicationName = "Hello Triangle",
                    ApplicationVersion = new Version(1, 0, 0),
                    EngineName = "SharpVk",
                    EngineVersion = new Version(0, 1, 1)
                });
        }

        private void CreateSurface()
        {
            this.surface = this.instance.CreateWin32Surface(IntPtr.Zero, this.window.Handle);
        }

        private void PickPhysicalDevice()
        {
            var availableDevices = this.instance.EnumeratePhysicalDevices();

            this.physicalDevice = availableDevices.First(IsSuitableDevice);
        }

        private void CreateLogicalDevice()
        {
            QueueFamilyIndices queueFamilies = FindQueueFamilies(this.physicalDevice);

            this.device = physicalDevice.CreateDevice(queueFamilies.Indices
                                                                        .Select(index => new DeviceQueueCreateInfo
                                                                        {
                                                                            QueueFamilyIndex = index,
                                                                            QueuePriorities = new[] { 1f }
                                                                        }).ToArray(),
                                                        null,
                                                        new[] { "VK_KHR_swapchain" });

            this.graphicsQueue = this.device.GetQueue(queueFamilies.GraphicsFamily.Value, 0);
            this.presentQueue = this.device.GetQueue(queueFamilies.PresentFamily.Value, 0);
        }

        private void CreateSwapChain()
        {
            SwapChainSupportDetails swapChainSupport = this.QuerySwapChainSupport(this.physicalDevice);

            uint imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            SurfaceFormat surfaceFormat = this.ChooseSwapSurfaceFormat(swapChainSupport.Formats);

            QueueFamilyIndices queueFamilies = this.FindQueueFamilies(this.physicalDevice);

            var indices = queueFamilies.Indices.ToArray();

            Extent2D extent = this.ChooseSwapExtent(swapChainSupport.Capabilities);

            this.swapChain = device.CreateSwapchain(surface,
                                                    imageCount,
                                                    surfaceFormat.Format,
                                                    surfaceFormat.ColorSpace,
                                                    extent,
                                                    1,
                                                    ImageUsageFlags.ColorAttachment,
                                                    indices.Length == 1
                                                        ? SharingMode.Exclusive
                                                        : SharingMode.Concurrent,
                                                    indices,
                                                    swapChainSupport.Capabilities.CurrentTransform,
                                                    CompositeAlphaFlags.Opaque,
                                                    this.ChooseSwapPresentMode(swapChainSupport.PresentModes),
                                                    true,
                                                    this.swapChain);

            this.swapChainImages = this.swapChain.GetImages();
            this.swapChainFormat = surfaceFormat.Format;
            this.swapChainExtent = extent;
        }

        private void CreateImageViews()
        {
            this.swapChainImageViews = this.swapChainImages
                                                .Select(image => device.CreateImageView(image,
                                                                                        ImageViewType.ImageView2d,
                                                                                        this.swapChainFormat,
                                                                                        new ComponentMapping(),
                                                                                        new ImageSubresourceRange
                                                                                        {
                                                                                            AspectMask = ImageAspectFlags.Color,
                                                                                            BaseMipLevel = 0,
                                                                                            LevelCount = 1,
                                                                                            BaseArrayLayer = 0,
                                                                                            LayerCount = 1
                                                                                        }))
                                                .ToArray();
        }

        private void CreateRenderPass()
        {
            this.renderPass = device.CreateRenderPass(new[]
                                                        {
                                                            new AttachmentDescription
                                                            {
                                                                Format = this.swapChainFormat,
                                                                Samples = SampleCountFlags.SampleCount1,
                                                                LoadOp = AttachmentLoadOp.Clear,
                                                                StoreOp = AttachmentStoreOp.Store,
                                                                StencilLoadOp = AttachmentLoadOp.DontCare,
                                                                StencilStoreOp = AttachmentStoreOp.DontCare,
                                                                InitialLayout = ImageLayout.Undefined,
                                                                FinalLayout = ImageLayout.PresentSourceKhr
                                                            }
                                                        },
                                                        new[]
                                                        {
                                                            new SubpassDescription
                                                            {
                                                                DepthStencilAttachment = new AttachmentReference
                                                                {
                                                                    Attachment = Constants.AttachmentUnused
                                                                },
                                                                PipelineBindPoint = PipelineBindPoint.Graphics,
                                                                ColorAttachments = new []
                                                                {
                                                                    new AttachmentReference
                                                                    {
                                                                        Attachment = 0,
                                                                        Layout = ImageLayout.ColorAttachmentOptimal
                                                                    }
                                                                }
                                                            }
                                                        },
                                                        new[]
                                                        {
                                                            new SubpassDependency
                                                            {
                                                                SourceSubpass = Constants.SubpassExternal,
                                                                DestinationSubpass = 0,
                                                                SourceStageMask = PipelineStageFlags.BottomOfPipe,
                                                                SourceAccessMask = AccessFlags.MemoryRead,
                                                                DestinationStageMask = PipelineStageFlags.ColorAttachmentOutput,
                                                                DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite
                                                            },
                                                            new SubpassDependency
                                                            {
                                                                SourceSubpass = 0,
                                                                DestinationSubpass = Constants.SubpassExternal,
                                                                SourceStageMask = PipelineStageFlags.ColorAttachmentOutput,
                                                                SourceAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite,
                                                                DestinationStageMask = PipelineStageFlags.BottomOfPipe,
                                                                DestinationAccessMask = AccessFlags.MemoryRead
                                                            }
                                                        });
        }


        private void CreateShaderModules()
        {
            ShaderModule CreateShader(string path)
            {
                var shaderData = LoadShaderData(path, out int codeSize);

                return device.CreateShaderModule(codeSize, shaderData);
            }
            
            this.vertShader = CreateShader(@".\Shaders\vert.spv");
            
            this.fragShader = CreateShader(@".\Shaders\frag.spv");
        }

        private void CreateGraphicsPipeline()
        {
            this.pipelineLayout = device.CreatePipelineLayout(null, null);

            this.pipeline = device.CreateGraphicsPipelines(null, new[]
            {
                    new GraphicsPipelineCreateInfo
                    {
                        Layout = this.pipelineLayout,
                        RenderPass = this.renderPass,
                        Subpass = 0,
                        VertexInputState = new PipelineVertexInputStateCreateInfo(),
                        InputAssemblyState = new PipelineInputAssemblyStateCreateInfo
                        {
                            PrimitiveRestartEnable = false,
                            Topology = PrimitiveTopology.TriangleList
                        },
                        ViewportState = new PipelineViewportStateCreateInfo
                        {
                            Viewports = new[]
                            {
                                new Viewport
                                {
                                    X = 0f,
                                    Y = 0f,
                                    Width = this.swapChainExtent.Width,
                                    Height = this.swapChainExtent.Height,
                                    MaxDepth = 1,
                                    MinDepth = 0
                                }
                            },
                            Scissors = new[]
                            {
                                new Rect2D
                                {
                                    Extent= this.swapChainExtent
                                }
                            }
                        },
                        RasterizationState = new PipelineRasterizationStateCreateInfo
                        {
                            DepthClampEnable = false,
                            RasterizerDiscardEnable = false,
                            PolygonMode = PolygonMode.Fill,
                            LineWidth = 1,
                            CullMode = CullModeFlags.Back,
                            FrontFace = FrontFace.Clockwise,
                            DepthBiasEnable = false
                        },
                        MultisampleState = new PipelineMultisampleStateCreateInfo
                        {
                            SampleShadingEnable = false,
                            RasterizationSamples = SampleCountFlags.SampleCount1,
                            MinSampleShading = 1
                        },
                        ColorBlendState = new PipelineColorBlendStateCreateInfo
                        {
                            Attachments = new[]
                            {
                                new PipelineColorBlendAttachmentState
                                {
                                    ColorWriteMask = ColorComponentFlags.R
                                                        | ColorComponentFlags.G
                                                        | ColorComponentFlags.B
                                                        | ColorComponentFlags.A,
                                    BlendEnable = false,
                                    SourceColorBlendFactor = BlendFactor.One,
                                    DestinationColorBlendFactor = BlendFactor.Zero,
                                    ColorBlendOp = BlendOp.Add,
                                    SourceAlphaBlendFactor = BlendFactor.One,
                                    DestinationAlphaBlendFactor = BlendFactor.Zero,
                                    AlphaBlendOp = BlendOp.Add
                                }
                            },
                            LogicOpEnable = false,
                            LogicOp = LogicOp.Copy,
                            BlendConstants = new float[4]
                        },
                        Stages = new[]
                        {
                            new PipelineShaderStageCreateInfo
                            {
                                Stage = ShaderStageFlags.Vertex,
                                Module = this.vertShader,
                                Name = "main"
                            },
                            new PipelineShaderStageCreateInfo
                            {
                                Stage = ShaderStageFlags.Fragment,
                                Module = this.fragShader,
                                Name = "main"
                            }
                        }
                    }
                }).Single();
        }

        private void CreateFrameBuffers()
        {
            Framebuffer Create(ImageView imageView) => device.CreateFramebuffer(renderPass,
                                                                                new[] { imageView },
                                                                                this.swapChainExtent.Width,
                                                                                this.swapChainExtent.Height,
                                                                                1);

            this.frameBuffers = this.swapChainImageViews.Select(Create).ToArray();
        }

        private void CreateCommandPool()
        {
            QueueFamilyIndices queueFamilies = FindQueueFamilies(this.physicalDevice);

            this.commandPool = device.CreateCommandPool(queueFamilies.GraphicsFamily.Value);
        }

        private void CreateCommandBuffers()
        {
            this.commandBuffers = device.AllocateCommandBuffers(this.commandPool, CommandBufferLevel.Primary, (uint)this.frameBuffers.Length);

            for (int index = 0; index < this.frameBuffers.Length; index++)
            {
                var commandBuffer = this.commandBuffers[index];

                commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);

                commandBuffer.BeginRenderPass(this.renderPass,
                                                this.frameBuffers[index],
                                                new Rect2D
                                                {
                                                    Extent = this.swapChainExtent
                                                },
                                                new ClearValue[1],
                                                SubpassContents.Inline);

                commandBuffer.BindPipeline(PipelineBindPoint.Graphics, this.pipeline);

                commandBuffer.Draw(3, 1, 0, 0);

                commandBuffer.EndRenderPass();

                commandBuffer.End();
            }
        }

        private void CreateSemaphores()
        {
            this.imageAvailableSemaphore = device.CreateSemaphore();
            this.renderFinishedSemaphore = device.CreateSemaphore();
        }

        private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
        {
            QueueFamilyIndices indices = new QueueFamilyIndices();

            var queueFamilies = device.GetQueueFamilyProperties();

            for (uint index = 0; index < queueFamilies.Length && !indices.IsComplete; index++)
            {
                if (queueFamilies[index].QueueFlags.HasFlag(QueueFlags.Graphics))
                {
                    indices.GraphicsFamily = index;
                }

                if (device.GetSurfaceSupport(index, this.surface))
                {
                    indices.PresentFamily = index;
                }
            }

            return indices;
        }

        private SurfaceFormat ChooseSwapSurfaceFormat(SurfaceFormat[] availableFormats)
        {
            if (availableFormats.Length == 1 && availableFormats[0].Format == Format.Undefined)
            {
                return new SurfaceFormat
                {
                    Format = Format.B8g8r8a8Unorm,
                    ColorSpace = ColorSpace.SrgbNonlinear
                };
            }

            foreach (var format in availableFormats)
            {
                if (format.Format == Format.B8g8r8a8Unorm && format.ColorSpace == ColorSpace.SrgbNonlinear)
                {
                    return format;
                }
            }

            return availableFormats[0];
        }

        private PresentMode ChooseSwapPresentMode(PresentMode[] availablePresentModes)
        {
            return availablePresentModes.Contains(PresentMode.Mailbox)
                    ? PresentMode.Mailbox
                    : PresentMode.Fifo;
        }

        public Extent2D ChooseSwapExtent(SurfaceCapabilities capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return capabilities.CurrentExtent;
            }
            else
            {
                return new Extent2D
                {
                    Width = Math.Max(capabilities.MinImageExtent.Width, Math.Min(capabilities.MaxImageExtent.Width, SurfaceWidth)),
                    Height = Math.Max(capabilities.MinImageExtent.Height, Math.Min(capabilities.MaxImageExtent.Height, SurfaceHeight))
                };
            }
        }

        SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice device)
        {
            return new SwapChainSupportDetails
            {
                Capabilities = device.GetSurfaceCapabilities(this.surface),
                Formats = device.GetSurfaceFormats(this.surface),
                PresentModes = device.GetSurfacePresentModes(this.surface)
            };
        }

        private bool IsSuitableDevice(PhysicalDevice device)
        {
            return device.EnumerateDeviceExtensionProperties(null).Any(extension => extension.ExtensionName == "VK_KHR_swapchain")
                    && FindQueueFamilies(device).IsComplete;
        }

        private static uint[] LoadShaderData(string filePath, out int codeSize)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            var shaderData = new uint[(int)Math.Ceiling(fileBytes.Length / 4f)];

            System.Buffer.BlockCopy(fileBytes, 0, shaderData, 0, fileBytes.Length);

            codeSize = fileBytes.Length;

            return shaderData;
        }

        private struct QueueFamilyIndices
        {
            public uint? GraphicsFamily;
            public uint? PresentFamily;

            public IEnumerable<uint> Indices
            {
                get
                {
                    if (this.GraphicsFamily.HasValue)
                    {
                        yield return this.GraphicsFamily.Value;
                    }

                    if (this.PresentFamily.HasValue && this.PresentFamily != this.GraphicsFamily)
                    {
                        yield return this.PresentFamily.Value;
                    }
                }
            }

            public bool IsComplete
            {
                get
                {
                    return this.GraphicsFamily.HasValue
                        && this.PresentFamily.HasValue;
                }
            }
        }

        private struct SwapChainSupportDetails
        {
            public SurfaceCapabilities Capabilities;
            public SurfaceFormat[] Formats;
            public PresentMode[] PresentModes;
        }
    }
}
