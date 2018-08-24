using Lime;
using Yuzu;

namespace Tangerine.Core
{
	public class CoreUserPreferences : Component
	{
		[YuzuOptional]
		public bool AutoKeyframes { get; set; }

		[YuzuOptional]
		public bool AnimationMode { get; set; }

		[YuzuOptional]
		public KeyFunction DefaultKeyFunction { get; set; }

		[YuzuOptional]
		public bool ReloadModifiedFiles { get; set; }

		[YuzuOptional]
		public bool StopAnimationOnCurrentFrame { get; set; }

		public CoreUserPreferences()
		{
			ResetToDefaults();
		}

		public void ResetToDefaults()
		{
			AutoKeyframes = false;
			AnimationMode = false;
			DefaultKeyFunction = KeyFunction.Linear;
			StopAnimationOnCurrentFrame = false;
		}

		public static CoreUserPreferences Instance => UserPreferences.Instance.Get<CoreUserPreferences>();
	}
}
