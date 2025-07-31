using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AIBookExtractor.Views.Dialogs
{
    public partial class ProgressDialog : Window, INotifyPropertyChanged
    {
        private double _progressValue;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ProgressDialog()
        {
            InitializeComponent();
            DataContext = this;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                if (Math.Abs(_progressValue - value) > 0.01) // Only update if there's a meaningful change
                {
                    _progressValue = value;
                    OnPropertyChanged();
                    
                    // Force UI update
                    if (ProgressBar != null)
                    {
                        ProgressBar.Value = value;
                    }
                }
            }
        }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
            Close();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            _cancellationTokenSource.Cancel();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}