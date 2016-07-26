﻿using System;
using System.Collections.Generic;
using Lime;
using Tangerine.UI;

namespace Tangerine
{
	public class PreferencesDialog
	{
		readonly UserPreferences preferences;
		readonly Window window;
		readonly WindowWidget rootWidget;
		readonly Button okButton;
		readonly Button cancelButton;

		public PreferencesDialog(UserPreferences preferences)
		{
			this.preferences = preferences;
			window = new Window(new WindowOptions {
				ClientSize = new Vector2(600, 400),
				FixedSize = false,
				Title = "Preferences",
				MinimumDecoratedSize = new Vector2(400, 300)
			});
			rootWidget = new DefaultWindowWidget(window, continuousRendering: false) {
				Padding = new Thickness(8),
				Layout = new VBoxLayout(),
				Nodes = {
					new TabBar {
						Nodes = {
							new Tab {
								Text = "General",
								Active = true
							}
						}
					},
					new BorderedFrame {
						ClipChildren = ClipMethod.ScissorTest,
						LayoutCell = new LayoutCell { StretchY = float.MaxValue },
						Nodes = {
							CreateGenericPane(),
						}
					},
					new Widget { MinHeight = 8 },
					new Widget {
						Layout = new HBoxLayout { Spacing = 8 },
						LayoutCell = new LayoutCell(Alignment.RightCenter),
						Nodes = {
							(okButton = new Button { Text = "Ok" }),
							(cancelButton = new Button { Text = "Cancel" }),
						}
					}
				}
			};
			okButton.Clicked += window.Close;
			cancelButton.Clicked += window.Close;
			new TabTraverseController(rootWidget);
			new WidgetKeyHandler(rootWidget, KeyBindings.CloseDialog).KeyPressed += window.Close;
			KeyboardFocus.Instance.SetFocus(okButton);
		}

		Widget CreateGenericPane()
		{
			var pane = new Widget {
				Padding = new Thickness(16),
				Layout = new TableLayout { 
					ColCount = 2,
					RowCount = 2,
					RowSpacing = 8,
					ColSpacing = 16,
					Cols = new List<LayoutCell> {
						new LayoutCell(Alignment.RightCenter),
						new LayoutCell(Alignment.LeftCenter)
					}
				},
				Nodes = {
					new SimpleText("Default scene size"),
					new Widget {
						Layout = new HBoxLayout { Spacing = 4 },
						Nodes = {
							new EditBox { Text = "1024" },
							new EditBox { Text = "768" }
						}
					},
					new SimpleText("Run on 30fps"),
					new CheckBox(),
				}
			};
			return pane;
		}
	}
}