#if WIN
using System;
using OpenTK;
using OpenTK.Input;

namespace Lime
{
	public class GameView : OpenTK.GameWindow
	{
		public static GameView Instance;
		Application app;
		internal Action ScheduledActions;
		public bool PowerSaveMode { get; set; }

		public GameView(Application app, string[] args = null)
			: base(640, 480, new OpenTK.Graphics.GraphicsMode(32, 0, 0, 1))
		{
			Instance = this;
			this.app = app;
			app.Active = true;
			AudioSystem.Initialize(16);
			app.OnCreate();
			this.Keyboard.KeyDown += HandleKeyDown;
			this.Keyboard.KeyUp += HandleKeyUp;
			this.KeyPress += HandleKeyPress;
			this.Mouse.ButtonDown += HandleMouseButtonDown;
			this.Mouse.ButtonUp += HandleMouseButtonUp;
			this.Mouse.Move += HandleMouseMove;
			// ��� ������ ���������� �������� ������ ��� Windows Forms?
			Size screenSize = new Size(1280, 1024);
			if (CheckFullscreenArg(args)) {
				this.WindowState = OpenTK.WindowState.Fullscreen;
			} else if (CheckMaximizedFlag(args)) {
				this.Location = new System.Drawing.Point(0, 0);
				this.WindowState = OpenTK.WindowState.Maximized;
			} else {
				this.Location = new System.Drawing.Point(
					(screenSize.Width - this.Width) / 2,
					(screenSize.Height - this.Height) / 2
				);
			}
			PowerSaveMode = CheckPowerSaveFlag(args);
		}

		private static bool CheckMaximizedFlag(string[] args)
		{
			return args != null && Array.IndexOf(args, "--Maximized") >= 0;
		}

		private static bool CheckPowerSaveFlag(string[] args)
		{
			return args != null && Array.IndexOf(args, "--PowerSave") >= 0;
		}

		private static bool CheckFullscreenArg(string[] args)
		{
			return args != null && Array.IndexOf(args, "--Fullscreen") >= 0;
		}

		void HandleKeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			Input.SetKeyState((Key)e.Key, true);
		}

		void HandleKeyUp(object sender, KeyboardKeyEventArgs e)
		{
			Input.SetKeyState((Key)e.Key, false);
		}

		void HandleKeyPress(object sender, KeyPressEventArgs e)
		{
			Input.TextInput += e.KeyChar;
		}

		protected override void OnFocusedChanged(EventArgs e)
		{
			Application.Instance.Active = this.Focused;
			if (this.Focused) {
				Application.Instance.OnActivate();
			} else {
				Application.Instance.OnDeactivate();
			}
		}

		void HandleMouseButtonUp(object sender, MouseButtonEventArgs e)
		{
			switch(e.Button) {
			case OpenTK.Input.MouseButton.Left:
				Input.SetKeyState(Key.Mouse0, false);
				Input.SetKeyState(Key.Touch0, false);
				break;
			case OpenTK.Input.MouseButton.Right:
				Input.SetKeyState(Key.Mouse1, false);
				break;
			case OpenTK.Input.MouseButton.Middle:
				Input.SetKeyState(Key.Mouse2, false);
				break;
			}
		}

		void HandleMouseButtonDown(object sender, MouseButtonEventArgs e)
		{
			switch(e.Button) {
			case OpenTK.Input.MouseButton.Left:
				Input.SetKeyState(Key.Mouse0, true);
				Input.SetKeyState(Key.Touch0, true);
				break;
			case OpenTK.Input.MouseButton.Right:
				Input.SetKeyState(Key.Mouse1, true);
				break;
			case OpenTK.Input.MouseButton.Middle:
				Input.SetKeyState(Key.Mouse2, true);
				break;
			}
		}

		void HandleMouseMove(object sender, MouseMoveEventArgs e)
		{
			Vector2 position = new Vector2(e.X, e.Y) * Input.ScreenToWorldTransform;
			Input.MousePosition = position;
			Input.SetTouchPosition(0, position);
		}

		protected override void OnClosed(EventArgs e)
		{
			TexturePool.Instance.DiscardAllTextures();
			AudioSystem.Terminate();
		}

		private long lastMillisecondsCount = 0;

		protected override void OnRenderFrame(OpenTK.FrameEventArgs e)
		{
			lock (Application.MainThreadSync) {
				long millisecondsCount = TimeUtils.GetMillisecondsSinceGameStarted();
				int delta = (int)(millisecondsCount - lastMillisecondsCount);
				delta = delta.Clamp(0, 40);
				lastMillisecondsCount = millisecondsCount;
				DoUpdate(delta);
				DoRender();
				if (PowerSaveMode) {
					millisecondsCount = TimeUtils.GetMillisecondsSinceGameStarted();
					delta = (int)(millisecondsCount - lastMillisecondsCount);
					System.Threading.Thread.Sleep(Math.Max(0, (1000 / 25) - delta));
				}
				if (ScheduledActions != null) {
					ScheduledActions();
					ScheduledActions = null;
				}
			}
		}

		private void DoRender()
		{
			TimeUtils.RefreshFrameRate();
			MakeCurrent();
			app.OnRenderFrame();
			SwapBuffers();
		}

		private void DoUpdate(float delta)
		{
			Input.ProcessPendingKeyEvents();
			Input.MouseVisible = true;
			app.OnUpdateFrame((int)delta);
			Input.TextInput = null;
			Input.CopyKeysState();
		}
		
		public Size WindowSize { 
			get { return new Size(ClientSize.Width, ClientSize.Height); } 
			set { this.ClientSize = new System.Drawing.Size(value.Width, value.Height); } 
		}
		
		public bool FullScreen { 
			get { 
				return this.WindowState == WindowState.Fullscreen;
			}
			set { 
				this.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
			}
		}

		public float FrameRate { 
			get { return TimeUtils.FrameRate; } 
		}
	}
}
#endif