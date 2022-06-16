using Microsoft.Win32;
using PdfToJww;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;

namespace PdfToJwwConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string mPdfPath = "";

        public MainWindow()
        {
            if (Properties.Settings.Default.IsUpgrade == false)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.IsUpgrade = true;
                Debug.WriteLine(this, "Upgraded");
            }
            InitializeComponent();
            Part_Convert.IsEnabled = false;
            DataContext = this;
        }

        public bool EnableCombineText
        {
            get => Properties.Settings.Default.EnableCombineText;
            set => Properties.Settings.Default.EnableCombineText = value;
        }

        public bool EnableUnifyKanji
        {
            get => Properties.Settings.Default.EnableUnifyKanji;
            set => Properties.Settings.Default.EnableUnifyKanji = value;
        }

        public bool EnableOverwrite
        {
            get => Properties.Settings.Default.EnableOverwrite;
            set => Properties.Settings.Default.EnableOverwrite = value;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var conv = new PdfConverter();
            try
            {
                Part_Convert.IsEnabled = false;
                int max = conv.GetPageSize(mPdfPath);
                var range = PageRangeParser.ParsePageRange(Part_PageNumber.Text, max);

                var sb = new StringBuilder();
                if (range.Count == 0)
                {
                    sb.AppendLine("Converted no file.");
                }
                var count = 0;
                foreach (var i in range)
                {
                    var jwwPath = GetJwwPath(i);
                    var isSkip = false;
                    if (!EnableOverwrite && File.Exists(jwwPath))
                    {
                        if (MessageBox.Show(this, $"{jwwPath} is already exist.\nOverwrite?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        {
                            sb.AppendLine($"Skipped {jwwPath}");
                            isSkip = true;
                        }
                    }
                    if (!isSkip)
                    {
                        conv.Convert(mPdfPath, i, GetJwwPath(i), EnableCombineText, EnableUnifyKanji);
                        sb.AppendLine($"Converted {jwwPath}");
                        count++;
                    }
                }
                sb.AppendLine($"total {count} file was converted.");
                MessageBox.Show(this, sb.ToString(), "Result");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error");
            }
            finally
            {
                Part_Convert.IsEnabled = true;
            }
        }


        private string GetJwwPath(int pageNumber)
        {
            var s1 = System.IO.Path.GetDirectoryName(mPdfPath);
            var s2 = System.IO.Path.GetFileNameWithoutExtension(mPdfPath) + $"_{pageNumber}.jww";
            var s = System.IO.Path.Join(s1, s2);
            return s;
        }

        private void SetPdfPath(string pdfPath)
        {
            mPdfPath = pdfPath;
            Part_PdfFile.Text = pdfPath;
            var conv = new PdfConverter();
            var a = conv.GetPageSize(pdfPath);
            Part_PageRange.Text = $"Max {a}";
            Part_Convert.IsEnabled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("Window_Drop");
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
                {
                    SetPdfPath(files[0]);
                }
            }
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            Debug.WriteLine("Window_PreviewDragOver");
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                e.Effects = System.Windows.DragDropEffects.None;
                if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
                {
                    // TODO Check acceptable.
                    e.Effects = System.Windows.DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }
        }

        private void Part_OpenPDF_Click(object sender, RoutedEventArgs e)
        {
            var f = new OpenFileDialog
            {
                FileName = "",
                FilterIndex = 1,
                Filter = "PDF file(.pdf)|*.pdf|All files (*.*)|*.*",
            };
            if (f.ShowDialog(Application.Current.MainWindow) == true)
            {
                SetPdfPath(f.FileName);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowBounds();
            Properties.Settings.Default.Save();
        }

        private void SaveWindowBounds()
        {
            var settings = Properties.Settings.Default;
            WindowState = WindowState.Normal; // 最大化解除
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
        }
        private void RecoverWindowBounds()
        {
            var settings = Properties.Settings.Default;
            // 左
            if (settings.WindowLeft >= 0 &&
                (settings.WindowLeft + ActualWidth) < SystemParameters.VirtualScreenWidth) { Left = settings.WindowLeft; }
            // 上
            if (settings.WindowTop >= 0 &&
                (settings.WindowTop + ActualHeight) < SystemParameters.VirtualScreenHeight) { Top = settings.WindowTop; }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RecoverWindowBounds();
        }
    }
}
