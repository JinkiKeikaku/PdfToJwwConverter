using Microsoft.Win32;
using PdfToJww;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PdfToJwwConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string mPdfPath = "";
        bool mConvertCancel = false;
        public MainWindow()
        {
            if (Properties.Settings.Default.IsUpgrade == false)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.IsUpgrade = true;
                Debug.WriteLine(this, "Upgraded");
            }
            InitializeComponent();
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

        public JwwScale Scale {
            get=> new JwwScale(Properties.Settings.Default.JwwScaleValue);
            set => Properties.Settings.Default.JwwScaleValue = value.ScaleNumber;
        }

        public JwwScale[] ScaleList { get; } = new JwwScale[]
        {
            new (0.2),
            new (0.5),
            new (1),
            new (2),
            new (5),
            new (10),
            new (50),
            new (100),
            new (200),
            new (500),
            new (1000),
        };


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            mConvertCancel = false;
            try
            {
                Part_WaitingOverlay.Visibility = Visibility.Visible;
                var conv = new PdfConverter();
                conv.JwwScaleNumber = Scale.ScaleNumber;
                int max = conv.GetPageSize(mPdfPath);
                var range = PageRangeParser.ParsePageRange(Part_PageNumber.Text, max);
                var count = 1;
                var createdCount = 0;
                Part_Cancel.Visibility = Visibility.Visible;
                Part_Progress.Visibility = Visibility.Visible;
                Part_Progress.IsIndeterminate = true;
                foreach (var i in range)
                {
                    var jwwPath = GetJwwPath(i);
                    var isSkip = false;
                    if (!EnableOverwrite && File.Exists(jwwPath))
                    {
                        Part_Progress.IsIndeterminate = false;
                        switch (MessageBox.Show(
                            this, String.Format(Properties.Resources.OverwriteConfirm, jwwPath),
                            Properties.Resources.Confirmation, MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                        {
                            case MessageBoxResult.Yes:
                                break;
                            case MessageBoxResult.No:
                                isSkip = true;
                                break;
                            case MessageBoxResult.Cancel:
                                mConvertCancel = true;
                                break;
                        }
                    }
                    Part_Progress.IsIndeterminate = true;
                    if (mConvertCancel) break;
                    if (!isSkip)
                    {
                        SetMessage(String.Format(Properties.Resources.Converting, count,range.Count,jwwPath), 0);
                        await Task.Run(() =>
                        {
                            conv.Convert(mPdfPath, i, GetJwwPath(i), EnableCombineText, EnableUnifyKanji);
//                            Thread.Sleep(1000);
                        });
                        createdCount++;
                    }
                    count++;
                }
                if (mConvertCancel)
                {
                    SetMessage(Properties.Resources.Canceled, 3000);
                }
                else
                {
                    SetMessage(Properties.Resources.Completed, 3000);
                }
            }
            catch (Exception ex)
            {
                SetMessage($"Error! {ex.Message}", 4000);
                SystemSounds.Beep.Play();
            }
            finally
            {
                Part_WaitingOverlay.Visibility = Visibility.Collapsed;
                Part_Cancel.Visibility=Visibility.Hidden;
                Part_Progress.Visibility = Visibility.Collapsed;
                Part_Progress.IsIndeterminate = false;
                mConvertCancel = false;
            }
        }

        DispatcherTimer mMessageTimer = new();
        void SetMessage(string message, int periodMS)
        {
            mMessageTimer.Stop();
            if (periodMS > 0)
            {
                mMessageTimer.Tick += (s, args) =>
                {
                    Part_Message.Text = "";
                };
                mMessageTimer.Interval = TimeSpan.FromMilliseconds(periodMS);
                mMessageTimer.Start();
            }
            Part_Message.Text = message;
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
            Part_PageRange.Text = $" / {a}";
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

        private void Part_Cancel_Click(object sender, RoutedEventArgs e)
        {
            mConvertCancel = true;
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
