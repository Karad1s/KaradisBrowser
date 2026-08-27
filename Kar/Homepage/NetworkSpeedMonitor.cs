using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Kar.Homepage
{
    public class NetworkSpeedMonitor
    {
        private CancellationTokenSource? _cancellationTokenSource;
        public event Action<double, double>? SpeedUpd;

        public NetworkSpeedMonitor()
        {
            SpeedUpd += (download, upload) =>
            {
                //System.Diagnostics.Debug.WriteLine(
                //    $"DL:{download / 1024 / 1024:F2} MB/s |\n UP:{upload / 1024 / 1024:F2} MB/s");
            };
        }

        public void Start()
        {
            if (_cancellationTokenSource != null) return;

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => MonitorAsync(_cancellationTokenSource.Token));
        }
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }
        private async Task MonitorAsync(CancellationToken cancellationToken)
        {
            var previous = GetNetworkBytes();
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);

                var current = GetNetworkBytes();

                double downloadSpeed = current.download - previous.download;
                double uploadSpeed = current.upload - previous.upload;

                previous = current;

                SpeedUpd?.Invoke(downloadSpeed, uploadSpeed);
            }
        }
        private (long download, long upload) GetNetworkBytes()
        {
            long download = 0;
            long upload = 0;

            foreach(var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up) continue;

                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                var stats = networkInterface.GetIPv4Statistics();

                download += stats.BytesReceived;
                upload += stats.BytesSent;
            }
            return (download, upload);
        }
    }
}
