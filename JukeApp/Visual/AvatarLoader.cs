using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;

using Android.Graphics;

using Rdio.TangoAndCache.Android.Collections;
using Rdio.TangoAndCache.Android.UI.Drawables;
using Android.Content.Res;

namespace JukeApp
{
	public static class AvatarLoader
	{
		static ReuseBitmapDrawableCache memCache;
		static Dictionary<string, Task<SelfDisposingBitmapDrawable>> runningTasks =
			new Dictionary<string, Task<SelfDisposingBitmapDrawable>> ();

		static AvatarLoader ()
		{
			var highWatermark = Java.Lang.Runtime.GetRuntime().MaxMemory() / 3;
			var lowWatermark = highWatermark / 2;
			memCache = new ReuseBitmapDrawableCache (highWatermark, lowWatermark, highWatermark);
		}

		public static bool TryGrabAvatar (string url, out SelfDisposingBitmapDrawable drawable)
		{
			return memCache.TryGetValue (new Uri (url), out drawable);
		}

		public static Task<SelfDisposingBitmapDrawable> FetchAvatarAsync (Resources res, string url)
		{
			SelfDisposingBitmapDrawable result;
			if (TryGrabAvatar (url, out result))
				return Task.FromResult (result);

			Task<SelfDisposingBitmapDrawable> task = null;
			if (runningTasks.TryGetValue (url, out task))
				return task;

			task = InternalFetchAvatarAsync (res, url);
			runningTasks [url] = task;

			return task;
		}

		static async Task<SelfDisposingBitmapDrawable> InternalFetchAvatarAsync (Resources res, string url)
		{
			var client = new HttpClient ();

			var bytes = await client.GetByteArrayAsync (url);
			var bmp = await BitmapFactory.DecodeByteArrayAsync (bytes, 0, bytes.Length);
			//var drawable = new CircleDrawable (res, bmp);
			var drawable = new SelfDisposingBitmapDrawable (res, bmp);
			memCache [new Uri (url)] = drawable;

			return drawable;
		}
	}
}

