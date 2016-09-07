using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Lime;
using Yuzu;

namespace Tangerine.UI
{
	public class DockPanel
	{
		public WindowWidget WindowWidget { get; set; }
		public PanelPlacement Placement { get; set; }

		public readonly Widget RootWidget;
		public readonly Widget TitleWidget;
		public readonly Widget ContentWidget;
		public readonly Button CloseButton;
		public readonly string Title;

		public DockPanel(string title)
		{
			Title = title;
			TitleWidget = new Widget {
				Layout = new HBoxLayout(),
				Nodes = {
					new SimpleText { Text = Title, Padding = new Thickness(4, 0), AutoSizeConstraints = false, MinMaxHeight = 20 },
					(CloseButton = new DesktopTheme.TabCloseButton { LayoutCell = new LayoutCell(Alignment.Center) })
				},
				HitTestTarget = true
			};
			ContentWidget = new Frame { Id = "PanelContent", ClipChildren = ClipMethod.ScissorTest, Layout = new StackLayout() };
			RootWidget = new Widget {
				Id = $"DockPanel<{title}>",
				LayoutCell = new LayoutCell(),
				Layout = new VBoxLayout(),
				Nodes = {
					TitleWidget,
					ContentWidget
				}
			};
			RootWidget.FocusScope = new KeyboardFocusScope(RootWidget);
		}

		internal void RefreshDockedSize()
		{
			var s = Vector2.Zero;
			foreach (var w in RootWidget.Parent.Nodes) {
				s += w.AsWidget.LayoutCell.Stretch;
			}
			Placement.DockedSize = RootWidget.LayoutCell.Stretch / s;
		}

		public class PanelPlacement
		{
			[YuzuMember]
			public string Title;
			[YuzuMember]
			public bool Docked;
			[YuzuMember]
			public DockSite Site;
			[YuzuMember]
			public Vector2 DockedSize;
			[YuzuMember]
			public bool Hidden;
			[YuzuMember]
			public Vector2 UndockedPosition;
			[YuzuMember]
			public Vector2 UndockedSize;
		}

		public class DragBehaviour
		{
			readonly WindowWidget mainWidget;
			readonly DockPanel panel;

			public event Action<DockSite> OnDock;
			public event Action<Vector2> OnUndock;

			public DragBehaviour(WindowWidget mainWindow, DockPanel panel)
			{
				this.mainWidget = mainWindow;
				this.panel = panel;
				panel.TitleWidget.Tasks.Add(MainTask());
			}

			IEnumerator<object> MainTask()
			{
				var input = panel.TitleWidget.Input;
				while (true) {
					var mousePos = input.MousePosition;
					if (input.WasMousePressed()) {
						while ((mousePos - input.MousePosition).Length < 10 && input.IsMousePressed()) {
							yield return null;
						}
						if (input.IsMousePressed()) {
							yield return DragTask();
						}
					}
					yield return null;
				}
			}

			IEnumerator<object> DragTask()
			{
				var dockSite = DockSite.None;
				var dockSiteRect = new Rectangle();
				mainWidget.PostPresenter = new DelegatePresenter<Widget>(widget => {
					if (dockSite != DockSite.None) {
						widget.PrepareRendererState();
						Renderer.DrawRectOutline(dockSiteRect.A + Vector2.One, dockSiteRect.B - Vector2.One, Colors.Docking.DragRectagleOutline, 2);
					}
				});
				var input = panel.TitleWidget.Input;
				input.CaptureMouse();
				const float dockSiteWidth = 0.25f;
				var dockSiteRects = new Rectangle[4] {
					new Rectangle(Vector2.Zero, new Vector2(dockSiteWidth, 1)),
					new Rectangle(Vector2.Zero, new Vector2(1, dockSiteWidth)),
					new Rectangle(new Vector2(1 - dockSiteWidth, 0), Vector2.One),
					new Rectangle(new Vector2(0, 1 - dockSiteWidth), Vector2.One)
				};
				ThumbnalWindow thumbWindow = null;
				var initialMousePos = input.MousePosition;
				var mainWindow = mainWidget.Window;
				while (input.IsMousePressed()) {
					var extent = mainWindow.ClientSize;
					dockSite = DockSite.None;
					for (int i = 0; i < 4; i++) {
						var r = dockSiteRects[i];
						r.A *= extent;
						r.B *= extent;
						var p = Application.DesktopMousePosition - mainWindow.ClientPosition;
						if (Application.Platform == PlatformId.Mac) {
							p.Y = mainWindow.ClientSize.Y - p.Y;
						}
						if (r.Contains(p)) {
							dockSiteRect = r;
							dockSite = (DockSite)(i + 1);
						}
					}
					if (dockSite == DockSite.None) {
						if (thumbWindow == null) {
							thumbWindow = new ThumbnalWindow(panel.Title);
						}
					} else {
						mainWindow.Invalidate();
						thumbWindow?.Dispose();
						thumbWindow = null;
					}
					yield return null;
				}
				input.ReleaseMouse();
				mainWidget.PostPresenter = null;
				mainWindow.Invalidate();
				thumbWindow?.Dispose();
				thumbWindow = null;
				if (dockSite != DockSite.None) {
					OnDock?.Invoke(dockSite);
				} else {
					OnUndock?.Invoke(Application.DesktopMousePosition - (initialMousePos - panel.TitleWidget.GlobalPosition));
				}
			}
		}

		class ThumbnalWindow : IDisposable
		{
			Window window;
			WindowWidget rootWidget;

			public ThumbnalWindow(string title)
			{
				window = new Window(new WindowOptions { FixedSize = true, ClientSize = new Vector2(100, 40), Style = WindowStyle.Borderless });
				rootWidget = new InvalidableWindowWidget(window) {
					PostPresenter = new WidgetBoundsPresenter(Color4.Black, 1),
					Layout = new StackLayout(),
					Nodes = {
						new SimpleText { Text = title, LayoutCell = new LayoutCell(Alignment.Center) }
					}
				};
				rootWidget.Updated += delta => StickToMouseCursor();
				StickToMouseCursor();
			}

			void StickToMouseCursor()
			{
				window.DecoratedPosition = Application.DesktopMousePosition - window.ClientSize / 2;
			}

			public void Dispose()
			{
				window.Close();
			}
		}
	}
}
