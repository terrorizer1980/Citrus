﻿using System;
using System.Linq;
using Lime;
using Tangerine.Core;
using Tangerine.UI;

namespace Tangerine.UI.Timeline
{
	public class Toolbar
	{
		public Widget RootWidget { get; private set; }
		
		public Toolbar()
		{
			RootWidget = new Widget {
				Padding = new Thickness { Left = 4 },
				MinMaxHeight = Metrics.ToolbarHeight,
				MinWidth = TimelineMetrics.ToolbarMinWidth,
				Presenter = new DelegatePresenter<Widget>(Render),
				Layout = new HBoxLayout { Spacing = 2, CellDefaults = new LayoutCell(Alignment.Center) },
				Nodes = {
					CreateAnimationModeButton(),
					CreateAutoKeyframesButton(),
					new Widget(),
					CreateExitButton(),
					CreateEyeButton(),
					CreateLockButton()
				}
			};
		}
		
		void Render(Widget widget)
		{
			widget.PrepareRendererState();
			Renderer.DrawRect(Vector2.Zero, widget.Size, ToolbarColors.Background); 
		}

		ToolbarButton CreateAnimationModeButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.AnimationMode")) { Tip = "Animation mode" };
			button.Tasks.Add(new Property<bool>(() => UserPreferences.Instance.AnimationMode).DistinctUntilChanged().Consume(i => button.Checked = i));
			button.Clicked += () => {
				UserPreferences.Instance.AnimationMode = !UserPreferences.Instance.AnimationMode;
			};
			return button;
		}

		ToolbarButton CreateAutoKeyframesButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.Key")) { Tip = "Automatic keyframes" };
			button.Tasks.Add(new Property<bool>(() => UserPreferences.Instance.AutoKeyframes).DistinctUntilChanged().Consume(i => button.Checked = i));
			button.Clicked += () => {
				UserPreferences.Instance.AutoKeyframes = !UserPreferences.Instance.AutoKeyframes;
			};
			return button;
		}

		ToolbarButton CreateExitButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.ExitContainer")) { Tip = "Exit current container (backspace)" };
			button.Clicked += Core.Operations.LeaveNode.Perform;
			button.Updating += _ => button.Enabled = Core.Operations.LeaveNode.IsAllowed();
			return button;
		}

		ToolbarButton CreateEyeButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.Eye")) { Tip = "Show widgets" };
			button.Clicked += () => {
				var visibility = NodeVisibility.Hidden;
				if (Document.Current.Container.Nodes.All(i => i.EditorState().Visibility == NodeVisibility.Hidden)) {
					visibility = NodeVisibility.Shown;
				} else if (Document.Current.Container.Nodes.All(i => i.EditorState().Visibility == NodeVisibility.Shown)) {
					visibility = NodeVisibility.Default;
				}
				foreach (var node in Document.Current.Container.Nodes) {
					Core.Operations.SetGenericProperty<NodeVisibility>.Perform(
						() => node.EditorState().Visibility, value => node.EditorState().Visibility = value,
						visibility
					);
				}
			};
			return button;
		}

		ToolbarButton CreateLockButton()
		{
			var button = new ToolbarButton(IconPool.GetTexture("Timeline.Lock")) { Tip = "Lock widgets" };
			button.Clicked += () => {
				var locked = Document.Current.Container.Nodes.All(i => !i.EditorState().Locked);
				foreach (var node in Document.Current.Container.Nodes) {
					Core.Operations.SetGenericProperty<bool>.Perform(
						() => node.EditorState().Locked, value => node.EditorState().Locked = value,
						locked
					);
				}
			};
			return button;
		}
	}
}

