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

            Trace.Listeners.Add(new TextBlockTraceListener(statusBarTextBlock));

            AvailableTitles.CollectionChanged +=
                new NotifyCollectionChangedEventHandler(availableTitles_CollectionChanged);
            SelectedTitles.CollectionChanged +=
                new NotifyCollectionChangedEventHandler(selectedTitles_CollectionChanged);

            try {
                foreach (string dependency in new[] { "eac3to", "xport" }) {
                    try {
                        new Process {
                            StartInfo = {
                                FileName = dependency,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        }.Start();
                    } catch (Win32Exception) {
                        throw new InvalidOperationException("Could not find " + dependency);
                    }
                }
            } catch (InvalidOperationException e) {
                dependencyMissing = true;

                Title += " - " +
                    (Xin1Generator.Properties.Resources.ErrorPrefix + e.Message).ToUpper();
            }
        }

        private void availableTitles_CollectionChanged(object sender,
                NotifyCollectionChangedEventArgs e) {
            SelectedTitles.Clear();
            Tracks.Clear();
        }

        private void selectedTitles_CollectionChanged(object sender,
                NotifyCollectionChangedEventArgs e) {
            clearButton.IsEnabled = SelectedTitles.Count > 0;
            UpdateStartButton();

            if (e.NewStartingIndex == 0 || e.OldStartingIndex == 0 ||
                    e.Action == NotifyCollectionChangedAction.Reset) {
                Tracks.Clear();

                if (SelectedTitles.Count > 0) {
                    tasksRunning++;
                    UpdateStartButton();

                    var worker = new BackgroundWorker();
                    worker.DoWork += (s, args) => {
                        args.Result =
                            Eac3toWrapper.GetTracks(string.Empty, (string)args.Argument);
                    };
                    worker.RunWorkerCompleted += (s, args) => {
                        if (args.Error != null) {
                            Trace.WriteLine(Xin1Generator.Properties.Resources.ErrorPrefix +
                                args.Error.Message);
                            return;
                        }

                        var tracks = (List<Track>)args.Result;

                        Trace.WriteLine("Found " + tracks.Count + " track" +
                            (tracks.Count != 1 ? "s" : string.Empty));

                        foreach (Track track in tracks)
                            Tracks.Add(track);

                        tasksRunning--;
                        UpdateStartButton();
                    };
                    worker.RunWorkerAsync(SelectedTitles[0].Files[0]);
                }
            }
        }

        private void inputPathTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            var worker = new BackgroundWorker();
            worker.DoWork += (s, args) => {
                args.Result = Eac3toWrapper.GetTitles((string)args.Argument);
            };
            worker.RunWorkerCompleted += (s, args) => {
                AvailableTitles.Clear();

                if (args.Error != null) {
                    Trace.WriteLine(Xin1Generator.Properties.Resources.ErrorPrefix +
                        args.Error.Message);
                    return;
                }

                var titles = (List<Title>)args.Result;

                Trace.WriteLine("Found " + titles.Count + " title" +
                    (titles.Count != 1 ? "s" : string.Empty));

                foreach (Title title in titles)
                    AvailableTitles.Add(title);
            };
            worker.RunWorkerAsync(inputPathTextBox.Text);
        }

        private void outputPathTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateStartButton();
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
            while (selectedTitlesListView.SelectedIndex >= 0)
                SelectedTitles.Remove((Title)selectedTitlesListView.SelectedItem);
        }

        private void clearButton_Click(object sender, RoutedEventArgs e) {
            SelectedTitles.Clear();
        }

        private void startButton_Click(object sender, RoutedEventArgs e) {
            tasksRunning++;
            UpdateStartButton();

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
                    string.Empty);

                tasksRunning--;
                UpdateStartButton();
            };
            worker.RunWorkerAsync();
        }

        private void Window_Closed(object sender, EventArgs e) {
            Properties.Settings.Default.Save();
        }

        private void UpdateStartButton() {
            if (startButton != null && inputPathTextBox != null && outputPathTextBox != null)
                startButton.IsEnabled = !dependencyMissing && tasksRunning == 0 &&
                    Directory.Exists(inputPathTextBox.Text) &&
                    Directory.Exists(outputPathTextBox.Text) && SelectedTitles.Count > 0;
        }
    }
}
