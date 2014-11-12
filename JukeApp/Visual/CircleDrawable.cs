using System;

using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Content.Res;

using Rdio.TangoAndCache.Android.UI.Drawables;

namespace JukeApp
{
	public class CircleDrawable : SelfDisposingBitmapDrawable
	{
		RectF mRect = new RectF ();
		BitmapShader bitmapShader;
		Paint paint;
		int margin;

		public CircleDrawable (Resources res, Bitmap bitmap, int margin = 3)
			: base (res, bitmap)
		{
			this.margin = margin;
			bitmapShader = new BitmapShader (bitmap, Shader.TileMode.Clamp, Shader.TileMode.Clamp);

			paint = new Paint () {
				AntiAlias = true,
			};
			paint.SetShader (bitmapShader);
		}

		protected override void OnBoundsChange (Rect bounds)
		{
			base.OnBoundsChange (bounds);
			mRect.Set (margin, margin, bounds.Width () - margin, bounds.Height () - margin);
		}

		public override void Draw (Canvas canvas)
		{
			canvas.DrawCircle (mRect.CenterX (), mRect.CenterY (), mRect.Width () / 2, paint);
		}

		public override int Opacity {
			get {
				return (int)Format.Translucent;
			}
		}

		public override void SetAlpha (int alpha)
		{
			paint.Alpha = alpha;
		}

		public override void SetColorFilter (ColorFilter cf)
		{
			paint.SetColorFilter (cf);
		}
	}
}

