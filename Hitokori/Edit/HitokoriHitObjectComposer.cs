﻿using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Hitokori.Beatmaps;
using osu.Game.Rulesets.Hitokori.Edit.Blueprints;
using osu.Game.Rulesets.Hitokori.Edit.SelectionOverlays;
using osu.Game.Rulesets.Hitokori.Objects;
using osu.Game.Rulesets.Hitokori.Objects.Drawables;
using osu.Game.Rulesets.Hitokori.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osu.Game.Screens.Edit.Compose;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace osu.Game.Rulesets.Hitokori.Edit {
	[Cached]
	public class HitokoriHitObjectComposer : HitObjectComposer<HitokoriHitObject> {
		public HitokoriBeatmap Beatmap => (HitokoriBeatmap)EditorBeatmap.PlayableBeatmap;
		new public HitokoriEditorPlayfield Playfield => (HitokoriEditorPlayfield)base.Playfield;

		public readonly Container LayerAbovePlayfield;
		public readonly CameraController CameraController;
		public readonly MultiSelectionContainer ProxiedSelectionContainer;

		[NotNull, MaybeNull]
		private DependencyContainer dependencyContainer;
		protected override IReadOnlyDependencyContainer CreateChildDependencies ( IReadOnlyDependencyContainer parent ) {
			return dependencyContainer = new DependencyContainer( base.CreateChildDependencies( parent ) );
		}

		public HitokoriHitObjectComposer ( Ruleset ruleset ) : base( ruleset ) {
			LayerAbovePlayfield = new Container {
				Name = "Overlays",
				RelativeSizeAxes = Axes.Both,
				Children = new Drawable[] {
					CameraController = new CameraController( this ),
					ProxiedSelectionContainer = new MultiSelectionContainer {
						Alpha = 0.4f
					}
				}
			};

			ProxiedSelectionContainer.OnUpdate += d => {
				var pos = ComposeScreen.ToSpaceOfOtherDrawable( Vector2.Zero, d.Parent );
				if ( d.Position != pos )
					d.Position = ComposeScreen.ToSpaceOfOtherDrawable( Vector2.Zero, d.Parent );

				if ( d.Size != ComposeScreen.DrawSize )
					d.Size = ComposeScreen.DrawSize;
			};
		}

		private ComposeScreen? composeScreen;
		private ComposeScreen ComposeScreen => composeScreen ??= getContainingComposeScreen();

		private ComposeScreen getContainingComposeScreen () {
			Drawable drawable = this;
			while ( !(drawable is ComposeScreen) ) 
				drawable = drawable.Parent;

			return (ComposeScreen)drawable;
		}

		protected override DrawableRuleset<HitokoriHitObject> CreateDrawableRuleset ( Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod>? mods = null )
			=> new DrawableHitokoriRuleset( ruleset, beatmap, mods ) { IsEditor = true };

		public readonly Bindable<TernaryState> ManualCameraToggle = new Bindable<TernaryState>();
		protected override IEnumerable<TernaryButton> CreateTernaryButtons () {
			yield return new TernaryButton( ManualCameraToggle, "Manual Camera", () => new SpriteIcon { Icon = FontAwesome.Solid.Video } );
		}

		protected override void LoadComplete () {
			base.LoadComplete();

			var children = new Drawable[ InternalChildren.Count + 1 ];
			var i = 0;
			foreach ( var c in InternalChildren ) {
				if ( c.Name == "Sidebar" ) { // NOTE this is terrible.
					children[ i++ ] = LayerAbovePlayfield;
				}
				children[ i++ ] = c;
			}
			ClearInternal( disposeChildren: false );
			InternalChildren = children;

			EditorBeatmap.HitObjectRemoved += onHitObjectRemoved;
			EditorBeatmap.HitObjectUpdated += onHitObjectUpdated;

			dependencyContainer.CacheAs<HitokoriPlayfield>( Playfield );
			dependencyContainer.CacheAs<HitokoriBeatmap>( Beatmap );

			ManualCameraToggle.BindValueChanged( v => {
				if ( v.NewValue == TernaryState.True ) 
					Playfield.ShouldUpdateCamera = false;
				else if ( v.NewValue == TernaryState.False ) 
					Playfield.ShouldUpdateCamera = true;
			}, true );
		}

		protected override void Update () {
			base.Update();

			if ( ManualCameraToggle.Value != TernaryState.True ) 
				Playfield.UpdateCameraViewport( Time.Elapsed );
		}

		private void onHitObjectUpdated ( HitObject obj ) {
			if ( obj is not TilePoint tp ) return;

			if ( tp.NextIs( x => x.StartTime < tp.StartTime ) )
				tp.StartTime = tp.Next.StartTime;

			if ( tp.PreviousIs( x => x.StartTime > tp.StartTime ) )
				tp.StartTime = tp.Previous.StartTime;

			tp.Previous?.Invalidate();
			tp.Invalidate();

			foreach ( DrawableHitokoriHitObject i in Playfield.HitObjectContainer.AliveObjects ) {
				i.UpdateInitialVisuals();
			}
		}

		private void onHitObjectRemoved ( HitObject obj ) {
			if ( obj is not TilePoint tp ) return;

			if ( tp.Previous is null && tp.Next is null ) {
				Beatmap.Chains.Remove( tp.ChainID );
				Playfield.RemoveChain( tp.ChainID );
			}
			else if ( tp.Previous is null ) {
				if ( Playfield.ChainWithID( tp.ChainID ).CurrentTile == tp ) {
					Playfield.RemoveChain( tp.ChainID );
					Playfield.AddChain( tp.Next! );
				}

				Beatmap.Chains[ tp.ChainID ] = tp.Next!;
				tp.Next!.ConstrainPosition = tp.Next.Position;
				tp.Next.OrbitalState = tp.Next.OrbitalState; // constraining the value
				tp.ToNext = null;
			}
			else if ( tp.Next is null ) {
				if ( Playfield.ChainWithID( tp.ChainID ).CurrentTile == tp ) {
					Playfield.RemoveChain( tp.ChainID );
					Playfield.AddChain( tp.Previous! );
				}

				tp.FromPrevious = null;
			}
			else {
				splitNeighbours( tp );
			}
		}

		private void linkNeighbours ( TilePoint tp ) {
			var prev = tp.Previous!;
			Playfield.RemoveChain( tp.ChainID );
			tp.FromPrevious!.To = tp.Next;
			Playfield.AddChain( prev );
		}

		private void splitNeighbours ( TilePoint tp ) {
			Playfield.RemoveChain( tp.ChainID );

			tp.Next!.ConstrainPosition = tp.Next.Position;
			tp.Next.OrbitalState = tp.Next.OrbitalState; // constraining the value

			tp.Next.ChainID = Beatmap.CreateChain( tp.Next );
			foreach ( var i in tp.Next.AllNext ) {
				i.ChainID = tp.Next.ChainID;
			}

			var next = tp.Next;
			var prev = tp.Previous!;

			tp.ToNext = null;
			tp.FromPrevious = null;

			Playfield.AddChain( next );
			Playfield.AddChain( prev );
		}

		protected override IReadOnlyList<HitObjectCompositionTool> CompositionTools => Array.Empty<HitObjectCompositionTool>();

		protected override ComposeBlueprintContainer CreateBlueprintContainer ()
			=> new HitokoriComposeBlueprintContainer( this );
	}
}