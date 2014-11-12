
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
using Android.Transitions;
using Android.Animation;
using Android.Views.Animations;

namespace JukeApp
{
	public class LoadingFragment : Fragment
	{
		ObjectAnimator flashAnimator;
		TextView extraTimeText;
		bool willTakeTime;

		public LoadingFragment ()
		{
			ExitTransition = new Fade (FadingMode.Out);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			return inflater.Inflate (Resource.Layout.LoadingScreen, container, false);
		}

		public override void OnViewCreated (View view, Bundle savedInstanceState)
		{
			base.OnViewCreated (view, savedInstanceState);

			extraTimeText = view.FindViewById<TextView> (Resource.Id.extraExpl);
			extraTimeText.Visibility = willTakeTime ? ViewStates.Visible : ViewStates.Invisible;

			var logo = view.FindViewById (Resource.Id.logoImage);
			flashAnimator = ObjectAnimator.OfFloat (logo, "alpha", 0, 1);
			flashAnimator.RepeatMode = ValueAnimatorRepeatMode.Reverse;
			flashAnimator.RepeatCount = ObjectAnimator.Infinite;
			flashAnimator.SetDuration (800);
			flashAnimator.SetInterpolator (AnimationUtils.LoadInterpolator (view.Context, Android.Resource.Interpolator.FastOutSlowIn));
			flashAnimator.Start ();
		}

		public bool WillTakeTime {
			get {
				return willTakeTime || (extraTimeText != null && extraTimeText.Visibility == ViewStates.Visible);
			}
			set {
				if (extraTimeText != null)
					extraTimeText.Visibility = value ? ViewStates.Visible : ViewStates.Invisible;
				else
					willTakeTime = true;
			}
		}
	}
}

