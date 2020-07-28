﻿using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Hitokori.Beatmaps;
using osu.Game.Rulesets.Mods;
using System;

namespace osu.Game.Rulesets.Hitokori.Mods {
	public class HitokoriModStretched : Mod, IApplicableToBeatmapConverter {
		public override string Name => "Stretched";
		public override string Acronym => "Str";
		public override string Description => "Something feels faster";

		public override double ScoreMultiplier => 1.2;

		public override IconUsage? Icon => FontAwesome.Solid.ExpandArrowsAlt;

		public override ModType Type => ModType.DifficultyIncrease;

		public override bool HasImplementation => true;

		public override Type[] IncompatibleMods => new Type[] { typeof( HitokoriModSquashed ) };

		public void ApplyToBeatmapConverter ( IBeatmapConverter beatmapConverter ) {
			( beatmapConverter as HitokoriBeatmapConverter ).Speed = 2;
		}
	}
}