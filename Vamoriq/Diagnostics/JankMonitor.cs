using System.Timers;
using Microsoft.Extensions.Logging;

namespace Vamoriq.Diagnostics
{
    public sealed class JankMonitor : IDisposable
    {
        private readonly ILogger<JankMonitor> _logger;
        private readonly IDispatcher? _dispatcher;
        private readonly System.Timers.Timer _timer;
        private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(500);
        private readonly TimeSpan _warnThreshold = TimeSpan.FromMilliseconds(120);
        private bool _running;

        public JankMonitor(ILogger<JankMonitor> logger)
        {
            _logger = logger;
            _dispatcher = Application.Current?.Dispatcher;
            _timer = new System.Timers.Timer(_interval.TotalMilliseconds)
            {
                AutoReset = true,
                Enabled = false
            };
            _timer.Elapsed += OnTick;
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _logger.LogInformation("JankMonitor started (interval={Interval}ms, threshold={Threshold}ms)", _interval.TotalMilliseconds, _warnThreshold.TotalMilliseconds);
            _timer.Start();
        }

        private void OnTick(object? sender, ElapsedEventArgs e)
        {
            var scheduled = DateTime.UtcNow;
            try
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    var delay = DateTime.UtcNow - scheduled;
                    if (delay > _warnThreshold)
                    {
                        _logger.LogWarning("MainThread delay {DelayMs} ms (threshold {ThresholdMs} ms)", (int)delay.TotalMilliseconds, (int)_warnThreshold.TotalMilliseconds);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JankMonitor tick failed");
            }
        }

        public void Stop()
        {
            if (!_running) return;
            _timer.Stop();
            _running = false;
            _logger.LogInformation("JankMonitor stopped");
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
