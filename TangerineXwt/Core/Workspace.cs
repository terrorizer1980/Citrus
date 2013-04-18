﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Tangerine
{
	public class Workspace
	{
		[ProtoContract]
		class UserPrefs
		{
			[ProtoMember(1)]
			public List<string> OpenedDocuments = new List<string>();

			[ProtoMember(2)]
			public string CurrentDirectory;

			public void Load(string file)
			{
				if (File.Exists(file)) {
					Lime.Serialization.ReadObjectFromFile<UserPrefs>(file, this);
				}
			}

			public void Save(string file)
			{
				Lime.Serialization.WriteObjectToFile<UserPrefs>(file, this);
			}
		}

		UserPrefs userPrefs = new UserPrefs();

		public readonly List<Document> Documents = new List<Document>();
		public string ProjectFile { get; private set; }
		public string AssetsDirectory { get; private set; }
		public static Workspace Instance;
		public string Title { get; private set; }

		public static void Open(string projectFile)
		{
			Instance = new Workspace(projectFile);
		}

		public static bool Close()
		{
			if (Instance == null) {
				return true;
			}
			if (Instance.CloseHelper()) {
				Instance = null;
				return true;
			}
			return false;
		}

		public Workspace(string projectFile)
		{
			ProjectFile = projectFile;
			AssetsDirectory = Path.Combine(Path.GetDirectoryName(projectFile), "Data");
			Title = File.ReadAllText(projectFile);
			//LoadUserPrefs();
			Lime.AssetsBundle.Instance = new Lime.UnpackedAssetsBundle(AssetsDirectory);
			//The.MainWindow.Title = Title + " - Tangerine";
		}

		public void OpenDocument(string file)
		{
			Document doc;
			try {
				doc = new Document(file);
			} catch (Exception e) {
				//var buttons = QMessageBox.StandardButton.Ok;
				//var text = string.Format("An exception has occurred while opening '{0}':\n{1}",
				//	file, e.ToString());
				//QMessageBox.Critical(The.DefaultQtParent, "Tangerine", text, buttons);
				return;
			}
			Documents.Add(doc);
			doc.Closed += () => Documents.Remove(doc);
		}

		private void LoadUserPrefs()
		{
			userPrefs.CurrentDirectory = AssetsDirectory;
			userPrefs.Load(GetUserPrefsFile());
			System.IO.Directory.SetCurrentDirectory(userPrefs.CurrentDirectory);
			foreach (var file in userPrefs.OpenedDocuments) {
				OpenDocument(file);
			}
			userPrefs.OpenedDocuments.Clear();
		}

		private string GetUserPrefsFile()
		{
			var userPrefsFile = Path.ChangeExtension(ProjectFile, ".citproj.user");
			return userPrefsFile;
		}

		public void SaveAll()
		{
			foreach (var doc in Documents) {
				doc.Save();
			}
		}

		private bool CloseHelper()
		{
			SaveUserPrefs();
			foreach (var doc in Documents.ToArray()) {
				if (!doc.Close()) {
					return false;
				}
			}
			//The.MainWindow.Title = "Tangerine";
			Lime.TexturePool.Instance.DiscardAllTextures();
			userPrefs.OpenedDocuments.Clear();
			return true;
		}

		private void SaveUserPrefs()
		{
			userPrefs.OpenedDocuments.Clear();
			userPrefs.CurrentDirectory = System.IO.Directory.GetCurrentDirectory();
			foreach (var doc in Documents) {
				userPrefs.OpenedDocuments.Add(doc.Path);
			}
			userPrefs.Save(GetUserPrefsFile());
		}
	}
}
