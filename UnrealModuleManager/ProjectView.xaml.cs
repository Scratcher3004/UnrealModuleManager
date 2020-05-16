using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static UnrealModuleManager.MainWindow;

namespace UnrealModuleManager
{
	/// <summary>
	/// Interaktionslogik für ProjectView.xaml
	/// </summary>
	public partial class ProjectView : Page
	{
		public UnrealProject SelectedUproject { get; private set; }

		public UnrealProject.Module SelectedModule { get; private set; }
		public List<ModuleDependency> moduleDependencies = new List<ModuleDependency>();

		public string ProjPath => SelectedUproject.PathToUProject;
		public string ProjName => SelectedUproject.Name;
		public string ProjCategory => SelectedUproject.JsUnrealProject.Category != "" ? SelectedUproject.JsUnrealProject.Category : "No Category";
		public string ProjDescription => SelectedUproject.JsUnrealProject.Description != "" ? SelectedUproject.JsUnrealProject.Description : "No Description";
		public string ProjVersion => SelectedUproject.JsUnrealProject.EngineAssociation;
		public JsSettings settings;

		public ProjectView()
		{
			InitializeComponent();
			gr.DataContext = this;
		}

		public ProjectView(UnrealProject uproject, JsSettings s)
		{
			SelectedUproject = uproject;
			settings = s;
			InitializeComponent();
			gr.DataContext = this;
			if (SelectedUproject.JsUnrealProject.Plugins != null)
				Plugins.ItemsSource = SelectedUproject.JsUnrealProject.Plugins;
			Modules.ItemsSource = SelectedUproject.JsUnrealProject.Modules;
		}

		private void Modules_Selected(object sender, RoutedEventArgs e)
		{
			SelectedModule = (Modules.ItemsSource as UnrealProject.Module[])[Modules.SelectedIndex] as UnrealProject.Module;
			moduleDependencies = new List<ModuleDependency>();

			var path = SelectedUproject.Path + "Source/" + SelectedModule.Name + "/" + SelectedModule.Name + ".Build.cs";
			int lineId = -1;

			int x = 0;
			foreach (var item in File.ReadAllLines(path))
			{
				if (!item.Contains("PublicDependencyModuleNames.AddRange(new string[] {"))
				{
					x++;
					continue;
				}

				lineId = x;
				break;
			}

			var line = File.ReadAllLines(path)[lineId];
			var dependencies = line.Replace("PublicDependencyModuleNames.AddRange(new string[] {", " ").Replace("});", " ").   Replace('	', ' ').Trim().Split(", ");

			foreach (var item in dependencies)
			{
				moduleDependencies.Add(new ModuleDependency(item.Replace('"', ' ').Trim(), path));
			}

			if (moduleDependencies.Find(a => a.Name == "Core") != null)
			{
				moduleDependencies.RemoveAll(a => a.Name == "Core" || a.Name == "CoreUObject" || a.Name == "Engine");
				moduleDependencies.Add(new ModuleDependency("Default Dependencies", path, new string[] { "Core", "CoreUObject", "Engine" }));
			}

			if (moduleDependencies.Find(a => a.Name == "UMG") != null)
			{
				moduleDependencies.RemoveAll(a => a.Name == "UMG" || a.Name == "Slate" || a.Name == "SlateCore");
				moduleDependencies.Add(new ModuleDependency("UI", path, new string[] { "UMG", "Slate", "SlateCore" }));
			}

			ModuleDependencies.ItemsSource = moduleDependencies;
		}

		public class ModuleDependency 
		{
			public string Name { get; set; }
			public string PathToDefenition { get; set; }
			public string[] ChildDependencies;

			public ModuleDependency(string name, string pathToDefinitionFile) 
			{
				Name = name;
				PathToDefenition = pathToDefinitionFile;
			}

			public ModuleDependency(string name, string pathToDefinitionFile, string[] childDependencies)
			{
				Name = name;
				PathToDefenition = pathToDefinitionFile;
				ChildDependencies = childDependencies;
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var Dependency = (sender as System.Windows.Controls.Button).DataContext as ModuleDependency;

			if (Dependency.Name == "Default Dependencies")
			{
				System.Windows.MessageBox.Show("You can't remove the default dependencies.");
				return;
			}
			else 
			{
				if (System.Windows.MessageBox.Show("This action can break your code.\n Do you wish to continue anyway?", "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes) 
				{
					return;
				}
			}

			moduleDependencies.Remove(Dependency);

			// Serialize

			var path = Dependency.PathToDefenition;
			int lineId = -1;

			int x = 0;
			foreach (var item in File.ReadAllLines(path))
			{
				if (!item.Contains("PublicDependencyModuleNames.AddRange(new string[] {"))
				{
					x++;
					continue;
				}

				lineId = x;
				break;
			}

			var line = File.ReadAllLines(path)[lineId];

			if (Dependency.Name == "UI")
			{
				RemoveDependencies(new string[]{ "UMG", "Slate", "SlateCore" }, path, lineId);

				if (settings.DeleteDerivedFiles)
				{
					DeleteDerivedFiles();
				}

				System.Windows.MessageBox.Show("Dependency Succesful deleted!", "Completed!");
				return;
			}

			RemoveDependency(Dependency.Name, path, lineId);

			if (settings.DeleteDerivedFiles)
			{
				DeleteDerivedFiles();
			}

			Modules_Selected(null, null);

			System.Windows.MessageBox.Show("Dependency Succesful deleted!", "Completed!");
		}

		private void RemoveDependency(string name, string path, int lineId)
		{
			string[] lines = File.ReadAllLines(path);
			lines[lineId] = lines[lineId].Replace(", \"" + name + "\"", "");
			File.WriteAllLines(path, lines);
		}

		private void RemoveDependencies(string[] dependencies, string path, int lineId)
		{
			string[] lines = File.ReadAllLines(path);

			foreach (var name in dependencies) 
			{
				lines[lineId] = lines[lineId].Replace(", \"" + name + "\"", ""); 
			}

			File.WriteAllLines(path, lines);
		}

		public void DeleteDerivedFiles() 
		{
			if (Directory.Exists(SelectedUproject.Path + ".vs")) 
			{
				Directory.Delete(SelectedUproject.Path + ".vs", true);
			}
			if (Directory.Exists(SelectedUproject.Path + ".idea"))
			{
				Directory.Delete(SelectedUproject.Path + ".idea", true);
			}
			if (Directory.Exists(SelectedUproject.Path + "Binaries"))
			{
				Directory.Delete(SelectedUproject.Path + "Binaries", true);
			}
			if (Directory.Exists(SelectedUproject.Path + "Intermediate"))
			{
				Directory.Delete(SelectedUproject.Path + "Intermediate", true);
			}
			if (settings.IncludeSavedFolder && Directory.Exists(SelectedUproject.Path + "Saved"))
			{
				Directory.Delete(SelectedUproject.Path + "Saved", true);
			}
		}
	}
}
