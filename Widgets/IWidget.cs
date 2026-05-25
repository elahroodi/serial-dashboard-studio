using System;
using System.Windows;

namespace SerialDebugPanel.Core
{
    public interface IWidget
    {
        string Key { get; }
        event EventHandler<string>? ValueChanged;
        void UpdateValue(string value);
        FrameworkElement GetControl();
    }
}