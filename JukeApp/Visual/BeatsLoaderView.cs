
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Animation;
using Android.Graphics;
using Android.Views.Animations;
using Android.Graphics.Drawables;

namespace JukeApp
{
	public class BeatsLoaderView : View
	{
		const int CircleCount = 2;
		const int Duration = 500;

		Paint circlePaint;
		Drawable ripple;
		float variation;

		ObjectAnimator animator;

		public BeatsLoaderView (Context context) :
			base (context)
		{
			Initialize ();
		}

		public BeatsLoaderView (Context context, IAttributeSet attrs) :
			base (context, attrs)
		{
			Initialize ();
		}

		public BeatsLoaderView (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{
			Initialize ();
		}

		void Initialize ()
		{
			ripple = Resources.GetDrawable (Resource.Drawable.ripple_background);
			circlePaint = new Paint {
				Color = Color.White,
				AntiAlias = true
			};

			animator = ObjectAnimator.OfFloat (this, "scaleX", 0f,
			                                   1f + (CircleCount - 1) * .5f);
			animator.SetInterpolator (new LinearInterpolator ());
			animator.SetDuration (2000);
			animator.RepeatCount = ObjectAnimator.Infinite;
			animator.Start ();
		}

		public override float ScaleX {
			get {
				return variation;
			}
			set {
				variation = value;
				Invalidate ();
			}
		}

		protected override void OnDraw (Android.Graphics.Canvas canvas)
		{
			var w = Width;
			int baseRadius = w / 2;

			for (int i = 0; i < CircleCount; i++) {
				var value = variation - i * .5f;
				value = Math.Max (0f, Math.Min (1f, value));
				value = InvertQuadratic (value);
				var radius = (int)Math.Round (baseRadius * value);
				ripple.SetBounds (-radius, -radius, w + radius, w + radius);
				ripple.SetAlpha ((int)(255f * (1 - value)));
				ripple.Draw (canvas);
			}
		}

		float InvertQuadratic (float time)
		{
			return 1 - (1 - time) * (1 - time);
		}
	}
}

