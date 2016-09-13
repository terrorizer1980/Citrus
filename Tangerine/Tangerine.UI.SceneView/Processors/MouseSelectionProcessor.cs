﻿using System;
using System.Linq;
using System.Collections.Generic;
using Lime;
using Tangerine.Core;

namespace Tangerine.UI.SceneView
{
	class MouseSelectionProcessor : IProcessor
	{
		public IEnumerator<object> Loop()
		{
			var sceneView = SceneView.Instance;
			var input = sceneView.Input;
			var canvasInput = sceneView.Scene.Input;
			while (true) {
				if (input.WasMousePressed() && !CommonWindow.Current.Input.IsKeyPressed(Key.Space)) {
					var rect = new Rectangle(canvasInput.LocalMousePosition, canvasInput.LocalMousePosition);
					var presenter = new DelegatePresenter<Widget>(w => {
						w.PrepareRendererState();
						Renderer.DrawRectOutline(rect.A, rect.B, SceneViewColors.MouseSelection, 1);
					});
					sceneView.Scene.CompoundPostPresenter.Add(presenter);
					input.CaptureMouse();
					var occasionalClick = true;
					while (input.IsMousePressed()) {
						rect.B = canvasInput.LocalMousePosition;
						occasionalClick &= (rect.B - rect.A).Length <= 5;
						if (!occasionalClick) {
							RefreshSelectedWidgets(rect);
							CommonWindow.Current.Invalidate();
						}
						yield return null;
					}
					input.ReleaseMouse();
					sceneView.Scene.CompoundPostPresenter.Remove(presenter);
					CommonWindow.Current.Invalidate();
				}
				yield return null;
			}
		}

		void RefreshSelectedWidgets(Rectangle rect)
		{
			var currentSelection = Document.Current.SelectedNodes().OfType<Widget>();
			var selectionQuad = rect.ToQuadrangle();
			var newSelection = Document.Current.Container.Nodes.OfType<Widget>().Where(w => selectionQuad.Intersects(w.CalcHullInSpaceOf(SceneView.Instance.Scene)));
			if (!currentSelection.SequenceEqual(newSelection)) {
				Core.Operations.ClearRowSelection.Perform();
				foreach (var node in newSelection) {
					Core.Operations.SelectNode.Perform(node);
				}
			}
		}
	}
}