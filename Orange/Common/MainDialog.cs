using System;
using System.IO;
using Lime;
using Lemon;
using System.Reflection;
using System.Collections.Generic;

namespace Orange
{
	public partial class MainDialog : Gtk.Dialog
	{
		public MainDialog ()
		{
			Build ();
			LoadState ();
			TextWriter writer = new LogWriter (CompileLog);
			Console.SetOut (writer);
			Console.SetError (writer);
			SyncButton.GrabFocus ();
		}
		
		void LoadState ()
		{
			var config = AppConfig.Load ();
			AssetsFolderChooser.SetCurrentFolder (config.AssetsFolder);
			TargetPlatform.Active = config.TargetPlatform;
			GameAssemblyChooser.SetFilename (config.GameAssembly);
			GameProtoChooser.SetFilename (config.GameProto);
		}
		
		void SaveState ()
		{
			var config = AppConfig.Load ();
			config.AssetsFolder = AssetsFolderChooser.CurrentFolder;
			config.TargetPlatform = TargetPlatform.Active;
			config.GameAssembly = GameAssemblyChooser.Filename;
			config.GameProto = GameProtoChooser.Filename;
			AppConfig.Save (config);
		}
		
		class LogWriter : TextWriter
		{
			Gtk.TextView textView;
			int bufferedLines = 0;
			
			public LogWriter (Gtk.TextView textView)
			{
				this.textView = textView;			
			}
			
			public override void WriteLine (string value)
			{
				Write (value);
			}
			
			public override void Write (string value)
			{
				value += "\n";
				#pragma warning disable 618
				textView.Buffer.Insert (textView.Buffer.EndIter, value);
				while (Gtk.Application.EventsPending ())
					Gtk.Application.RunIteration ();
				if (bufferedLines > 4) {
					bufferedLines = 0;
					textView.ScrollToIter (textView.Buffer.EndIter, 0, false, 0, 0);
				}
				bufferedLines++;
			}
		
			public override System.Text.Encoding Encoding {
				get {
					throw new NotImplementedException ();
				}
			}
		}
		
		public static void GenerateSerializerDll (ProtoBuf.Meta.RuntimeTypeModel model, string directory)
		{
			string currentDirectory = System.IO.Directory.GetCurrentDirectory ();
			try {
				System.IO.Directory.SetCurrentDirectory (directory);
				model.Compile ("Serializer", "Serializer.dll");
			} finally {
				System.IO.Directory.SetCurrentDirectory (currentDirectory);
			}
		}
		
		private void RegisterEngineTypes (ProtoBuf.Meta.RuntimeTypeModel model)
		{
			model.Add (typeof(Node), true);
			model.Add (typeof(TextureAtlasPart), true);
			model.Add (typeof(Font), true);
		}

		private void PrepareTypeModel (ProtoBuf.Meta.RuntimeTypeModel model, Assembly gameAssembly)
		{
			Type gameWidgets = gameAssembly.GetType ("SerializationSupport");
			if (gameWidgets == null) {
				throw new Lime.Exception ("Class 'SerializationSupport' not found in assembly {0}", gameAssembly);
			}
			MethodInfo mi = gameWidgets.GetMethod ("SetupTypes");
			mi.Invoke (null, new object [] {model});
		}
		
		private void RunBuild (bool rebuild)
		{
			SaveState ();
			try {
				System.DateTime startTime = System.DateTime.Now;
				CompileLog.Buffer.Clear ();
				// Generate game proto C# binding
				var gameProto = GameProtoChooser.Filename;
				if (gameProto == "" || !File.Exists (gameProto)) {
					gameProto = null;
				}
				if (gameProto != null) {
					ProtoGen.Execute (gameProto);
				}
				// Load game assembly if specified
				Assembly gameAssembly = null;
				if (File.Exists (GameAssemblyChooser.Filename)) {
					gameAssembly = Assembly.LoadFile (GameAssemblyChooser.Filename);
				}
				// Create serialization model
				var model = ProtoBuf.Meta.TypeModel.Create ();
				RegisterEngineTypes (model);
				Serialization.Serializer = model;
				if (gameAssembly != null) {
					// Populate model with ingame types
					PrepareTypeModel (model, gameAssembly);
				}
				model.CompileInPlace ();
				// Cook all assets (the main job)
				var platform = (TargetPlatform)this.TargetPlatform.Active;
				AssetCooker cooker = new AssetCooker (AssetsFolderChooser.CurrentFolder, platform);
				cooker.Cook (rebuild, gameAssembly, gameProto);
				// Update serialization assembly	
				GenerateSerializerDll (model, System.IO.Path.Combine (AssetsFolderChooser.CurrentFolder, ".."));
				// Show time statistics
				System.DateTime endTime = System.DateTime.Now;
				System.TimeSpan delta = endTime - startTime;
				Console.WriteLine ("Done at " + endTime.ToLongTimeString ());
				Console.WriteLine ("Building time {0}:{1}:{2}", delta.Hours, delta.Minutes, delta.Seconds);
				CompileLog.ScrollToIter (CompileLog.Buffer.EndIter, 0, false, 0, 0);				
			} catch (System.Exception exc) {
				Console.WriteLine ("Exception: " + exc.Message);
			}
		}
		
		protected void OnSyncClicked (object sender, System.EventArgs e)
		{
			this.Sensitive = false;
			try {
				RunBuild (false);
			} finally {
				this.Sensitive = true;
			}
		}

		protected void OnRebuildButtonClicked (object sender, System.EventArgs e)
		{
			this.Sensitive = false;
			try {
				RunBuild (true);
			} finally {
				this.Sensitive = true;
			}
		}
	}
}