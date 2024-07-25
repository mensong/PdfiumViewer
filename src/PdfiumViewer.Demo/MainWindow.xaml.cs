using Microsoft.Win32;
using PdfiumViewer.Core;
using PdfiumViewer.Demo.Annotations;
using PdfiumViewer.Drawing;
using PdfiumViewer.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace PdfiumViewer.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private List<ViewConfig> viewsConfig = new List<ViewConfig>();

        private DispatcherTimer dispatcherTimer = null;

        private bool passivePropertyChanged = false;
        public MainWindow()
        {
            InitializeComponent();

            //MuPDFCore.MuPDFContext ctx = new MuPDFCore.MuPDFContext();
            //MuPDFCore.MuPDFDocument doc1 = new MuPDFCore.MuPDFDocument(ctx, 
            //    @"D:\Working\PDFUCK\x64\Debug\a1.pdf");
            //var textBlocks = doc1.GetStructuredTextPage(0).StructuredTextBlocks;
            //string text = "";
            //for (var i = 0; i < textBlocks.Length; ++i)
            //{
            //    var block = textBlocks[i];

            //    for (var j = 0; j < block.Count; ++j)
            //    {
            //        string s = block[j].Text;
                    
            //        text += s;
            //    }
            //}

            var version = GetType().Assembly.GetName().Version.ToString(3);
            Title = $"MPLM Tools PDF Viewer v{version}";
            CurrentProcess = Process.GetCurrentProcess();
            Cts = new CancellationTokenSource();
            DataContext = this;
            Renderer1.PropertyChanged += delegate
            {
                if (!passivePropertyChanged)
                {
                    passivePropertyChanged = true;

                    OnPropertyChanged(nameof(Page));
                    OnPropertyChanged(nameof(ZoomPercent));

                    //Renderer2.GotoPage(Renderer1.PageNo);
                    //Renderer2.SetZoom(Renderer1.Zoom);
                    //Renderer2.ScrollToHorizontalOffset(Renderer1.HorizontalOffset);
                    //Renderer2.ScrollToVerticalOffset(Renderer1.VerticalOffset);

                    passivePropertyChanged = false;
                }
                
            };
            Renderer2.PropertyChanged += delegate
            {
                if (!passivePropertyChanged)
                {
                    passivePropertyChanged = true;

                    OnPropertyChanged(nameof(Page));
                    OnPropertyChanged(nameof(ZoomPercent));

                    //Renderer1.GotoPage(Renderer2.PageNo);
                    //Renderer1.SetZoom(Renderer2.Zoom);
                    //Renderer1.ScrollToHorizontalOffset(Renderer2.HorizontalOffset);
                    //Renderer1.ScrollToVerticalOffset(Renderer2.VerticalOffset);

                    passivePropertyChanged = false;
                }
            };

            Renderer1.PageChanged += Renderer_PageChanged;
            Renderer2.PageChanged += Renderer_PageChanged;
            Renderer1.ScrollChanged += Renderer_ScrollChanged;
            Renderer2.ScrollChanged += Renderer_ScrollChanged;
            Renderer1.ZoomChanged += Renderer_ZoomChanged;
            Renderer2.ZoomChanged += Renderer_ZoomChanged;

            MemoryChecker = new System.Windows.Threading.DispatcherTimer();
            MemoryChecker.Tick += OnMemoryChecker;
            MemoryChecker.Interval = new TimeSpan(0, 0, 1);
            MemoryChecker.Start();

            //SearchManager = new PdfSearchManager(Renderer1);
            //MatchCaseCheckBox.IsChecked = SearchManager.MatchCase;
            //WholeWordOnlyCheckBox.IsChecked = SearchManager.MatchWholeWord;
            //HighlightAllMatchesCheckBox.IsChecked = SearchManager.HighlightAllMatches;

            //Renderer1.EnableKinetic = true;
            //Renderer2.EnableKinetic = true;
            //RendererMerge.EnableKinetic = true;

            var args = Environment.GetCommandLineArgs();
            if (args != null && args.Length >= 2)
            {
                try
                {
                    //var basePath = Path.GetDirectoryName(
                    //    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    string viewsConfigStr = File.ReadAllText(args[1]);
                    viewsConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ViewConfig>>(viewsConfigStr);
                    foreach (var config in viewsConfig)
                    {
                        cmbViewType.Items.Add(config.name);
                    }

                    if (viewsConfig != null && viewsConfig.Count > 0)
                    {
                        dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                        dispatcherTimer.Tick += new EventHandler(OnTimedStartUpOpen);
                        dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                        dispatcherTimer.Start();
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        private int GetConfigIndexByName(string name)
        {
            for (int i = 0; i < viewsConfig.Count; i++)
            {
                if (viewsConfig[i].name == name)
                    return i;
            }
            return -1;
        }

        private void SwitchToConfig(int configIdx)
        {
            if (configIdx < 0 || configIdx >= viewsConfig.Count)
                return;
            var config = viewsConfig[configIdx];
            cmbViewType.SelectedIndex = configIdx;

            Dispatcher.Invoke(() => {

                if (config.mode.Equals("leftright", StringComparison.OrdinalIgnoreCase))
                {
                    isDiffLeftRight = true;

                    //OpenPDF1.Visibility = Visibility.Visible;
                    //OpenPDF2.Visibility = Visibility.Visible;
                    //OpenPDFMerge.Visibility = Visibility.Collapsed;
                    gridLeftRight.Visibility = Visibility.Visible;
                    RendererMerge.Visibility = Visibility.Collapsed;

                    if (dispatcherTimer != null)
                        dispatcherTimer.Stop();
                    dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                    dispatcherTimer.Tick += (object? sender, EventArgs e) =>
                    {
                        dispatcherTimer.Stop();

                        if (!string.IsNullOrEmpty(config.pdf1))
                        {
                            var bytes = File.ReadAllBytes(config.pdf1);
                            var mem = new MemoryStream(bytes);
                            Renderer1.OpenPdf(mem);
                            Renderer1.SetZoomMode(PdfViewerZoomMode.FitWidth);
                        }
                        if (!string.IsNullOrEmpty(config.pdf2))
                        {
                            var bytes = File.ReadAllBytes(config.pdf2);
                            var mem = new MemoryStream(bytes);
                            Renderer2.OpenPdf(mem);
                            Renderer2.SetZoomMode(PdfViewerZoomMode.FitWidth);
                        }
                    };
                    dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1);
                    dispatcherTimer.Start();
                }
                else
                {
                    isDiffLeftRight = false;

                    //OpenPDF1.Visibility = Visibility.Collapsed;
                    //OpenPDF2.Visibility = Visibility.Collapsed;
                    //OpenPDFMerge.Visibility = Visibility.Visible;
                    gridLeftRight.Visibility = Visibility.Collapsed;
                    RendererMerge.Visibility = Visibility.Visible;

                    if (dispatcherTimer != null)
                        dispatcherTimer.Stop();
                    dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                    dispatcherTimer.Tick += (object? sender, EventArgs e) =>
                    {
                        dispatcherTimer.Stop();

                        if (!string.IsNullOrEmpty(config.pdf1))
                        {
                            var bytes = File.ReadAllBytes(config.pdf1);
                            var mem = new MemoryStream(bytes);
                            RendererMerge.OpenPdf(mem);
                            RendererMerge.SetZoomMode(PdfViewerZoomMode.FitWidth);
                        }
                    };
                    dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1);
                    dispatcherTimer.Start();
                }

            });
        }

        private void OnTimedStartUpOpen(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();

            SwitchToConfig(0);
        }

        private bool passiveZoomChanged = false;
        private void Renderer_ZoomChanged(object sender, double e)
        {
            if (sender == Renderer1)
            {
                Dispatcher.Invoke(() =>
                {
                    if (!passiveZoomChanged)
                    {
                        passiveZoomChanged = true;
                        Renderer2.SetZoom(e);
                        passiveZoomChanged = false;
                    }
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    if (!passiveZoomChanged)
                    {
                        passiveZoomChanged = true;
                        Renderer1.SetZoom(e);
                        passiveZoomChanged = false;
                    }
                });
            }
        }

        //private bool passiveMouseWheel = false;
        //private void Renderer_MouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    if (sender == Renderer1)
        //    {
        //        Dispatcher.Invoke(() =>
        //        {
        //            if (!passiveScrollChanged)
        //            {
        //                passiveScrollChanged = true;
        //                Renderer2.RaiseEvent(e);
        //                passiveScrollChanged = false;
        //            }
        //        });
        //    }
        //    else
        //    {
        //        Dispatcher.Invoke(() =>
        //        {
        //            if (!passiveScrollChanged)
        //            {
        //                passiveScrollChanged = true;
        //                Renderer1.RaiseEvent(e);
        //                passiveScrollChanged = false;
        //            }
        //        });
        //    }

        //}

        private bool passiveScrollChanged = false;
        private void Renderer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (sender == Renderer1)
            {
                Dispatcher.Invoke(() => 
                {
                    if (!passiveScrollChanged)
                    {
                        passiveScrollChanged = true;
                        Renderer2.ScrollToHorizontalOffset(e.HorizontalOffset);
                        Renderer2.ScrollToVerticalOffset(e.VerticalOffset);
                        passiveScrollChanged = false;
                    }
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    if (!passiveScrollChanged)
                    {
                        passiveScrollChanged = true;
                        Renderer1.ScrollToHorizontalOffset(e.HorizontalOffset);
                        Renderer1.ScrollToVerticalOffset(e.VerticalOffset);
                        passiveScrollChanged = false;
                    }
                });                
            }
        }

        private bool passivePageChanged = false;
        private void Renderer_PageChanged(object sender, int e)
        {
            if (sender == Renderer1)
            {
                Dispatcher.Invoke(() =>
                {
                    if (!passivePageChanged)
                    {
                        passivePageChanged = true;
                        Renderer2.GotoPage(e);
                        passivePageChanged = false;
                    }
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    if (!passivePageChanged)
                    {
                        passivePageChanged = true;
                        Renderer1.GotoPage(e);
                        passivePageChanged = false;
                    }
                });
            }
        }

        private Process CurrentProcess { get; }
        private CancellationTokenSource Cts { get; }
        private System.Windows.Threading.DispatcherTimer MemoryChecker { get; }
        private PdfSearchManager SearchManager { get; }
        public string InfoText { get; set; }
        public string SearchTerm { get; set; }
        public PdfBookmarkCollection Bookmarks { get; set; }
        public bool ShowBookmarks { get; set; }
        public PdfBookmark SelectedBookIndex { get; set; }
        public double ZoomPercent
        {
            get => MainRender.Zoom * 100;
            set 
            {
                if (isDiffLeftRight)
                {
                    Renderer1.SetZoom(value / 100);
                    Renderer2.SetZoom(value / 100);
                }
                else
                    RendererMerge.SetZoom(value / 100);
            }
        }
        public bool IsSearchOpen { get; set; }
        public int SearchMatchItemNo { get; set; }
        public int SearchMatchesCount { get; set; }
        public int Page
        {
            get => MainRender.PageNo + 1;
            set 
            {
                if (isDiffLeftRight)
                {
                    Renderer1.GotoPage(Math.Min(Math.Max(value - 1, 0), Renderer1.PageCount - 1));
                    Renderer2.GotoPage(Math.Min(Math.Max(value - 1, 0), Renderer2.PageCount - 1));
                }
                else
                    MainRender.GotoPage(Math.Min(Math.Max(value - 1, 0), MainRender.PageCount - 1));
            }
        }
        public FlowDirection IsRtl
        {
            get => MainRender.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            set 
            {
                Renderer1.IsRightToLeft = value == FlowDirection.RightToLeft ? true : false; 
                Renderer2.IsRightToLeft = value == FlowDirection.RightToLeft ? true : false;
                MainRender.IsRightToLeft = value == FlowDirection.RightToLeft ? true : false;
            }
        }


        private void OnMemoryChecker(object sender, EventArgs e)
        {
            CurrentProcess.Refresh();
            InfoText = $"Memory: {CurrentProcess.PrivateMemorySize64 / 1024 / 1024} MB";
            OnPropertyChanged(nameof(InfoText));
        }


        private async void RenderToMemory(object sender, RoutedEventArgs e)
        {
            try
            {
                {
                    var pageStep = Renderer1.PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode ? 2 : 1;
                    Dispatcher.Invoke(() => Renderer1.GotoPage(0));
                    while (Renderer1.PageNo < Renderer1.PageCount - pageStep)
                    {
                        Dispatcher.Invoke(() => Renderer1.NextPage());
                        await Task.Delay(1);
                    }
                }
                {
                    var pageStep = Renderer2.PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode ? 2 : 1;
                    Dispatcher.Invoke(() => Renderer2.GotoPage(0));
                    while (Renderer2.PageNo < Renderer2.PageCount - pageStep)
                    {
                        Dispatcher.Invoke(() => Renderer2.NextPage());
                        await Task.Delay(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Cts.Cancel();
                Debug.Fail(ex.Message);
                MessageBox.Show(this, ex.Message, "Error!");
            }
        }

        private void OpenPdf(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                Title = "Open PDF File"
            };

            if (dialog.ShowDialog() == true)
            {
                var bytes = File.ReadAllBytes(dialog.FileName);
                var mem = new MemoryStream(bytes);
                Renderer1.OpenPdf(mem);
            }
        }

        private void OpenPdf2(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                Title = "Open PDF File"
            };

            if (dialog.ShowDialog() == true)
            {
                var bytes = File.ReadAllBytes(dialog.FileName);
                var mem = new MemoryStream(bytes);
                Renderer2.OpenPdf(mem);
            }
        }

        private void OpenPdfMerge(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                Title = "Open PDF File"
            };

            if (dialog.ShowDialog() == true)
            {
                var bytes = File.ReadAllBytes(dialog.FileName);
                var mem = new MemoryStream(bytes);
                RendererMerge.OpenPdf(mem);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            MemoryChecker?.Stop();
            Renderer1?.Dispose();
            Renderer2?.Dispose();
            RendererMerge?.Dispose();
        }
        private void OnPrevPageClick(object sender, RoutedEventArgs e)
        {
            if (isDiffLeftRight)
            {
                Renderer1.PreviousPage();
                Renderer2.PreviousPage();
            }
            else
                RendererMerge.PreviousPage();
        }
        private void OnNextPageClick(object sender, RoutedEventArgs e)
        {
            if (isDiffLeftRight)
            {
                Renderer1.NextPage();
                Renderer2.NextPage();
            }
            else
                RendererMerge.NextPage();
        }
        private void OnFitWidth(object sender, RoutedEventArgs e)
        {
            if (isDiffLeftRight)
            {
                Renderer1.SetZoomMode(PdfViewerZoomMode.FitWidth);
                Renderer2.SetZoomMode(PdfViewerZoomMode.FitWidth);
            }
            else
                RendererMerge.SetZoomMode(PdfViewerZoomMode.FitWidth);
        }
        private void OnFitHeight(object sender, RoutedEventArgs e)
        {
            if (isDiffLeftRight)
            {
                Renderer1.SetZoomMode(PdfViewerZoomMode.FitHeight);
                Renderer2.SetZoomMode(PdfViewerZoomMode.FitHeight);
            }
            else
            {
                RendererMerge.SetZoomMode(PdfViewerZoomMode.FitHeight);
            }
        }
        private void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            if (isDiffLeftRight)
            {
                Renderer1.ZoomIn();
                Renderer2.ZoomIn();
            }
            else
            {
                RendererMerge.ZoomIn();
            }
        }
        private void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            if (isDiffLeftRight)
            {
                Renderer1.ZoomOut();
                Renderer2.ZoomOut();
            }
            else
            {
                RendererMerge.ZoomOut();
            }
        }
        private void OnRotateLeftClick(object sender, RoutedEventArgs e)
        {
            if (isDiffLeftRight)
            {
                Renderer1.Counterclockwise();
                Renderer2.Counterclockwise();
            }
            else
            {
                RendererMerge.Counterclockwise();
            }
        }
        private void OnRotateRightClick(object sender, RoutedEventArgs e)
        {
            if (isDiffLeftRight)
            {
                Renderer1.ClockwiseRotate();
                Renderer2.ClockwiseRotate();
            }
            else
            {
                RendererMerge.ClockwiseRotate();
            }
        }
        private void OnInfo(object sender, RoutedEventArgs e)
        {
            if (isDiffLeftRight)
            {
                var info1 = Renderer1.GetInformation();
                var info2 = Renderer2.GetInformation();

                var sb = new StringBuilder();
                sb.AppendLine($"Author: [{info1?.Author}] VS [{info2?.Author}]");
                sb.AppendLine($"Creator: [{info1?.Creator}] VS [{info2?.Creator}]");
                sb.AppendLine($"Keywords: [{info1?.Keywords}] VS [{info2?.Keywords}]");
                sb.AppendLine($"Producer: [{info1?.Producer}] VS [{info2?.Producer}]");
                sb.AppendLine($"Subject: [{info1?.Subject}] VS [{info2?.Subject}]");
                sb.AppendLine($"Title: [{info1?.Title}] VS [{info2?.Title}]");
                sb.AppendLine($"Create Date: [{info1?.CreationDate}] VS [{info2?.CreationDate}]");
                sb.AppendLine($"Modified Date: [{info1?.ModificationDate}] VS [{info2?.ModificationDate}]");

                MessageBox.Show(sb.ToString(), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var info = RendererMerge.GetInformation();

                var sb = new StringBuilder();
                sb.AppendLine($"Author: [{info?.Author}] ");
                sb.AppendLine($"Creator: [{info?.Creator}] ");
                sb.AppendLine($"Keywords: [{info?.Keywords}] ");
                sb.AppendLine($"Producer: [{info?.Producer}] ");
                sb.AppendLine($"Subject: [{info?.Subject}] ");
                sb.AppendLine($"Title: [{info?.Title}] ");
                sb.AppendLine($"Create Date: [{info?.CreationDate}] ");
                sb.AppendLine($"Modified Date: [{info?.ModificationDate}] ");

                MessageBox.Show(sb.ToString(), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void OnGetText(object sender, RoutedEventArgs e)
        {
            var txtViewer = new TextViewer();
            var page = Renderer1.PageNo;
            txtViewer.Body = Renderer1.GetPdfText(page);
            txtViewer.Caption = $"Page {page + 1} contains {txtViewer.Body?.Length} character(s):";
            txtViewer.ShowDialog();
        }
        private void OnDisplayBookmarks(object sender, RoutedEventArgs e)
        {
            if (isDiffLeftRight)
            {
                Bookmarks = Renderer1.Bookmarks;
                if (Bookmarks?.Count > 0)
                    ShowBookmarks = !ShowBookmarks;
            }
            else
            {
                Bookmarks = RendererMerge.Bookmarks;
                if (Bookmarks?.Count > 0)
                    ShowBookmarks = !ShowBookmarks;
            }
        }
        private void OnContinuousModeClick(object sender, RoutedEventArgs e)
        {
            Renderer1.PagesDisplayMode = PdfViewerPagesDisplayMode.ContinuousMode;
            Renderer2.PagesDisplayMode = PdfViewerPagesDisplayMode.ContinuousMode;
            RendererMerge.PagesDisplayMode = PdfViewerPagesDisplayMode.ContinuousMode;
        }
        private void OnBookModeClick(object sender, RoutedEventArgs e)
        {
            Renderer1.PagesDisplayMode = PdfViewerPagesDisplayMode.BookMode;
            Renderer2.PagesDisplayMode = PdfViewerPagesDisplayMode.BookMode;
            RendererMerge.PagesDisplayMode = PdfViewerPagesDisplayMode.BookMode;
        }
        private void OnSinglePageModeClick(object sender, RoutedEventArgs e)
        {
            Renderer1.PagesDisplayMode = PdfViewerPagesDisplayMode.SinglePageMode;
            Renderer2.PagesDisplayMode = PdfViewerPagesDisplayMode.SinglePageMode;
            RendererMerge.PagesDisplayMode = PdfViewerPagesDisplayMode.SinglePageMode;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void OnTransparent(object sender, RoutedEventArgs e)
        {
            {
                if ((Renderer1.Flags & PdfRenderFlags.Transparent) != 0)
                {
                    Renderer1.Flags &= ~PdfRenderFlags.Transparent;
                }
                else
                {
                    Renderer1.Flags |= PdfRenderFlags.Transparent;
                }
            }
            {
                if ((Renderer2.Flags & PdfRenderFlags.Transparent) != 0)
                {
                    Renderer2.Flags &= ~PdfRenderFlags.Transparent;
                }
                else
                {
                    Renderer2.Flags |= PdfRenderFlags.Transparent;
                }
            }
            {
                if ((RendererMerge.Flags & PdfRenderFlags.Transparent) != 0)
                {
                    RendererMerge.Flags &= ~PdfRenderFlags.Transparent;
                }
                else
                {
                    RendererMerge.Flags |= PdfRenderFlags.Transparent;
                }
            }
        }
        private void OpenCloseSearch(object sender, RoutedEventArgs e)
        {
            IsSearchOpen = !IsSearchOpen;
            OnPropertyChanged(nameof(IsSearchOpen));
        }

        //private void OnSearchTermKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Enter)
        //    {
        //        Search();
        //    }
        //}

        private void SaveAsImages(object sender, RoutedEventArgs e)
        {
            // Create a "Save As" dialog for selecting a directory (HACK)
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Select a Directory",
                Filter = "Directory|*.this.directory",
                FileName = "select"
            };
            // instead of default "Save As"
            // Prevents displaying files
            // Filename will then be "select.this.directory"
            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                // Remove fake filename from resulting path
                path = path.Replace("\\select.this.directory", "");
                path = path.Replace(".this.directory", "");
                // If user has changed the filename, create the new directory
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                // Our final value is in path
                SaveAsImages(path);
            }
        }

        private void SaveAsImages(string path)
        {
            try
            {
                for (var i = 0; i < Renderer1.PageCount; i++)
                {
                    var size = Renderer1.Document.PageSizes[i];
                    var image = Renderer1.Document.Render(i, (int)size.Width * 5, (int)size.Height * 5, 300, 300, false);
                    image.Save(Path.Combine(path, $"img{i}.png"));
                }
            }
            catch (Exception ex)
            {
                Cts.Cancel();
                Debug.Fail(ex.Message);
                MessageBox.Show(this, ex.Message, "Error!");
            }
        }

        //private void Search()
        //{
        //    SearchMatchItemNo = 0;
        //    SearchManager.MatchCase = MatchCaseCheckBox.IsChecked.GetValueOrDefault();
        //    SearchManager.MatchWholeWord = WholeWordOnlyCheckBox.IsChecked.GetValueOrDefault();
        //    SearchManager.HighlightAllMatches = HighlightAllMatchesCheckBox.IsChecked.GetValueOrDefault();
        //    SearchMatchesTextBlock.Visibility = Visibility.Visible;

        //    if (!SearchManager.Search(SearchTerm))
        //    {
        //        MessageBox.Show(this, "No matches found.");
        //    }
        //    else
        //    {
        //        SearchMatchesCount = SearchManager.MatchesCount;
        //        // DisplayTextSpan(SearchMatches.Items[SearchMatchItemNo++].TextSpan);
        //    }

        //    if (!SearchManager.FindNext(true))
        //        MessageBox.Show(this, "Find reached the starting point of the search.");
        //}

        private void DisplayTextSpan(PdfTextSpan span)
        {
            Page = span.Page + 1;
            Renderer1.ScrollToVerticalOffset(span.Offset);
            Renderer2.ScrollToVerticalOffset(span.Offset);
        }

        private void OnNextFoundClick(object sender, RoutedEventArgs e)
        {
            if (SearchMatchesCount > SearchMatchItemNo)
            {
                SearchMatchItemNo++;
                //DisplayTextSpan(SearchMatches.Items[SearchMatchItemNo - 1].TextSpan);
                SearchManager.FindNext(true);
            }
        }

        private void OnPrevFoundClick(object sender, RoutedEventArgs e)
        {
            if (SearchMatchItemNo > 1)
            {
                SearchMatchItemNo--;
                // DisplayTextSpan(SearchMatches.Items[SearchMatchItemNo - 1].TextSpan);
                SearchManager.FindNext(false);
            }
        }

        private void ToRtlClick(object sender, RoutedEventArgs e)
        {
            Renderer1.IsRightToLeft = true;
            Renderer2.IsRightToLeft = true;
            RendererMerge.IsRightToLeft = true;
            OnPropertyChanged(nameof(IsRtl));
        }

        private void ToLtrClick(object sender, RoutedEventArgs e)
        {
            Renderer1.IsRightToLeft = false;
            Renderer2.IsRightToLeft = false;
            RendererMerge.IsRightToLeft = false;
            OnPropertyChanged(nameof(IsRtl));
        }

        private async void OnClosePdf(object sender, RoutedEventArgs e)
        {
            try
            {
                InfoBar.Foreground = System.Windows.Media.Brushes.Red;
                Renderer1.UnLoad();
                Renderer2.UnLoad();
                RendererMerge.UnLoad();
                await Task.Delay(5000);
                InfoBar.Foreground = System.Windows.Media.Brushes.Black;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private bool isDiffLeftRight = true;
        public PdfRenderer MainRender {
            get
            {
                return isDiffLeftRight ? Renderer1 : RendererMerge;
            }

            private set{}
        }

        private void OnDiffLeftRight(object sender, RoutedEventArgs e)
        {
            SwitchToConfig(0);
        }

        private void OnDiffMerge(object sender, RoutedEventArgs e)
        {
            SwitchToConfig(1);
        }

        private void EnableHandTools(object sender, RoutedEventArgs e)
        {
            var toggle = (ToggleButton)sender;
            Renderer1.EnableKinetic = toggle.IsChecked == true;
            Renderer2.EnableKinetic = toggle.IsChecked == true;
            RendererMerge.EnableKinetic = toggle.IsChecked == true;
        }

        /// <summary>
        /// Call when SelectedBookIndex changed.
        /// </summary>
        private void OnSelectedBookIndexChanged()
        {
            if (SelectedBookIndex != null)
            {
                if (isDiffLeftRight)
                {
                    Renderer1.GotoPage(SelectedBookIndex.PageIndex);
                    Renderer2.GotoPage(SelectedBookIndex.PageIndex);
                }
                else
                {
                    RendererMerge.GotoPage(SelectedBookIndex.PageIndex);
                }
            }
        }

        private void cmbViewType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SwitchToConfig(cmbViewType.SelectedIndex);
        }
    }
}
