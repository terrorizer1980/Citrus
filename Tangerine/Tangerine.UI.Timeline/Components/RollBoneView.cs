﻿using Lime;
using System.Collections.Generic;
using System.Linq;
using Tangerine.Core;
using Tangerine.Core.Components;
using Tangerine.UI.Timeline.Components;
using System;

namespace Tangerine.UI.Timeline
{
	internal class RollBoneView : RollNodeView
	{

		protected readonly BoneRow boneData;

		public RollBoneView(Row row) : base(row)
		{
			boneData = row.Components.Get<BoneRow>();
			var contentExpandButton = CreateContentExpandButton();
			var contentExpandButtonContainer = new Widget {
				Layout = new StackLayout { IgnoreHidden = false },
				LayoutCell = new LayoutCell(Alignment.Center),
				Visible = true,
				Nodes = { contentExpandButton }
			};
			Widget.Nodes.Insert(1, contentExpandButtonContainer);
		}

		ToolbarButton CreateContentExpandButton()
		{
			var button = new ToolbarButton { Highlightable = false, Padding = new Thickness(5) };
			button.Texture = IconPool.GetTexture("Timeline.plus");
			button.AddChangeWatcher(
				() => boneData.ChildrenExpanded,
				i => button.Texture = IconPool.GetTexture(i ? "Timeline.minus" : "Timeline.plus")
			);
			button.AddChangeWatcher(
				() => boneData.HaveChildren,
				i => button.Visible = i);
			button.Clicked += () => Core.Operations.SetProperty.Perform(boneData, nameof(BoneRow.ChildrenExpanded), !boneData.ChildrenExpanded);
			return button;
		}
	}
}
