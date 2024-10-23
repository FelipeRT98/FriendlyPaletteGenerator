using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Path = System.IO.Path;

namespace FriendlyPaletteGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string _paletteText = "Palette";
        public string _saveText = "Save";
        public string _loadText = "Load";
        public string _copyText = "Copy";
        public string _graysText = "Grays";
        public string _colorsText = "Colors";
        public string _allText = "All";
        public string _aboutText = "About";
        public string _languageText = "Language";

        public string defaultDirectory = Directory.GetCurrentDirectory();
        public string fileName = "palettes.json";
        public string fullPath;
        public JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true, };
        public string jsonString = "";
        public class Palette
        {
            public int Index { get; set; }
            public string? ColorMode { get; set; }
            public string[]? Values { get; set; }
        }
        public List<Palette> savedPalettes = [];

        public int quantityDropdownValue = 7;
        public string colorModelValue = "RGB";

        public List<(byte R, byte G, byte B)> rgbGrayList = [];
        public List<(byte R, byte G, byte B)> rgbColorList = [];

        public List<int> grayTextRowIndexList = [];
        public List<int> colorTextRowIndexList = [];
        public List<int> colorFillRowIndexList = [];

        public int grayTextBlockPosition = 0;
        public int copyCurrentGrayButtonPosition = 1;
        public int grayFillTextBlockPosition = 2;
        public int colorFillTextBlockPosition = 3;
        public int copyCurrentColorButtonPosition = 4;
        public int colorTextBlockPosition = 5;
        public int resetCurrentColorButtonPosition = 6;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            SetLanguage(CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

            fullPath = Path.Combine(defaultDirectory, fileName);
            if (File.Exists(fullPath))
            {
                jsonString = File.ReadAllText(fullPath);
                savedPalettes = JsonSerializer.Deserialize<List<Palette>>(jsonString) ?? [];
            }

            for (int i = 0; i < savedPalettes.Count; i++)
            {
                MenuItem miS = (MenuItem)FindName($"S{savedPalettes[i].Index}");
                MenuItem miL = (MenuItem)FindName($"L{savedPalettes[i].Index}");

                miS.Background = Brushes.SteelBlue;
                miL.Background = Brushes.SteelBlue;

                miL.IsEnabled = true;
            }

            SetQuantityDropdown();
        }

        private void SetQuantityDropdown()
        {
            for (int i = 3; i <= 20; i++)
            {
                ComboBoxItem comboBoxItem = new() { Content = i };

                if (i == 7) { comboBoxItem.IsSelected = true; }

                quantityDropdown.Items.Add(comboBoxItem);
            }

            quantityDropdownValue = int.TryParse(((ComboBoxItem)quantityDropdown.SelectedItem)?.Content.ToString(), out var result) ? result : 0;
        }

        private void GenerateGrays()
        {
            rgbGrayList = [];

            double step = 255.0 / (quantityDropdownValue - 1);

            for (int index = 0; index < quantityDropdownValue; index++)
            {
                byte grayValue = (byte)Math.Round(index * step);
                (byte R, byte G, byte B) rgbGray = (grayValue, grayValue, grayValue);
                rgbGrayList.Add(rgbGray);
            }
        }

        private void GenerateColors()
        {
            rgbColorList = [];

            for (int index = 0; index < quantityDropdownValue; index++)
            {
                (byte R, byte G, byte B) rgbColor = ReturnColorFromGray(rgbGrayList[index].R);
                rgbColorList.Add(rgbColor);
            }
        }

        private static (byte R, byte G, byte B) ReturnColorFromGray(byte grayValue)
        {
            Random random = new();

            int R = random.Next(0, 256);
            int G = random.Next(0, 256);
            double remainingGray = grayValue - (0.299 * R + 0.587 * G);
            int B = (int)Math.Round(remainingGray / 0.114);
            B = Math.Max(0, Math.Min(255, B));

            int generatedColorGrayValue = (int)Math.Round(0.299 * R + 0.587 * G + 0.114 * B);

            while (generatedColorGrayValue != grayValue)
            {
                R = random.Next(0, 256);
                G = random.Next(0, 256);
                remainingGray = grayValue - (0.299 * R + 0.587 * G);
                B = (int)Math.Round(remainingGray / 0.114);
                B = Math.Max(0, Math.Min(255, B));
                generatedColorGrayValue = (int)Math.Round(0.299 * R + 0.587 * G + 0.114 * B);
            }

            if (grayValue == 0)
            {
                R = G = B = 0;
            }
            else if (grayValue == 255)
            {
                R = G = B = 255;
            }
            var colorRgb = ((byte)R, (byte)G, (byte)B);
            return colorRgb;
        }

        private void ClearRows()
        {
            while (content.RowDefinitions.Count > 1)
            {
                int lastRowIndex = content.RowDefinitions.Count - 1;

                for (int i = content.Children.Count - 1; i >= 0; i--)
                {
                    if (Grid.GetRow(content.Children[i]) == lastRowIndex)
                    {
                        content.Children.RemoveAt(i);
                    }
                }

                content.RowDefinitions.RemoveAt(lastRowIndex);
            }
        }

        private void AddRows()
        {
            grayTextRowIndexList = [];
            colorTextRowIndexList = [];
            colorFillRowIndexList = [];

            for (int index = 0; index < quantityDropdownValue; index++)
            {
                CreateRow(index);
            }
        }

        private void CreateRow(int index)
        {
            content.RowDefinitions.Add(new());

            string grayText = FormatRgbTo(colorModelValue, rgbGrayList[index]);
            string colorText = FormatRgbTo(colorModelValue, rgbColorList[index]);

            VerticalAlignment textVA = VerticalAlignment.Center;

            VerticalAlignment fillBrushVA = VerticalAlignment.Stretch;
            HorizontalAlignment fillBrushHA = HorizontalAlignment.Stretch;

            SolidColorBrush grayFillBrush = new(Color.FromRgb(rgbGrayList[index].R, rgbGrayList[index].G, rgbGrayList[index].B));
            SolidColorBrush colorFillBrush = new(Color.FromRgb(rgbColorList[index].R, rgbColorList[index].G, rgbColorList[index].B));

            TextBlock grayTextBlock = new()
            {
                Text = grayText,
                VerticalAlignment = textVA,
                TextAlignment = TextAlignment.Right,
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(1, 0, 1, 0),
                Margin = new Thickness(1, 0, 1, 0)
            };
            Button copyCurrentGrayButton = new()
            {
                Name = "copyCurrentGrayButton",
                Content = "📋",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock grayFillTextBlock = new()
            {
                Text = "0123456789",
                VerticalAlignment = fillBrushVA,
                HorizontalAlignment = fillBrushHA,
                Background = grayFillBrush,
                Foreground = grayFillBrush
            };
            TextBlock colorFillTextBlock = new()
            {
                Text = "0123456789",
                VerticalAlignment = fillBrushVA,
                HorizontalAlignment = fillBrushHA,
                Background = colorFillBrush,
                Foreground = colorFillBrush
            };
            Button copyCurrentColorButton = new()
            {
                Name = "copyCurrentColorButton",
                Content = "📋",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock colorTextBlock = new()
            {
                Text = colorText,
                VerticalAlignment = textVA,
                TextAlignment = TextAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(1, 0, 1, 0),
                Margin = new Thickness(1, 0, 1, 0)
            };
            Button resetCurrentColorButton = new()
            {
                Name = "resetCurrentColorButton",
                Content = "↺",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (index==0 || index==quantityDropdownValue-1)
            {
                resetCurrentColorButton.Visibility = Visibility.Hidden;
            }

            resetCurrentColorButton.Click += ResetCurrentColorButton_Click;
            copyCurrentGrayButton.Click += CopyCurrentGrayButton_Click;
            copyCurrentColorButton.Click += CopyCurrentColorButton_Click;

            Grid.SetRow(grayTextBlock, index + 1);
            Grid.SetRow(copyCurrentGrayButton, index + 1);
            Grid.SetRow(grayFillTextBlock, index + 1);
            Grid.SetRow(colorFillTextBlock, index + 1);
            Grid.SetRow(copyCurrentColorButton, index + 1);
            Grid.SetRow(colorTextBlock, index + 1);
            Grid.SetRow(resetCurrentColorButton, index + 1);

            Grid.SetColumn(grayTextBlock, grayTextBlockPosition);
            Grid.SetColumn(copyCurrentGrayButton, copyCurrentGrayButtonPosition);
            Grid.SetColumn(grayFillTextBlock, grayFillTextBlockPosition);
            Grid.SetColumn(colorFillTextBlock, colorFillTextBlockPosition);
            Grid.SetColumn(copyCurrentColorButton, copyCurrentColorButtonPosition);
            Grid.SetColumn(colorTextBlock, colorTextBlockPosition);
            Grid.SetColumn(resetCurrentColorButton, resetCurrentColorButtonPosition);

            grayTextRowIndexList.Add(content.Children.Count);
            content.Children.Add(grayTextBlock);

            content.Children.Add(copyCurrentGrayButton);
            content.Children.Add(grayFillTextBlock);

            colorFillRowIndexList.Add(content.Children.Count);
            content.Children.Add(colorFillTextBlock);

            content.Children.Add(copyCurrentColorButton);

            colorTextRowIndexList.Add(content.Children.Count);
            content.Children.Add(colorTextBlock);

            content.Children.Add(resetCurrentColorButton);
        }

        private void ResetCurrentColorButton_Click(object sender, RoutedEventArgs e)
        {
            int row = Grid.GetRow((UIElement)sender);
            byte grayValue = rgbGrayList[row - 1].R;
            (byte R, byte G, byte B) newRgbColor = ReturnColorFromGray(grayValue);
            rgbColorList[row - 1] = newRgbColor;

            TextBlock ct = (TextBlock)content.Children[colorTextRowIndexList[row - 1]];
            TextBlock cf = (TextBlock)content.Children[colorFillRowIndexList[row - 1]];

            SolidColorBrush colorFillBrush = new(Color.FromRgb(newRgbColor.R, newRgbColor.G, newRgbColor.B));
            cf.Background = colorFillBrush;
            cf.Foreground = colorFillBrush;

            ct.Text = FormatRgbTo(colorModelValue, newRgbColor);
        }

        private void ResetAllColorsButton_Click(object sender, RoutedEventArgs e)
        {
            ClearRows();

            GenerateGrays();

            GenerateColors();

            AddRows();
        }

        private void CopyCurrentGrayButton_Click(object sender, RoutedEventArgs e)
        {
            int row = Grid.GetRow((UIElement)sender);
            TextBlock tb = (TextBlock)content.Children[grayTextRowIndexList[row - 1]];
            Clipboard.SetText(tb.Text);
        }

        private void CopyCurrentColorButton_Click(object sender, RoutedEventArgs e)
        {
            int row = Grid.GetRow((UIElement)sender);
            TextBlock tb = (TextBlock)content.Children[colorTextRowIndexList[row - 1]];
            Clipboard.SetText(tb.Text);
        }

        private void CopyAllGraysButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(CopyGrays());
        }

        private void CopyAllColorsButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(CopyColors());
        }

        private void QuantityDropdown_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                quantityDropdown.IsDropDownOpen = true;
                e.Handled = true;
            }
        }

        private void QuantityDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            quantityDropdownValue = int.TryParse(((ComboBoxItem)quantityDropdown.SelectedItem)?.Content.ToString(), out var result) ? result : 0;

            ClearRows();

            GenerateGrays();

            GenerateColors();

            AddRows();
        }

        private void ColorModelDropdown_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                colorModelDropdown.IsDropDownOpen = true;
                e.Handled = true;
            }
        }

        private void ColorModelDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (colorModelDropdown.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Content is string selectedValue)
                {
                    colorModelValue = selectedValue.ToUpperInvariant() switch
                    {
                        "RGB" => "RGB",
                        "HEX" => "HEX",
                        "HSL" => "HSL",
                        "HSV" => "HSV",
                        "CMYK" => "CMYK",
                        _ => "RGB"
                    };

                    for (int i = 0; i < quantityDropdownValue; i++)
                    {
                        TextBlock gtb = (TextBlock)content.Children[grayTextRowIndexList[i]];
                        TextBlock ctb = (TextBlock)content.Children[colorTextRowIndexList[i]];

                        gtb.Text = FormatRgbTo(colorModelValue, rgbGrayList[i]);
                        ctb.Text = FormatRgbTo(colorModelValue, rgbColorList[i]);
                    }
                }
            }
        }

        private static string FormatRgbTo(string colorModel, (byte R, byte G, byte B) rgb)
        {
            string formattedRgb = colorModel switch
            {
                "RGB" => $"{rgb.R}, {rgb.G}, {rgb.B}",
                "HEX" => string.Format("#{0:X2}{1:X2}{2:X2}", rgb.R, rgb.G, rgb.B),
                "HSL" => GetHSL(rgb),
                "HSV" => GetHSV(rgb),
                "CMYK" => GetCMYK(rgb),
                _ => $"{rgb.R}, {rgb.G}, {rgb.B}",
            };
            return formattedRgb;
        }

        private static string GetHSL((byte R, byte G, byte B) rgb)
        {
            double rNorm = rgb.R / 255.0;
            double gNorm = rgb.G / 255.0;
            double bNorm = rgb.B / 255.0;

            double min = Math.Min(Math.Min(rNorm, gNorm), bNorm);
            double max = Math.Max(Math.Max(rNorm, gNorm), bNorm);
            double delta = max - min;

            double l = (max + min) / 2.0;

            double h = 0.0;
            double s = 0.0;

            if (delta != 0)
            {
                s = l < 0.5 ? delta / (max + min) : delta / (2.0 - max - min);

                if (max == rNorm)
                {
                    h = ((gNorm - bNorm) / delta) + (gNorm < bNorm ? 6.0 : 0.0);
                }
                else if (max == gNorm)
                {
                    h = ((bNorm - rNorm) / delta) + 2.0;
                }
                else if (max == bNorm)
                {
                    h = ((rNorm - gNorm) / delta) + 4.0;
                }

                h *= 60.0;
            }

            h = Math.Round(h, 0);
            s = Math.Round(s * 100.0, 0);
            l = Math.Round(l * 100.0, 0);
            return $"{h}°, {s}%, {l}%";
        }

        private static string GetHSV((byte R, byte G, byte B) rgb)
        {
            double rNorm = rgb.R / 255.0;
            double gNorm = rgb.G / 255.0;
            double bNorm = rgb.B / 255.0;

            double min = Math.Min(Math.Min(rNorm, gNorm), bNorm);
            double max = Math.Max(Math.Max(rNorm, gNorm), bNorm);
            double delta = max - min;

            double v = max;

            double h = 0.0;
            double s = 0.0;

            if (max != 0)
            {
                s = delta / max;

                if (delta != 0)
                {
                    if (max == rNorm)
                    {
                        h = ((gNorm - bNorm) / delta) % 6;
                    }
                    else if (max == gNorm)
                    {
                        h = ((bNorm - rNorm) / delta) + 2;
                    }
                    else if (max == bNorm)
                    {
                        h = ((rNorm - gNorm) / delta) + 4;
                    }

                    h *= 60;
                    if (h < 0) h += 360;
                }
            }

            h = Math.Round(h, 0);
            s = Math.Round(s * 100.0, 0);
            v = Math.Round(v * 100.0, 0);

            return $"{h}°, {s}%, {v}%";
        }

        private static string GetCMYK((byte R, byte G, byte B) rgb)
        {
            double rNorm = rgb.R / 255.0;
            double gNorm = rgb.G / 255.0;
            double bNorm = rgb.B / 255.0;

            double k = 1 - Math.Max(Math.Max(rNorm, gNorm), bNorm);

            double c, m, y;

            if (k < 1)
            {
                c = (1 - rNorm - k) / (1 - k);
                m = (1 - gNorm - k) / (1 - k);
                y = (1 - bNorm - k) / (1 - k);
            }
            else
            {
                c = 0;
                m = 0;
                y = 0;
            }

            c = Math.Round(c * 100.0, 0);
            m = Math.Round(m * 100.0, 0);
            y = Math.Round(y * 100.0, 0);
            k = Math.Round(k * 100.0, 0);

            return $"{c}%, {m}%, {y}%, {k}%";
        }

        private void AltMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            string? text = mi.Name.ToString();

            MenuItem parentMI = (MenuItem)mi.Parent;
            string? parentText = parentMI.Name.ToString();

            string actionName = parentText?.Replace("_", string.Empty) +
                                text?.Replace("_", string.Empty);

            switch (actionName)
            {
                case "CopyGrays":
                    Clipboard.SetText(CopyGrays());
                    break;
                case "CopyColors":
                    Clipboard.SetText(CopyColors());
                    break;
                case "CopyAll":
                    Clipboard.SetText(CopyAll());
                    break;
                default:
                    if (actionName.Contains("Save"))
                    {
                        SavePalette(int.Parse(actionName.Last().ToString()));
                    }
                    else if (actionName.Contains("Load"))
                    {
                        LoadPalette(int.Parse(actionName.Last().ToString()));
                    }
                    else
                    {
                        DisplayAbout();
                    }
                    break;
            }
        }

        private static void DisplayAbout()
        {
            string messageBoxText =
                "Copyright (C) 2024 Felipe R.T.\r\n\r\n" +
                "This program is free software: you can redistribute it and/or modify it\r\n" +
                "under the terms of the GNU General Public License as published by\r\n" +
                "the Free Software Foundation, either version 3 of the License, or\r\n" +
                "(at your option) any later version.\r\n\r\n" +
                "This program is distributed in the hope that it will be useful,\r\n" +
                "but WITHOUT ANY WARRANTY; without even the implied warranty of\r\n" +
                "MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.\r\n" +
                "See the GNU General Public License for more details.\r\n\r\n" +
                "You should have received a copy of the GNU General Public License along with this program. " +
                "If not, see <https://www.gnu.org/licenses/>." +
                "\r\n\r\n" +
                "==========" +
                "\r\n\r\n" +
                "https://github.com/FelipeRT98/FriendlyPaletteGenerator";
            string caption = "FPG";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None);
        }
        private void SavePalette(int slot)
        {
            if (!File.Exists(fullPath))
            {
                FileStream myStream = new(fullPath, FileMode.Create, FileAccess.Write);
                myStream.Close();

                WriteIntoFile(slot, fullPath);
            }
            else
            {
                WriteIntoFile(slot, fullPath);
            }
        }

        private void WriteIntoFile(int slot, string fullPath)
        {
            string[] colors = rgbColorList
                .Select(color => $"{color.R},{color.G},{color.B}")
                .ToArray();

            var existingPalette = savedPalettes.FirstOrDefault(p => p.Index == slot);
            if (existingPalette != null)
            {
                existingPalette.ColorMode = colorModelValue;
                existingPalette.Values = colors;
            }
            else
            {
                savedPalettes.Add(new Palette
                {
                    Index = slot,
                    ColorMode = colorModelValue,
                    Values = colors
                });
            }

            jsonString = JsonSerializer.Serialize(savedPalettes, jsonSerializerOptions);

            File.WriteAllText(fullPath, jsonString);

            MenuItem miS = (MenuItem)FindName($"S{slot}");
            MenuItem miL = (MenuItem)FindName($"L{slot}");

            miS.Background = Brushes.SteelBlue;
            miL.Background = Brushes.SteelBlue;

            miL.IsEnabled = true;
        }

        private void LoadPalette(int slot)
        {
            Palette? paletteToLoad = savedPalettes.FirstOrDefault(p => p.Index == slot);

            if (string.IsNullOrEmpty(paletteToLoad?.ColorMode))
            {
                return;
            }

            quantityDropdownValue = paletteToLoad?.Values?.Length ?? 7;
            quantityDropdown.SelectedItem = quantityDropdown.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => item.Content.ToString() == quantityDropdownValue.ToString());

            colorModelValue = paletteToLoad?.ColorMode ?? "RGB";
            colorModelDropdown.SelectedValue = colorModelDropdown.SelectedItem = colorModelDropdown.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => item.Content.ToString() == colorModelValue);

            ClearRows();

            GenerateGrays();

            rgbColorList = [];

            for (int index = 0; index < quantityDropdownValue; index++)
            {
                string[]? rgbParts = paletteToLoad?.Values?[index]?.Split(',');

                (byte R, byte G, byte B) rgbColor = (
                    byte.Parse(rgbParts?[0] ?? "0"),
                    byte.Parse(rgbParts?[1] ?? "0"),
                    byte.Parse(rgbParts?[2] ?? "0")
                );

                rgbColorList.Add(rgbColor);
            }

            AddRows();
        }

        private string CopyGrays()
        {
            string dataToCopy = "";
            for (int i = 0; i < quantityDropdownValue; i++)
            {
                TextBlock tb = (TextBlock)content.Children[grayTextRowIndexList[i]];
                dataToCopy += tb.Text + System.Environment.NewLine;
            }
            return dataToCopy;
        }

        private string CopyColors()
        {
            string dataToCopy = "";
            for (int i = 0; i < quantityDropdownValue; i++)
            {
                TextBlock tb = (TextBlock)content.Children[colorTextRowIndexList[i]];
                dataToCopy += tb.Text + System.Environment.NewLine;
            }
            return dataToCopy;
        }

        private string CopyAll()
        {
            string input = CopyGrays() + CopyColors();

            var lines = input.Split(System.Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            var uniqueLines = new HashSet<string>(lines);

            return string.Join(System.Environment.NewLine, uniqueLines);
        }

        public string PaletteText
        {
            get => _paletteText;
            set { _paletteText = value; OnPropertyChanged(nameof(PaletteText)); }
        }

        public string SaveText
        {
            get => _saveText;
            set { _saveText = value; OnPropertyChanged(nameof(SaveText)); }
        }

        public string LoadText
        {
            get => _loadText;
            set { _loadText = value; OnPropertyChanged(nameof(LoadText)); }
        }

        public string CopyText
        {
            get => _copyText;
            set { _copyText = value; OnPropertyChanged(nameof(CopyText)); }
        }

        public string GraysText
        {
            get => _graysText;
            set { _graysText = value; OnPropertyChanged(nameof(GraysText)); }
        }

        public string ColorsText
        {
            get => _colorsText;
            set { _colorsText = value; OnPropertyChanged(nameof(ColorsText)); }
        }

        public string AllText
        {
            get => _allText;
            set { _allText = value; OnPropertyChanged(nameof(AllText)); }
        }

        public string AboutText
        {
            get => _aboutText;
            set { _aboutText = value; OnPropertyChanged(nameof(AboutText)); }
        }

        public string LanguageText
        {
            get => _languageText;
            set { _languageText = value; OnPropertyChanged(nameof(LanguageText)); }
        }

        private void SetLanguage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                string? languageCode = menuItem.Tag.ToString();
                SetLanguage(languageCode ?? "en");
            }
        }
        public void SetLanguage(string languageCode)
        {
            switch (languageCode)
            {
                case "en":
                    PaletteText = "_Palette";
                    SaveText = "_Save";
                    LoadText = "_Load";
                    CopyText = "_Copy";
                    GraysText = "_Grays";
                    ColorsText = "_Colors";
                    AllText = "_All";
                    AboutText = "_About";
                    LanguageText = "_Language";
                    break;
                case "es":
                    PaletteText = "_Paleta";
                    SaveText = "_Guardar";
                    LoadText = "_Cargar";
                    CopyText = "_Copiar";
                    GraysText = "_Grises";
                    ColorsText = "_Colores";
                    AllText = "_Todos";
                    AboutText = "_Acerca de";
                    LanguageText = "_Idioma";
                    break;
                case "de":
                    PaletteText = "_Palette";
                    SaveText = "_Speichern";
                    LoadText = "_Laden";
                    CopyText = "_Kopieren";
                    GraysText = "_Grautöne";
                    ColorsText = "_Farben";
                    AllText = "_Alle";
                    AboutText = "Ü_ber";
                    LanguageText = "_Sprache";
                    break;
                case "pt":
                    PaletteText = "_Paleta";
                    SaveText = "_Salvar";
                    LoadText = "_Carregar";
                    CopyText = "_Copiar";
                    GraysText = "Cin_zas";
                    ColorsText = "Co_res";
                    AllText = "_Tudo";
                    AboutText = "_Sobre";
                    LanguageText = "_Idioma";
                    break;
                case "fr":
                    PaletteText = "_Palette";
                    SaveText = "_Enregistrer";
                    LoadText = "_Charger";
                    CopyText = "_Copier";
                    GraysText = "_Gris";
                    ColorsText = "_Couleurs";
                    AllText = "_Tout";
                    AboutText = "À pr_opos";
                    LanguageText = "_Langue";
                    break;
                case "it":
                    PaletteText = "_Tavolozza";
                    SaveText = "_Salva";
                    LoadText = "_Carica";
                    CopyText = "_Copia";
                    GraysText = "_Grigi";
                    ColorsText = "_Colori";
                    AllText = "_Tutto";
                    AboutText = "_Informazioni";
                    LanguageText = "_Lingua";
                    break;
                case "ja":
                    PaletteText = "パレット";
                    SaveText = "保存";
                    LoadText = "読み込む";
                    CopyText = "コピー";
                    GraysText = "グレー";
                    ColorsText = "色";
                    AllText = "全て";
                    AboutText = "情報";
                    LanguageText = "言語";
                    break;
                case "ko":
                    PaletteText = "팔레트";
                    SaveText = "저장";
                    LoadText = "불러오기";
                    CopyText = "복사";
                    GraysText = "회색";
                    ColorsText = "색상";
                    AllText = "모두";
                    AboutText = "정보";
                    LanguageText = "언어";
                    break;
                case "zh":
                    PaletteText = "调色板";
                    SaveText = "保存";
                    LoadText = "加载";
                    CopyText = "复制";
                    GraysText = "灰色";
                    ColorsText = "颜色";
                    AllText = "全部";
                    AboutText = "关于";
                    LanguageText = "语言";
                    break;
                case "hi":
                    PaletteText = "पैलेट";
                    SaveText = "सहेजें";
                    LoadText = "लोड करें";
                    CopyText = "कॉपी करें";
                    GraysText = "धूसर";
                    ColorsText = "रंग";
                    AllText = "सभी";
                    AboutText = "के बारे में";
                    LanguageText = "भाषा";
                    break;
                case "ru":
                    PaletteText = "Палитра";
                    SaveText = "Сохранить";
                    LoadText = "Загрузить";
                    CopyText = "Копировать";
                    GraysText = "Оттенки серого";
                    ColorsText = "Цвета";
                    AllText = "Все";
                    AboutText = "О программе";
                    LanguageText = "Язык";
                    break;
                default:
                    PaletteText = "_Palette";
                    SaveText = "_Save";
                    LoadText = "_Load";
                    CopyText = "_Copy";
                    GraysText = "_Grays";
                    ColorsText = "_Colors";
                    AllText = "_All";
                    AboutText = "_About";
                    LanguageText = "_Language";
                    break;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            DisplayAbout();
        }
    }
}
