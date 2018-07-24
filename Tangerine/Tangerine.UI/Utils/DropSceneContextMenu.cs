using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime;
using Tangerine.Core;
using Tangerine.Core.Operations;

namespace Tangerine.UI
{
	public static class DropSceneContextMenu
	{
		public static void Create(string assetPath, string assetType, Vector2 size, Action<Node> onNodeCreated)
		{
			var fileName = Path.GetFileNameWithoutExtension(assetPath);
			var menu = new Menu() {
				new Command("Open in New Tab", () => Project.Current.OpenDocument(assetPath)),
				new Command("Add As External Scene", () => Document.Current.History.DoTransaction(
					() => {
						var scene = Node.CreateFromAssetBundle(assetPath);
						var node = CreateNode.Perform(scene.GetType());
						SetProperty.Perform(node, nameof(Widget.ContentsPath), assetPath);
						SetProperty.Perform(node, nameof(Node.Id), fileName);
						if (scene is Widget) {
							SetProperty.Perform(node, nameof(Widget.Pivot), Vector2.Half);
							SetProperty.Perform(node, nameof(Widget.Size), size);
						}
						onNodeCreated?.Invoke(node);
						node.LoadExternalScenes();
				})),
				new Command("Cancel")
			};
			menu[0].Enabled = assetType != ".model";
			menu.Popup();
		}
	}
}