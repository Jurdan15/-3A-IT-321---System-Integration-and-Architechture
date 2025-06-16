using Microsoft.Maui.Controls;

namespace bot;

public static class ThemeManager
{
    public static bool IsDarkMode { get; private set; } = false;

    public static void ApplyTheme(bool darkMode)
    {
        IsDarkMode = darkMode;

        Application.Current.Resources["PageBackgroundColor"] = darkMode ? Color.FromArgb("#121212") : Colors.White;
        Application.Current.Resources["TextColor"] = darkMode ? Colors.White : Colors.Black;
        Application.Current.Resources["HeaderBackgroundColor"] = darkMode ? Color.FromArgb("#1F1F1F") : Colors.Maroon;
        Application.Current.Resources["EntryBackgroundColor"] = darkMode ? Color.FromArgb("#333333") : Colors.Maroon;
        Application.Current.Resources["EntryTextColor"] = darkMode ? Colors.White : Colors.White;

        // Add these for theme toggle colors
        Application.Current.Resources["ThemeToggleTrackColor"] = darkMode ? Color.FromArgb("#424242") : Color.FromArgb("#E0E0E0");
        Application.Current.Resources["ThemeToggleThumbColor"] = darkMode ? Color.FromArgb("#212121") : Colors.White;
    }
}