using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading;
using Avalonia.Interactivity;
using System;
using Avalonia;

namespace AIBookExtractor
{
    public partial class ProgressDialog : Window
    {
        private CancellationTokenSource _cancellationTokenSource;
        public static readonly StyledProperty<double> ProgressValueProperty =
            AvaloniaProperty.Register<ProgressDialog, double>(
                nameof(ProgressValue),
                0.0, // Default value
                coerce: (sender, value) => 
                {
                    double clampedValue = Math.Clamp(value, 0, 100);
                    if (sender is ProgressDialog dialog)
                    {
                        dialog.OnProgressValueChanged(clampedValue); // Trigger custom handler
                    }
                    return clampedValue;
                });

        public double ProgressValue
        {
            get => GetValue(ProgressValueProperty);
            set => SetValue(ProgressValueProperty, value); // Setter delegates to SetValue
        }

        public ProgressDialog()
        {
            InitializeComponent();
            DataContext = this; // Set DataContext to the current instance
            _cancellationTokenSource = new CancellationTokenSource();
            this.FindControl<Button>("CancelButton").Click += CancelButton_Click;
            ProgressValue = 0; // Initialize progress
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _cancellationTokenSource.Dispose();
        }

        public ProgressBar ProgressBarControl => this.FindControl<ProgressBar>("ProgressBar"); // Renamed to avoid conflict

        private void OnProgressValueChanged(double newValue)
        {
            ProgressBarControl?.InvalidateVisual(); // Force ProgressBar redraw
            InvalidateVisual(); // Force window redraw
            Console.WriteLine($"ProgressValue changed to {newValue:F2}%"); // Debug log
        }
    }
}