using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace UnrealModuleManager
{
	public class UnrealProject
	{
		public string Name { get; set; }
		public string PathToUProject { get; set; }
        public string Path { get; set; }

        public int ModuleCount => JsUnrealProject.Modules.Length;

        public JsonUnrealProject JsUnrealProject { get; private set; }

        public partial class JsonUnrealProject
        {
            [JsonProperty("FileVersion")]
            public int FileVersion { get; set; }

            [JsonProperty("EngineAssociation")]
            public string EngineAssociation { get; set; }

            [JsonProperty("Category")]
            public string Category { get; set; }

            [JsonProperty("Description")]
            public string Description { get; set; }

            [JsonProperty("Modules", NullValueHandling = NullValueHandling.Ignore)]
            public Module[] Modules { get; set; }

            [JsonProperty("Plugins", NullValueHandling = NullValueHandling.Ignore)]
            public Plugin[] Plugins { get; set; }
        }

        public partial class Module
        {
            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("Type")]
            public string Type { get; set; }

            [JsonProperty("LoadingPhase")]
            public string LoadingPhase { get; set; }

            [JsonProperty("AdditionalDependencies", NullValueHandling = NullValueHandling.Ignore)]
            public string[] AdditionalDependencies { get; set; }
        }

        public partial class Plugin
        {
            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("Enabled")]
            public bool Enabled { get; set; }

            [JsonProperty("MarketplaceURL", NullValueHandling = NullValueHandling.Ignore)]
            public string MarketplaceUrl { get; set; }
        }

        public UnrealProject(string path, string name)
		{
			Name = path;
			PathToUProject = path;
		}

        /// <summary>
        /// Takes the path and loads th unreal project from it.
        /// </summary>
        /// <param name="path">The Path to the unreal project (e.g. E:\SomeUnrealProject\SomeUnrealProject.uproject).</param>
		public UnrealProject(string path)
		{
            if (!path.EndsWith(".uproject") || !File.Exists(path))
                throw new ArgumentException("The path must exist and lead to an .uproject-File");

            JsUnrealProject = JsonConvert.DeserializeObject<JsonUnrealProject>(File.ReadAllText(path));

            if (JsUnrealProject.Modules == null)
                throw new ArgumentException("Blueprint Projects are not allowed!");

            PathToUProject = path;
            Name = PathToUProject.Split('/', '\\')[^1].Replace(".uproject", "").Trim();
            Path = PathToUProject.Replace(Name + ".uproject", "");
		}

		public UnrealProject() : this ("NoName")
		{
		}

        public override bool Equals(object obj)
        {
            return obj is UnrealProject && (obj as UnrealProject).PathToUProject == PathToUProject;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PathToUProject);
        }

        public static bool operator ==(UnrealProject left, UnrealProject right) 
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnrealProject left, UnrealProject right)
        {
            return !left.Equals(right);
        }
    }
}
