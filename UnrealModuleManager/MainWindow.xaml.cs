using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UnrealModuleManager
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public partial class JsSettings
		{
			[JsonProperty("UnrealPath", NullValueHandling = NullValueHandling.Ignore)]
			public string UnrealPath { get; set; }

			[JsonProperty("DeleteDerivedFiles", NullValueHandling = NullValueHandling.Ignore)]
			public bool DeleteDerivedFiles { get; set; } = true;

			[JsonProperty("IncludeSavedFolder", NullValueHandling = NullValueHandling.Ignore)]
			public bool IncludeSavedFolder { get; set; }

			[JsonProperty("AutomaticallyRecompile", NullValueHandling = NullValueHandling.Ignore)]
			public bool AutomaticallyRecompile { get; set; }
		}

		public static UnrealProject lastInitializedProject;
		private readonly string appdatapath;

		public static JsSettings settings;

		public MainWindow()
		{
			appdatapath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\UnrealHelpers\\ModuleManager\\settings.json";
			LoadSettings();
			InitializeComponent();

			/*ProjectControl.ItemsSource = Projects;

			Projects.Add(new UnrealProject("Test", 2));
			Projects.Add(new UnrealProject("Riese", 45));
			Projects.Add(new UnrealProject("Normal", 7));*/
		}

		private void LoadSettings()
		{
			if (File.Exists(appdatapath)) 
			{
				settings = JsonConvert.DeserializeObject<JsSettings>(File.ReadAllText(appdatapath));
				return;
			}

			settings = new JsSettings { AutomaticallyRecompile = false, DeleteDerivedFiles = true, IncludeSavedFolder = false };
			if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/UnrealHelpers/ModuleManager"))
				Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/UnrealHelpers/ModuleManager");
			File.WriteAllText(appdatapath, JsonConvert.SerializeObject(settings));
		}

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			var XamlSettings = Resources["Settings"];
			((Grid)XamlSettings).DataContext = settings;

			foreach (var item in tabs.Items)
			{
				if ((item as TabItem).Content != XamlSettings)
					continue;

				tabs.SelectedIndex = tabs.Items.IndexOf(item);
				return;
			}

			tabs.Items.Add(new TabItem{ Content = XamlSettings, Header = "Settings" });
			tabs.SelectedIndex = tabs.Items.Count - 1;
		}

		private void Reset_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("This action will reset all settings. Do you wish to continue?", "Continue?", MessageBoxButton.YesNo) == MessageBoxResult.Yes) 
			{
				if (File.Exists(appdatapath))
					File.Delete(appdatapath);

				LoadSettings();
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			File.WriteAllText(appdatapath, JsonConvert.SerializeObject(settings));
			MessageBox.Show("Updated Settings!");
		}

		private void Load_Click(object sender, RoutedEventArgs e)
		{
			var fileDialogue = new OpenFileDialog { Title = "Open Unreal Project", Filter = "Unreal Projects(*.uproject)|*.uproject" };

			if (!(bool)fileDialogue.ShowDialog())
				return;

			if (!File.Exists(fileDialogue.FileName) || JsonConvert.DeserializeObject<UnrealProject.JsonUnrealProject>(File.ReadAllText(fileDialogue.FileName)).Modules == null) 
			{
				MessageBox.Show("Please select a cpp Unreal Project.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			OpenTab(new UnrealProject(fileDialogue.FileName));
		}

		private void OpenTab(UnrealProject unrealProject) 
		{
			foreach (var item in tabs.Items)
			{
				if (!((item as TabItem).Content is Frame) || !(((item as TabItem).Content as Frame).Content is ProjectView) || (((item as TabItem).Content as Frame).Content as ProjectView).SelectedUproject == unrealProject)
					continue;

				tabs.SelectedIndex = tabs.Items.IndexOf(item);
				return;
			}

			var tab = new TabItem
			{
				Content = new Frame { Width = 794, Height = 405, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch,
					HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch, Content = new ProjectView(unrealProject, settings) },
				Header = unrealProject.Name,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Stretch
			};

			tabs.Items.Add(tab);
			tabs.SelectedIndex = tabs.Items.Count - 1;
		}

		private void LoadFolder_Click(object sender, RoutedEventArgs e)
		{
			var folderBrowser = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog{ Description = "Please select a folder to load unreal projects from..." };

			if (!(bool)folderBrowser.ShowDialog() || !Directory.Exists(folderBrowser.SelectedPath))
				return;

			var files = Directory.GetFiles(folderBrowser.SelectedPath, "*.uproject", SearchOption.AllDirectories);
			bool createdTab = false;

			foreach (var item in files)
			{
				if (!File.Exists(item) || JsonConvert.DeserializeObject<UnrealProject.JsonUnrealProject>(File.ReadAllText(item)).Modules == null)
					continue;

				OpenTab(new UnrealProject(item));
				createdTab = true;
			}

			if (!createdTab)
				MessageBox.Show("There wasn't any unreal-project in the folder.", "Error");
		}
	}
}
