using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DynamicWin.Main
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public static SettingsWindow Instance { get; private set; }

        protected override void OnClosed(EventArgs e)
        {
            Instance = null;
            base.OnClosed(e);
        }

        bool showing = false;

        public SettingsWindow()
        {
            if (Instance != null) return;

            Instance = this;
            InitializeComponent();

            // Display window

            UpdateValues();
            Show();

            showing = true;
        }

        void SetValues()
        {
            if(!showing) return;

            Settings.IslandMode = IslandModeComboBox.SelectedIndex == 0 ? IslandObject.IslandMode.Notch : IslandObject.IslandMode.Island;
            Settings.AllowBlur = AllowBlur.IsChecked.Value;
            Settings.AllowAnimation = AllowAnimation.IsChecked.Value;
            Settings.AntiAliasing = AllowAntiAliasing.IsChecked.Value;

            Settings.Theme = BuiltInThemeComboBox.SelectedIndex;
            Settings.UseCustomTheme = UseCustomTheme.IsChecked.Value;
            Settings.CustomTheme = new ThemeHolder()
            {
                IslandColor = ToHex(CTIslandColor.SelectedColor.Value),
                TextMain = ToHex(CTTextMain.SelectedColor.Value),
                TextSecond = ToHex(CTTextSecond.SelectedColor.Value),
                TextThird = ToHex(CTTextThird.SelectedColor.Value),
                Primary = ToHex(CTPrimary.SelectedColor.Value),
                Secondary = ToHex(CTSecondary.SelectedColor.Value),
                Success = ToHex(CTSuccess.SelectedColor.Value),
                Error = ToHex(CTError.SelectedColor.Value),
                IconColor = ToHex(CTIconColor.SelectedColor.Value),
                WidgetBackground = ToHex(CTWidgetBackground.SelectedColor.Value)
            };

            Theme.Instance.UpdateTheme();
            MainForm.Instance.AddRenderer();
        }

        private static String ToHex(System.Windows.Media.Color c)
            => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

        void UpdateValues()
        {
            IslandModeComboBox.SelectedIndex = (Settings.IslandMode == IslandObject.IslandMode.Notch) ? 0 : 1;

            AllowBlur.IsChecked = Settings.AllowBlur;
            AllowAnimation.IsChecked = Settings.AllowAnimation;
            AllowAntiAliasing.IsChecked = Settings.AntiAliasing;

            BuiltInThemeComboBox.SelectedIndex = Settings.Theme;
            UseCustomTheme.IsChecked = Settings.UseCustomTheme;

            BuiltInThemeComboBox.Opacity = !Settings.UseCustomTheme ? 1f : 0.5f;
            BuiltInThemeComboBox.IsEnabled = !Settings.UseCustomTheme;

            CTIslandColor.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTTextMain.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTTextSecond.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTTextThird.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTPrimary.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTSecondary.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTSuccess.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTError.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTIconColor.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTWidgetBackground.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;

            CTIslandColor.IsEnabled = Settings.UseCustomTheme;
            CTTextMain.IsEnabled = Settings.UseCustomTheme;
            CTTextSecond.IsEnabled = Settings.UseCustomTheme;
            CTTextThird.IsEnabled = Settings.UseCustomTheme;
            CTPrimary.IsEnabled = Settings.UseCustomTheme;
            CTSecondary.IsEnabled = Settings.UseCustomTheme;
            CTSuccess.IsEnabled = Settings.UseCustomTheme;
            CTError.IsEnabled = Settings.UseCustomTheme;
            CTIconColor.IsEnabled = Settings.UseCustomTheme;
            CTWidgetBackground.IsEnabled = Settings.UseCustomTheme;

            CTIslandColor.SelectedColor = Col.FromHex(Settings.CustomTheme.IslandColor).ValueSystemMedia();
            CTTextMain.SelectedColor = Col.FromHex(Settings.CustomTheme.TextMain).ValueSystemMedia();
            CTTextSecond.SelectedColor = Col.FromHex(Settings.CustomTheme.TextSecond).ValueSystemMedia();
            CTTextThird.SelectedColor = Col.FromHex(Settings.CustomTheme.TextThird).ValueSystemMedia();
            CTPrimary.SelectedColor = Col.FromHex(Settings.CustomTheme.Primary).ValueSystemMedia();
            CTSecondary.SelectedColor = Col.FromHex(Settings.CustomTheme.Secondary).ValueSystemMedia();
            CTSuccess.SelectedColor = Col.FromHex(Settings.CustomTheme.Success).ValueSystemMedia();
            CTError.SelectedColor = Col.FromHex(Settings.CustomTheme.Error).ValueSystemMedia();
            CTIconColor.SelectedColor = Col.FromHex(Settings.CustomTheme.IconColor).ValueSystemMedia();
            CTWidgetBackground.SelectedColor = Col.FromHex(Settings.CustomTheme.WidgetBackground).ValueSystemMedia();
        }

        void Interact()
        {
            SetValues();
            UpdateValues();
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //SetValues();
        }

        private void Click(object sender, RoutedEventArgs e)
        {
            //Interact();
        }

        private void SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            //SetValues();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Interact();
        }

        private void UseCustomTheme_Click(object sender, RoutedEventArgs e)
        {
            BuiltInThemeComboBox.Opacity = !Settings.UseCustomTheme ? 1f : 0.5f;
            BuiltInThemeComboBox.IsEnabled = !Settings.UseCustomTheme;

            CTIslandColor.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTTextMain.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTTextSecond.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTTextThird.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTPrimary.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTSecondary.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTSuccess.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTError.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTIconColor.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;
            CTWidgetBackground.Opacity = Settings.UseCustomTheme ? 1f : 0.5f;

            CTIslandColor.IsEnabled = Settings.UseCustomTheme;
            CTTextMain.IsEnabled = Settings.UseCustomTheme;
            CTTextSecond.IsEnabled = Settings.UseCustomTheme;
            CTTextThird.IsEnabled = Settings.UseCustomTheme;
            CTPrimary.IsEnabled = Settings.UseCustomTheme;
            CTSecondary.IsEnabled = Settings.UseCustomTheme;
            CTSuccess.IsEnabled = Settings.UseCustomTheme;
            CTError.IsEnabled = Settings.UseCustomTheme;
            CTIconColor.IsEnabled = Settings.UseCustomTheme;
            CTWidgetBackground.IsEnabled = Settings.UseCustomTheme;
        }
    }
}
