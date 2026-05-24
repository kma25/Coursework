using System.Diagnostics;
using SystemWatcher.Models;

namespace SystemWatcher.Services
{
    /// <summary>
    /// Собирает CPU/RAM/HDD без аварийного завершения при частичных ошибках.
    /// </summary>
    public sealed class MetricCollector
    {
        private TimeSpan _lastCpuTime = Process.GetCurrentProcess().TotalProcessorTime;
        private DateTime _lastSampleTime = DateTime.UtcNow;

        /// <summary>
        /// Возвращает текущий снимок нагрузки.
        /// </summary>
        public MetricSnapshot Collect()
        {
            return new MetricSnapshot(
                CollectCpuPercent(),
                CollectRamPercent(),
                CollectHddPercent(),
                DateTime.UtcNow);
        }

        private double CollectCpuPercent()
        {
            var process = Process.GetCurrentProcess();
            var now = DateTime.UtcNow;
            var cpuDelta = process.TotalProcessorTime - _lastCpuTime;
            var timeDelta = now - _lastSampleTime;

            _lastCpuTime = process.TotalProcessorTime;
            _lastSampleTime = now;

            if (timeDelta.TotalMilliseconds <= 0)
            {
                return 0;
            }

            return Math.Clamp(cpuDelta.TotalMilliseconds / (Environment.ProcessorCount * timeDelta.TotalMilliseconds) * 100, 0, 100);
        }

        private static double CollectRamPercent()
        {
            var info = GC.GetGCMemoryInfo();
            if (info.TotalAvailableMemoryBytes <= 0)
            {
                return 0;
            }

            return Math.Clamp((double)GC.GetTotalMemory(forceFullCollection: false) / info.TotalAvailableMemoryBytes * 100, 0, 100);
        }

        private static double CollectHddPercent()
        {
            try
            {
                var drives = DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.TotalSize > 0).ToArray();
                if (drives.Length == 0)
                {
                    return 0;
                }

                var used = drives.Sum(drive => drive.TotalSize - drive.AvailableFreeSpace);
                var total = drives.Sum(drive => drive.TotalSize);
                return Math.Clamp((double)used / total * 100, 0, 100);
            }
            catch (IOException)
            {
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
        }
    }
}
