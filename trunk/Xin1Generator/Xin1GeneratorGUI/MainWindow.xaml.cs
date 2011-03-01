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
        private int tasksRunning;

        public MainWindow() {
            AvailableTitles = new ObservableCollection<Title>();
            SelectedTitles = new ObservableCollection<Title>();
            Tracks = new ObservableCollection<Track>();

            InitializeComponent();

            AssemblyName assemblyName = typeof(Xin1Generator.Xin1Generator).Assembly.GetName();
            Title = string.Format(Xin1Generator.Properties.Resources.NameAndVersionFormat,
                assemblyName.Name, assemblyName.Version.ToString(2));

            var worker = new BackgroundWorker();
            worker.DoWork += (s, args) => {
                Xin1Generator.Xin1Generator.CheckDependencies();
            };
            worker.RunWorkerCompleted += (s, args) => {
                if (args.Error != null) {
                    Title += " - " + (Xin1Generator.Properties.Resources.ErrorPrefix +
                        args.Error.Message).ToUpper();

                    dependencyMissing = true;
                }
            };
            worker.RunWorkerAsync();

            Trace.Listeners.Add(new TextBlockTraceListener(statusBarTextBlock));

            SelectedTitles.CollectionChanged +=
                new NotifyCollectionChangedEventHandler(SelectedTitles_CollectionChanged);
        }

        private void SelectedTitles_CollectionChanged(object sender,
                NotifyCollectionChangedEventArgs e) {
            clearButton.IsEnabled = SelectedTitles.Count > 0;

            if (e.NewStartingIndex + e.OldStartingIndex >= 0)
                return;

            UpdateStartButton(++tasksRunning);

            var worker = new BackgroundWorker();
            worker.DoWork += (s, args) => {
                args.Result = Eac3toWrapper.GetTracks(string.Empty, SelectedTitles[0].Files[0]);
            };
            worker.RunWorkerCompleted += (s, args) => {
                if (Tracks.Count > 0)
                    Tracks.Clear();

                if (SelectedTitles.Count == 0) {
                    Trace.WriteLine(string.Empty);
                } else if (args.Error != null) {
                    Trace.WriteLine(
                        Xin1Generator.Properties.Resources.ErrorPrefix + args.Error.Message);
                } else {
                    var tracks = (List<Track>)args.Result;

                    foreach (Track track in tracks)
                        Tracks.Add(track);

                    Trace.WriteLine("Found " + tracks.Count + " track" +
                        (tracks.Count != 1 ? "s" : string.Empty));
                }

                UpdateStartButton(--tasksRunning);
            };
            worker.RunWorkerAsync();
        }

        private void inputPathTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (SelectedTitles.Count > 0)
                SelectedTitles.Clear();

            var worker = new BackgroundWorker();
            worker.DoWork += (s, args) => {
                args.Result = Eac3toWrapper.GetTitles((string)args.Argument);
            };
            worker.RunWorkerCompleted += (s, args) => {
                AvailableTitles.Clear();

                if (args.Error != null) {
                    Trace.WriteLine(
                        Xin1Generator.Properties.Resources.ErrorPrefix + args.Error.Message);
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

        private void outputPathTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateStartButton(tasksRunning);
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
                    Name = "Edition " + (SelectedTitles.Count + 1)
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

        private void startButton_Click(object sender, RoutedEventArgs e) {
            UpdateStartButton(++tasksRunning);

            var p = new Xin1Generator.Parameters {
                InputPath = inputPathTextBox.Text,
                OutputPath = outputPathTextBox.Text,
                ExtractTracks = (bool)extractTracksCheckBox.IsChecked,
                HideChapters = (bool)hideChaptersCheckBox.IsChecked
            };

            p.Titles.AddRange(SelectedTitles);
            p.Tracks.AddRange(Tracks.Where(x => x.IsUsed));
            
            var worker = new BackgroundWorker();
            worker.DoWork += (s, args) => {
                var xin1Generator = new Xin1Generator.Xin1Generator(p);
                xin1Generator.ExtractInfo();
                xin1Generator.GenerateAll();
            };
            worker.RunWorkerCompleted += (s, args) => {
                Trace.WriteLine(args.Error != null ?
                    Xin1Generator.Properties.Resources.ErrorPrefix + args.Error.Message :
                    "Completed succesfully");

                UpdateStartButton(--tasksRunning);
            };
            worker.RunWorkerAsync();
        }

        private void Window_Closed(object sender, EventArgs e) {
            Properties.Settings.Default.Save();
        }

        private void UpdateStartButton(int tasksRunning) {
            if (startButton != null && inputPathTextBox != null && outputPathTextBox != null) {
                startButton.Content = tasksRunning == 0 ? "Start" : "Working...";
                startButton.IsEnabled = tasksRunning == 0 && !dependencyMissing &&
                    Directory.Exists(inputPathTextBox.Text) &&
                    Directory.Exists(outputPathTextBox.Text) && SelectedTitles.Count > 0;
            }
        }
    }
}
