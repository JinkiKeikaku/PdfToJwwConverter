using Microsoft.Win32;
using PdfToJww;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var conv = new PdfConverter();
            int max = conv.GetPageSize(mPdfPath);
            var range = PageRangeParser.ParsePageRange(Part_PageNumber.Text, max);
            foreach(var i in range)
            {
                conv.Convert(mPdfPath, i, GetJwwPath(i), EnableCombineText);
            }
        }


        private string GetJwwPath(int pageNumber)
        {
            var s1 = System.IO.Path.GetDirectoryName(mPdfPath);
            var s2 = System.IO.Path.GetFileNameWithoutExtension(mPdfPath)+$"_{pageNumber}.jww";
            var s = System.IO.Path.Join (s1, s2);
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
    }
}
