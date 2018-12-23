using Lime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tangerine.Core;

namespace Tangerine.UI
{
	public class NodeIdPropertyEditor : CommonPropertyEditor<string>
	{
		private EditBox editor;

		public NodeIdPropertyEditor(IPropertyEditorParams editorParams, bool multiline = false) : base(editorParams)
		{
			editor = editorParams.EditBoxFactory();
			editor.LayoutCell = new LayoutCell(Alignment.Center);
			editor.Editor.EditorParams.MaxLines = 1;
			EditorContainer.AddNode(editor);
			bool textValid = true;
			editor.AddChangeWatcher(() => editor.Text,
				text => textValid =  PropertyValidator.ValidateValue(text, EditorParams.PropertyInfo, out var none) == ValidationResult.Ok);
			editor.CompoundPostPresenter.Add(new SyncDelegatePresenter<EditBox>(editBox => {
				if (!textValid) {
					editBox.PrepareRendererState();
					Renderer.DrawRect(Vector2.Zero, editBox.Size, Color4.Red.Transparentify(0.8f));
				}
			}));
			editor.Submitted += SetValue;
			editor.AddChangeWatcher(CoalescedPropertyValue(), v => editor.Text = v.IsUndefined ? v.Value : ManyValuesText);
		}

		private void SetValue(string value)
		{
			SetProperty(editor.Text);
			editor.Text = SameValues() ? PropertyValue(EditorParams.Objects.First()).GetValue() : ManyValuesText;
		}
	}
}
