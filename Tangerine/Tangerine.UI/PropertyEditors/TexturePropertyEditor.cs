using System.Collections.Generic;
using System.Linq;
using Lime;
using Tangerine.Core;

namespace Tangerine.UI
{
	public class TexturePropertyEditor<T> : FilePropertyEditor<T> where T: ITexture
	{
		public TexturePropertyEditor(IPropertyEditorParams editorParams) : base(editorParams, new string[] { "png" })
		{ }

		protected override void AssignAsset(string path)
		{
			if (IsValid(path)) {
				SetProperty(new SerializableTexture(path));
			} else {
				editor.Text = (CoalescedPropertyValue().GetValue().Value as SerializableTexture)?.SerializationPath;
				new AlertDialog($"{EditorParams.PropertyName}: Value is not valid", "Ok").Show();
			}
		}

		protected override string ValueToStringConverter(T obj) {
			return (obj as SerializableTexture)?.SerializationPath ?? "";
		}

		protected override T StringToValueConverter(string path) {
			return (T)(ITexture)new SerializableTexture(path);
		}
	}
}
