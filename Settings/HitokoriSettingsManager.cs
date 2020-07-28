﻿using osu.Game.Configuration;
using osu.Game.Rulesets.Configuration;

namespace osu.Game.Rulesets.Hitokori.Settings {
	public class HitokoriSettingsManager : RulesetConfigManager<HitokoriSetting> {
		public HitokoriSettingsManager ( SettingsStore settings, RulesetInfo ruleset, int? variant = null ) : base( settings, ruleset, variant ) { }

		protected override void InitialiseDefaults () {
			base.InitialiseDefaults();

			Set( HitokoriSetting.ADOFAIJudgement, true );
			Set( HitokoriSetting.MissCorrectionMode, MissCorrectionMode.Angle );
			Set( HitokoriSetting.CameraFollowMode, CameraFollowMode.Smooth );
			Set( HitokoriSetting.CameraSpeed, 300, 100, 500, 20.0 );
		}
	}
}