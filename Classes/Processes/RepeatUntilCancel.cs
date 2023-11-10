using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.Diagnostics;

namespace DicingBlade.Classes.Processes
{
    public class RepeatUntilCancel
    {
        private readonly Func<Task> _next;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _cancellationTokenSourceForCancelling;

        public bool IsCancellationRequested => _cancellationTokenSource?.IsCancellationRequested ?? false;

        public RepeatUntilCancel(Func<Task> next)
        {
            _next = next;
        }
        public async Task StartAsync()
        {
            Guard.IsNotNull(_next, nameof(_next));
            if (!_cancellationTokenSource?.IsCancellationRequested ?? false) return;

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSourceForCancelling = new CancellationTokenSource();

            var cancellationToken = _cancellationTokenSource.Token;
            await Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _next.Invoke();
                }
            });
            _cancellationTokenSourceForCancelling.Cancel();
        }

        public async Task CancelAsync()
        {
            Guard.IsNotNull(_cancellationTokenSourceForCancelling, nameof(_cancellationTokenSourceForCancelling));
            var token = _cancellationTokenSourceForCancelling.Token;
            _cancellationTokenSource.Cancel();
            await Task.Run(() =>
            {
                while (!token.IsCancellationRequested) ;
            });
        }
    }
}

