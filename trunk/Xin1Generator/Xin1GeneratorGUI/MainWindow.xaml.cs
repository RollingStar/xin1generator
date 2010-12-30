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
        private const string chaptersName = "chapters.xml";
        private const string tagsName = "tags.xml";
        private const string qpfileName = "qpfile.txt";
        private const string demuxName = "demux.cmd";

        public ObservableCollection<AvailableTitle> AvailableTitles { get; set; }
        public ObservableCollection<SelectedTitle> SelectedTitles { get; set; }

        private bool dependencyMissing = false;
        private bool taskRunning = false;

        public MainWindow() {
            AvailableTitles = new ObservableCollection<AvailableTitle>();
            SelectedTitles = new ObservableCollection<SelectedTitle>();

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
        }

        private void selectedTitles_CollectionChanged(object sender,
                NotifyCollectionChangedEventArgs e) {
            clearButton.IsEnabled = SelectedTitles.Count > 0;
            UpdateStartButton();
        }

        private void inputPathTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            var worker = new BackgroundWorker();
            worker.DoWork += (s, args) => {
                args.Result = Eac3toWrapper.GetTitles((string)args.Argument);
            };
            worker.RunWorkerCompleted += (s, args) => {
                AvailableTitles.Clear();

                if (args.Error != null) {
                    Trace.WriteLine(
                        Xin1Generator.Properties.Resources.ErrorPrefix + args.Error.Message);
                    return;
                }

                var titles = (IDictionary<int, Title>)args.Result;

                Trace.WriteLine("Found " + titles.Count + " title" +
                   (titles.Count != 1 ? "s" : string.Empty));

                foreach (int number in titles.Keys)
                    AvailableTitles.Add(new AvailableTitle(number, titles[number].Length));
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
            foreach (AvailableTitle title in availableTitlesListView.SelectedItems)
                SelectedTitles.Add(new SelectedTitle(title.Number) {
                    Name = "Edition " + (SelectedTitles.Count + 1) });
        }

        private void removeButton_Click(object sender, RoutedEventArgs e) {
            while (selectedTitlesListView.SelectedIndex >= 0)
                SelectedTitles.Remove((SelectedTitle)selectedTitlesListView.SelectedItem);
        }

        private void clearButton_Click(object sender, RoutedEventArgs e) {
            SelectedTitles.Clear();
        }

        private void startButton_Click(object sender, RoutedEventArgs e) {
            startButton.IsEnabled = !(taskRunning = true);

            var p = new Xin1Generator.Parameters {
                InputPath = inputPathTextBox.Text,
                OutputPath = outputPathTextBox.Text,
                DemuxTracks = (bool)demuxTracksCheckBox.IsChecked,
                HideChapters = (bool)hideChaptersCheckBox.IsChecked
            };

            p.TitleNumbers.AddRange(SelectedTitles.Select(x => x.Number));
            p.TitleNames.AddRange(SelectedTitles.Select(x => x.Name));
            
            var worker = new BackgroundWorker();
            worker.DoWork += (s, args) => {
                var xin1Generator = new Xin1Generator.Xin1Generator(p);
                xin1Generator.ExtractInfo();
                xin1Generator.GenerateAll(chaptersName, tagsName, qpfileName, demuxName);
            };
            worker.RunWorkerCompleted += (s, args) => {
                Trace.WriteLine(args.Error != null ?
                    Xin1Generator.Properties.Resources.ErrorPrefix + args.Error.Message :
                    string.Empty);

                taskRunning = false;
                UpdateStartButton();
            };
            worker.RunWorkerAsync();
        }

        private void Window_Closed(object sender, EventArgs e) {
            Properties.Settings.Default.Save();
        }

        private void UpdateStartButton() {
            if (startButton != null && inputPathTextBox != null && outputPathTextBox != null)
                startButton.IsEnabled = !dependencyMissing && !taskRunning &&
                    Directory.Exists(inputPathTextBox.Text) &&
                    Directory.Exists(outputPathTextBox.Text) && SelectedTitles.Count > 0;
        }
    }
}
