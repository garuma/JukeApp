using System;
using Android.Views;
using Android.Graphics;

namespace JukeApp
{
	public class RoundOutlineProvider : ViewOutlineProvider
	{
		public static readonly ViewOutlineProvider Instance = new RoundOutlineProvider ();

		public override void GetOutline (View view, Outline outline)
		{
			outline.SetOval (0, 0, view.Width, view.Height);
		}
	}
}

