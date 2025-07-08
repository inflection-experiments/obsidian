using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using STLViewer.Core.Interfaces;
using STLViewer.Infrastructure.Parsers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using STLViewer.Math;
using Avalonia.Input;
using System.Linq;

namespace STLViewer.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        // Material controls
        var colorPickerButton = this.FindControl<Button>("ColorPickerButton");
        var materialPresetsComboBox = this.FindControl<ComboBox>("MaterialPresetsComboBox");

        if (colorPickerButton != null)
            colorPickerButton.Click += OnColorPickerClick;

        if (materialPresetsComboBox != null)
        {
            // Populate material presets
            materialPresetsComboBox.ItemsSource = Enum.GetValues<MaterialPreset>();
        }
    }

    private async void OnColorPickerClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel viewModel)
            return;

        try
        {
            // Simple color picker dialog using text input for now
            // In a production app, you'd use a proper color picker control
            var colorDialog = new Window
            {
                Title = "Pick Material Color",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(20) };

            stackPanel.Children.Add(new TextBlock { Text = "Enter color (hex format):", Margin = new Avalonia.Thickness(0, 0, 0, 10) });

            var colorTextBox = new TextBox
            {
                Text = viewModel.Viewport.MaterialColorHex,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(colorTextBox);

            var buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };

            var okButton = new Button { Content = "OK", Margin = new Avalonia.Thickness(0, 0, 10, 0) };
            var cancelButton = new Button { Content = "Cancel" };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);

            colorDialog.Content = stackPanel;

            bool? result = null;
            okButton.Click += (_, _) => { result = true; colorDialog.Close(); };
            cancelButton.Click += (_, _) => { result = false; colorDialog.Close(); };

            await colorDialog.ShowDialog(this);

            if (result == true && TryParseHexColor(colorTextBox.Text, out var color))
            {
                viewModel.Viewport.SetMaterialColor(color);
            }
        }
        catch (Exception ex)
        {
            // Simple error handling - in production you'd show a proper error dialog
            Console.WriteLine($"Color picker error: {ex.Message}");
        }
    }

    private static bool TryParseHexColor(string? hexColor, out Color color)
    {
        color = Color.White;

        if (string.IsNullOrWhiteSpace(hexColor))
            return false;

        hexColor = hexColor.Trim();
        if (hexColor.StartsWith("#"))
            hexColor = hexColor[1..];

        if (hexColor.Length != 6)
            return false;

        try
        {
            var r = Convert.ToByte(hexColor[0..2], 16) / 255.0f;
            var g = Convert.ToByte(hexColor[2..4], 16) / 255.0f;
            var b = Convert.ToByte(hexColor[4..6], 16) / 255.0f;

            color = new Color(r, g, b, 1.0f);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Check if the dragged data contains files
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files?.Any(f => f.Name.EndsWith(".stl", StringComparison.OrdinalIgnoreCase)) == true)
            {
                e.DragEffects = DragDropEffects.Copy;
                ShowDragDropOverlay(true);
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
                ShowDragDropOverlay(false);
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
            ShowDragDropOverlay(false);
        }
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        ShowDragDropOverlay(false);

        if (!e.Data.Contains(DataFormats.Files))
            return;

        var files = e.Data.GetFiles();
        if (files == null)
            return;

        var stlFiles = files
            .Where(f => f.Name.EndsWith(".stl", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Path.LocalPath)
            .ToList();

        if (!stlFiles.Any())
            return;

        if (DataContext is not ViewModels.MainWindowViewModel viewModel)
            return;

        try
        {
            if (stlFiles.Count == 1)
            {
                // Single file - use existing command
                await viewModel.LoadFileAsync(stlFiles[0]);
            }
            else
            {
                // Multiple files - use batch loading
                await viewModel.LoadMultipleFilesAsync(stlFiles);
            }
        }
        catch (Exception ex)
        {
            // Handle error - in production you'd show a proper error dialog
            Console.WriteLine($"Error loading dropped files: {ex.Message}");
        }
    }

    private void ShowDragDropOverlay(bool show)
    {
        var overlay = this.FindControl<Border>("DragDropOverlay");
        if (overlay != null)
        {
            overlay.IsVisible = show;
        }
    }
}
