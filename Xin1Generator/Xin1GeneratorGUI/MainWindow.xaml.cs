using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Xin1Generator;

namespace Xin1GeneratorGUI {
    public partial class MainWindow : Window {
        public ObservableCollection<Title> AvailableTitles { get; set; }
        public ObservableCollection<Title> SelectedTitles { get; set; }
        public ObservableCollection<Track> Tracks { get; set; }

        private bool dependencyMissing;
        private AbortableBackgroundWorker worker;

        public MainWindow() {
            AvailableTitles = new ObservableCollection<Title>();
            SelectedTitles = new ObservableCollection<Title>();
            Tracks = new ObservableCollection<Track>();

            InitializeComponent();

            Trace.Listeners.Add(new TextBlockTraceListener(statusBarTextBlock));

            AssemblyName assemblyName = typeof(Xin1Generator.Xin1Generator).Assembly.GetName();
            Title = string.Format(Xin1Generator.Properties.Resources.NameAndVersionFormat,
                assemblyName.Name, assemblyName.Version.ToString(2));

            using (var worker = new BackgroundWorker()) {
                worker.DoWork += (s, args) => {
                    Utilities.CheckDependencies();
                };
                worker.RunWorkerCompleted += (s, args) => {
                    if (args.Error != null) {
                        dependencyMissing = true;
                        Title += " - " + string.Format(
                            Xin1Generator.Properties.Resources.ErrorMessage,
                            args.Error.Message);
                    }
                };
                worker.RunWorkerAsync();
            }

            SelectedTitles.CollectionChanged +=
                new NotifyCollectionChangedEventHandler(SelectedTitles_CollectionChanged);
        }

        private void SelectedTitles_CollectionChanged(object sender,
                NotifyCollectionChangedEventArgs e) {
            clearButton.IsEnabled = SelectedTitles.Count > 0;

            if (e.NewStartingIndex + e.OldStartingIndex >= 0)
                return;

            if (Tracks.Count > 0) {
                Tracks.Clear();
                UpdateStartStopButton();
            }

            Trace.WriteLine("Loading tracks...");

            using (var worker = new BackgroundWorker()) {
                worker.DoWork += (s, args) => {
                    if (SelectedTitles.Count > 0)
                        args.Result = Eac3toWrapper.GetTracks(SelectedTitles[0].Files[0]);
                };
                worker.RunWorkerCompleted += (s, args) => {
                    if (SelectedTitles.Count == 0)
                        Trace.WriteLine(string.Empty);
                    else if (args.Error != null)
                        Trace.WriteLine(string.Format(
                            Xin1Generator.Properties.Resources.ErrorMessage,
                            args.Error.Message));
                    else {
                        var tracks = (List<Track>)args.Result;

                        foreach (Track track in tracks)
                            Tracks.Add(track);

                        Trace.WriteLine("Found " + tracks.Count + " track" +
                            (tracks.Count != 1 ? "s" : string.Empty));

                        UpdateStartStopButton();
                    }
                };
                worker.RunWorkerAsync();
            }
        }

        private void inputPathTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            AvailableTitles.Clear();

            if (SelectedTitles.Count > 0)
                SelectedTitles.Clear();

            Trace.WriteLine("Loading titles...");

            using (var worker = new BackgroundWorker()) {
                worker.DoWork += (s, args) => {
                    args.Result = Eac3toWrapper.GetTitles((string)args.Argument);
                };
                worker.RunWorkerCompleted += (s, args) => {
                    if (args.Error != null) {
                        Trace.WriteLine(string.Format(
                            Xin1Generator.Properties.Resources.ErrorMessage,
                            args.Error.Message));
                    } else {
                        var titles = (List<Title>)args.Result;

                        foreach (Title title in titles)
                            AvailableTitles.Add(title);

                        Trace.WriteLine("Found " + titles.Count + " title" +
                            (titles.Count != 1 ? "s" : string.Empty));
                    }
                };
                worker.RunWorkerAsync(inputPathTextBox.Text);
            }
        }

        private void outputPathTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateStartStopButton();
        }

        private void inputPathButton_Click(object sender, RoutedEventArgs e) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                inputPathTextBox.Text = dialog.SelectedPath;
        }

        private void outputPathButton_Click(object sender, RoutedEventArgs e) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                outputPathTextBox.Text = dialog.SelectedPath;
        }

        private void availableTitlesListView_SelectionChanged(object sender,
                SelectionChangedEventArgs e) {
            addButton.IsEnabled = ((ListView)sender).SelectedIndex != -1;
        }

        private void selectedTitlesListView_SelectionChanged(object sender,
                SelectionChangedEventArgs e) {
            removeButton.IsEnabled = ((ListView)sender).SelectedIndex != -1;
        }

        private void addButton_Click(object sender, RoutedEventArgs e) {
            foreach (Title title in availableTitlesListView.SelectedItems)
                SelectedTitles.Add(new Title(title) {
                    Name = title.Name ?? "Edition " + (SelectedTitles.Count + 1)
                });
        }

        private void removeButton_Click(object sender, RoutedEventArgs e) {
            var titles = new Title[selectedTitlesListView.SelectedItems.Count];
            selectedTitlesListView.SelectedItems.CopyTo(titles, 0);

            for (int i = titles.Length - 1; i >= 0; i--)
                SelectedTitles.Remove(titles[i]);
        }

        private void clearButton_Click(object sender, RoutedEventArgs e) {
            SelectedTitles.Clear();
        }

        private void startStopButton_Click(object sender, RoutedEventArgs e) {
            if (worker != null && worker.IsBusy) {
                Trace.WriteLine("Aborting...");
                worker.Abort();
                return;
            }

            var p = new Parameters {
                InputPath = inputPathTextBox.Text,
                OutputPath = outputPathTextBox.Text,
                ExtractTracks = (bool)extractTracksCheckBox.IsChecked,
                PreserveChapters = (bool)preserveChaptersCheckBox.IsChecked
            };

            p.Titles.AddRange(SelectedTitles);
            p.Tracks.AddRange(Tracks.Where(x => x.IsUsed));

            using (worker = new AbortableBackgroundWorker()) {
                worker.DoWork += (s, args) => {
                    var xin1Generator = new Xin1Generator.Xin1Generator(p);
                    xin1Generator.GenerateAll();
                };
                worker.RunWorkerCompleted += (s, args) => {
                    if (args.Error != null)
                        Trace.WriteLine(string.Format(
                            Xin1Generator.Properties.Resources.ErrorMessage,
                            args.Error.Message));
                    else if (args.Cancelled)
                        Trace.WriteLine("Aborted");
                    else
                        Trace.WriteLine("Completed");

                    UpdateStartStopButton();
                };
                worker.RunWorkerAsync();
            }

            UpdateStartStopButton();
        }

        private void Window_Closed(object sender, EventArgs e) {
            Properties.Settings.Default.Save();
        }

        private void UpdateStartStopButton() {
            if (startStopButton == null)
                return;

            if (worker != null && worker.IsBusy) {
                startStopButton.Content = "Stop";
                startStopButton.IsEnabled = true;
            } else {
                startStopButton.Content = "Start";
                startStopButton.IsEnabled = !dependencyMissing &&
                    Directory.Exists(outputPathTextBox.Text) && Tracks.Count > 0;
            }
        }
    }
}
