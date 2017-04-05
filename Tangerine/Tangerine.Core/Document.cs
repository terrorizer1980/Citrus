﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Lime;
using Tangerine.Core.Components;

namespace Tangerine.Core
{
	public interface IDocumentView
	{
		void Detach();
		void Attach();
	}

	public enum DocumentFormat
	{
		Scene,
		Tan,
		Model
	}

	public sealed class Document
	{
		public enum CloseAction
		{
			Cancel,
			SaveChanges,
			DiscardChanges
		}

		public static readonly string[] AllowedFileTypes = new string[] { "scene", "tan", "model" };

		readonly string defaultPath = "Untitled";
		readonly Vector2 defaultSceneSize = new Vector2(1024, 768);

		public delegate bool PathSelectorDelegate(out string path);

		private readonly Dictionary<object, Row> rowCache = new Dictionary<object, Row>();

		public static event Action<Document> AttachingViews;
		public static Func<Document, CloseAction> Closing;
		public static PathSelectorDelegate PathSelector;

		public static Document Current { get; private set; }

		public readonly DocumentHistory History = new DocumentHistory();
		public bool IsModified => History.IsDocumentModified;

		/// <summary>
		/// The list of Tangerine node decorators.
		/// </summary>
		public static readonly NodeDecoratorList NodeDecorators = new NodeDecoratorList();

		/// <summary>
		/// Gets the path to the document relative to the project directory.
		/// </summary>
		public string Path { get; private set; }

		/// <summary>
		/// Gets or sets the file format the document should be saved to.
		/// </summary>
		public DocumentFormat Format { get; set; }

		/// <summary>
		/// Gets the root node for the current document.
		/// </summary>
		public Node RootNode { get; private set; }

		/// <summary>
		/// Gets or sets the current container widget.
		/// </summary>
		public Node Container { get; set; }

		/// <summary>
		/// Gets or sets the scene we are navigated from. Need for getting back into the main scene from the external one.
		/// </summary>
		public string SceneNavigatedFrom { get; set; }

		/// <summary>
		/// The list of rows, currently displayed on the timeline.
		/// </summary>
		public readonly List<Row> Rows = new List<Row>();

		/// <summary>
		/// The root of the current row hierarchy.
		/// </summary>
		public Row RowTree { get; set; }

		/// <summary>
		/// The list of views (timeline, inspector, ...)
		/// </summary>
		public readonly List<IDocumentView> Views = new List<IDocumentView>();

		public int AnimationFrame
		{
			get { return Container.AnimationFrame; }
			set { Container.AnimationFrame = value; }
		}

		public bool PreviewAnimation { get; set; }
		public int PreviewAnimationBegin { get; set; }
		public Node PreviewAnimationContainer { get; set; }

		public string AnimationId { get; set; }

		public Document()
		{
			Format = DocumentFormat.Scene;
			Path = defaultPath;
			Container = RootNode = new Frame { Size = defaultSceneSize };
		}

		public Document(string path)
		{
			Path = path;
			Format = ResolveFormat(path);
			using (Theme.Push(DefaultTheme.Instance)) {
				RootNode = Node.CreateFromAssetBundle(path);
				if (RootNode is Node3D) {
					var vp = new Viewport3D { Width = 1024, Height = 768 };
					vp.AddNode(RootNode);
					vp.Camera = new Camera3D {
						Id = "DefaultCamera",
						Position = new Vector3(0, 0, 10),
						FarClipPlane = 100000,
						NearClipPlane = 0.001f,
						FieldOfView = 1.0f,
						AspectRatio = 1.3f,
						OrthographicSize = 1.0f
					};
					vp.AddNode(vp.Camera);
					RootNode = vp;
				}
			}
			foreach (var n in RootNode.Descendants) {
				foreach (var d in NodeDecorators) {
					d(n);
				}
			}
			RootNode.Update(0);
			Container = RootNode;
			// Hide all hitboxes
			foreach (var n in RootNode.Descendants.Where(n => n.Id == "HitBox")) {
				(n as Node3D).Visible = false;
			}
		}
		
		static DocumentFormat ResolveFormat(string path)
		{
			if (AssetExists(path, "scene")) {
				return DocumentFormat.Scene;
			} else if (AssetExists(path, "tan")) {
				return DocumentFormat.Tan;
			} else if (AssetExists(path, "model")) {
				return DocumentFormat.Model;
			} else {
				throw new FileNotFoundException(path);
			}
		}
		
		public string GetFileExtension()
		{
			switch (Format) {
				case DocumentFormat.Model: return "model";
				case DocumentFormat.Scene: return "scene";
				case DocumentFormat.Tan: return "tan";
				default: throw new InvalidOperationException();
			}
		}
		
		static bool AssetExists(string path, string ext) => AssetBundle.Instance.FileExists(System.IO.Path.ChangeExtension(path, ext));

		public void MakeCurrent()
		{
			SetCurrent(this);
		}

		public static void SetCurrent(Document doc)
		{
			if (Current != null) {
				Current.DetachViews();
			}
			Current = doc;
			if (doc != null) {
				doc.AttachViews();
			}
		}

		void AttachViews()
		{
			RefreshExternalScenes(RootNode);
			AttachingViews?.Invoke(this);
			foreach (var i in Current.Views) {
				i.Attach();
			}
		}

		void DetachViews()
		{
			foreach (var i in Current.Views) {
				i.Detach();
			}
		}

		private void RefreshExternalScenes(Node node)
		{
			if (node.ContentsPath != null) {
				var doc = Project.Current.Documents.FirstOrDefault(i => i.Path == node.ContentsPath);
				if (doc != null && doc.IsModified) {
					node.Nodes.Clear();
					node.Markers.Clear();
					var content = doc.RootNode.Clone();
					RefreshExternalScenes(content);
					if (content.AsWidget != null && node.AsWidget != null) {
						content.AsWidget.Size = node.AsWidget.Size;
					}
					node.Markers.AddRange(content.Markers);
					var nodes = content.Nodes.ToList();
					content.Nodes.Clear();
					node.Nodes.AddRange(nodes);
				}
			} else {
				foreach (var child in node.Nodes) {
					RefreshExternalScenes(child);
				}
			}
		}

		public bool Close()
		{
			if (!IsModified) {
				return true;
			}
			if (Closing != null) {
				var r = Closing(this);
				if (r == CloseAction.Cancel) {
					return false;
				}
				if (r == CloseAction.SaveChanges) {
					Save();
				}
			} else {
				Save();
			}
			return true;
		}

		public void Save()
		{
			if (Path == defaultPath) {
				string path;
				if (PathSelector(out path)) {
					SaveAs(path);
				}
			} else {
				SaveAs(Path);
			}
		}
		
		public void SaveAs(string path)
		{
			History.AddSavePoint();
			Path = path;
			// Save the document into memory at first to avoid a torn file in the case of a serialization error.
			var ms = new MemoryStream();
			// Dispose cloned object to preserve keyframes identity in the original node. See Animator.Dispose().
			using (var node = CreateCloneForSerialization(RootNode)) {
				if (Format == DocumentFormat.Scene) {
					var serializer = new Orange.HotSceneExporter.Serializer();
					Serialization.WriteObject(path, ms, node, serializer);
				} else {
					Serialization.WriteObject(path, ms, node, Serialization.Format.JSON);
				}
			}
			var fullPath = Project.Current.GetSystemPath(path, GetFileExtension());
			using (var fs = new FileStream(fullPath, FileMode.Create)) {
				var a = ms.ToArray();
				fs.Write(a, 0, a.Length);
			}
		}

		public static Node CreateCloneForSerialization(Node node)
		{
			var clone = node.Clone();
			foreach (var n in clone.Descendants) {
				n.AnimationFrame = 0;
				if (n.Folders != null && n.Folders.Count == 0) {
					n.Folders = null;
				}
			}
			foreach (var n in clone.Descendants.Where(i => i.ContentsPath != null)) {
				n.Nodes.Clear();
				n.Markers.Clear();
			}
			return clone;
		}

		public IEnumerable<Row> SelectedRows()
		{
			foreach (var row in Rows) {
				if (row.Selected) {
					yield return row;
				}
			}
		}

		public IEnumerable<Node> SelectedNodes()
		{
			foreach (var row in Rows) {
				if (row.Selected) {
					var nr = row.Components.Get<NodeRow>();
					if (nr != null) {
						yield return nr.Node;
					}
				}
			}
		}

		public IEnumerable<IFolderItem> SelectedFolderItems()
		{
			foreach (var row in Rows) {
				if (row.Selected) {
					var nr = row.Components.Get<NodeRow>();
					if (nr != null) {
						yield return nr.Node;
					}
					var fr = row.Components.Get<FolderRow>();
					if (fr != null) {
						yield return fr.Folder;
					}
				}
			}
		}

		public IEnumerable<Row> TopLevelSelectedRows()
		{
			foreach (var row in Rows) {
				if (row.Selected) {
					var discardRow = false;
					for (var p = row.Parent; p != null; p = p.Parent) {
						discardRow |= p.Selected;
					}
					if (!discardRow) {
						yield return row;
					}
				}
			}
		}

		public Row GetRowForObject(object obj)
		{
			Row row;
			if (!rowCache.TryGetValue(obj, out row)) {
				row = new Row();
				rowCache.Add(obj, row);
			}
			return row;
		}

		public static bool HasCurrent() => Current != null;

		public class NodeDecoratorList : List<Action<Node>>
		{
			public void AddFor<T>(Action<Node> action) where T: Node
			{
				Add(node => {
					if (node is T) {
						action(node);
					}
				});
			}
		}
	}
}
