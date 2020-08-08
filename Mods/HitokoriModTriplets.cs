﻿using osu.Framework.Graphics.Sprites;
using osu.Game.Rulesets.Hitokori.UI;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Hitokori.Mods {
	public class HitokoriModTriplets : Mod {
		public override string Name => "Triplets";
		public override string Acronym => "TR";
		public override string Description => "Mom said it's my turn on the rythms";

		public override double ScoreMultiplier => 1;

		public override IconUsage? Icon => new IconUsage( '∴' );

		public override bool Ranked => false;

		public override ModType Type => ModType.Fun;

		public override bool HasImplementation => true;
	}
}
