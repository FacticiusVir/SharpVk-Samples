using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpVk.SaschaWillems
{
    public class VulkanBuffer
        : IDisposable
    {
        private Buffer buffer;
        private Device device;
        private DeviceMemory memory;
        private DescriptorBufferInfo descriptor;
        private DeviceSize size = 0;
        private DeviceSize alignment = 0;
        private IntPtr mapped = IntPtr.Zero;

        private BufferUsageFlags usageFlags;

        private MemoryPropertyFlags memoryPropertyFlags;

        public void Map(ulong size = Constants.WholeSize, ulong offset = 0)
        {
            this.memory.MapMemory(offset, size, 0, ref this.mapped);
        }

        public void Unmap()
        {
            if (this.mapped != IntPtr.Zero)
            {
                this.memory.UnmapMemory();
                mapped = IntPtr.Zero;
            }
        }

        public void Bind(ulong offset = 0)
        {
            this.buffer.BindMemory(memory, offset);
        }

        public void SetupDescriptor(ulong size = Constants.WholeSize, ulong offset = 0)
        {
            this.descriptor.Offset = offset;
            this.descriptor.Buffer = this.buffer;
            this.descriptor.Range = size;
        }

        public void CopyTo(IntPtr data, ulong size)
        {
            MemoryUtil.Copy(mapped, data, (uint)size);
        }

        public void Flush(ulong size = Constants.WholeSize, ulong offset = 0)
        {
            this.device.FlushMappedMemoryRanges(new MappedMemoryRange
            {
                Memory = this.memory,
                Offset = offset,
                Size = size
            });
        }

        public void Invalidate(ulong size = Constants.WholeSize, ulong offset = 0)
        {
            this.device.FlushMappedMemoryRanges(new MappedMemoryRange
            {
                Memory = this.memory,
                Offset = offset,
                Size = size
            });
        }
        
        public void Dispose()
        {
            this.buffer?.Dispose();

            if (this.memory != null)
            {
                this.device.FreeMemory(this.memory);
            }
        }
    };
}
