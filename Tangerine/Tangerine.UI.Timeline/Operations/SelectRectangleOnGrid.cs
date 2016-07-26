using System;
using System.Collections.Generic;
using System.Linq;
using Lime;
using Tangerine.Core;

namespace Tangerine.UI.Timeline.Operations
{
	public class SelectRectangleOnGrid : IOperation
	{
		Timeline timeline => Timeline.Instance;
		GridSelection savedSelection;
		IntRectangle rect;

		public bool IsChangingDocument => false;
		public DateTime Timestamp { get; set; }

		public static void Perform(IntRectangle rect)
		{
			Document.Current.History.Perform(new SelectRectangleOnGrid(rect));
		}

		private SelectRectangleOnGrid(IntRectangle rect)
		{
			this.rect = rect;
		}

		public void Do()
		{
			savedSelection = timeline.GridSelection;
			timeline.GridSelection = new GridSelection(savedSelection);
			timeline.GridSelection.Add(rect);
		}

		public void Undo()
		{
			timeline.GridSelection = savedSelection;
			savedSelection = null;
		}
	}
}