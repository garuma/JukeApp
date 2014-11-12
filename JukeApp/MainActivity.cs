using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Rdio.Android.Sdk;
using Rdio.Android.Sdk.Services;
using System.Threading.Tasks;

using Log = Android.Util.Log;

namespace JukeApp
{
	[Activity (Label = "JukeApp", MainLauncher = true, Icon = "@drawable/icon",
	           Theme = "@style/JukeAppTheme")]
	public class MainActivity : Activity, IRdioListener
	{
		TaskCompletionSource<bool> rdioReady = new TaskCompletionSource<bool> ();
		RdioAPI rdioApi;

		PreferenceManager prefs;
		LoadingFragment loadingFragment;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			prefs = new PreferenceManager (this);

			SetContentView (Resource.Layout.Main);

			loadingFragment = new LoadingFragment ();
			FragmentManager
				.BeginTransaction ()
				.Add (Resource.Id.container, new LoadingFragment (), "loading")
				.Commit ();

			DoStartSequence ();
		}

		internal RdioAPI RdioApi {
			get { return rdioApi; }
		}

		async void DoStartSequence ()
		{
			var success = await LogToRdio ();

			if (success) {
				// Since we are logged in, fetch the current id so that we can later
				// answer advertisement without querying the API.
				var query = new AsyncRdioCallback ();
				rdioApi.ApiCall ("currentUser", new Hashtable (), query);
				var obj = await query.WaitForResultAsync ();
				var currentUser = RdioUser.FromJson (obj.GetJSONObject ("result"));
				prefs.CurrentUserKey = currentUser.Key;

				// Preps the playlist
				query.Reset ();
				rdioApi.ApiCall ("getPlaylists", new Hashtable (), query);
				obj = await query.WaitForResultAsync ();
				var stations = obj.GetJSONObject ("result").GetJSONArray ("owned");
				var station = Enumerable.Range (0, stations.Length ())
					.Select (i => stations.GetJSONObject (i))
					.FirstOrDefault (s => s.Has ("name") && s.GetString ("name") == Keys.StationName);
				if (station != null) {
					query.Reset ();
					rdioApi.ApiCall ("deletePlaylist", new Dictionary<string, string> {
						{ "playlist", station.GetString ("key") },
					}, query);
					await query.WaitForResultAsync ();
				}

				FragmentManager
					.BeginTransaction ()
					.Replace (Resource.Id.container, new JukeFragment ())
					.Commit ();
			} else {
				Toast.MakeText (this, "Ouch!!!!!", ToastLength.Long);
				Finish ();
			}
		}

		Task<bool> LogToRdio ()
		{
			if (prefs.HasTokens)
				loadingFragment.WillTakeTime = true;
			rdioApi = new RdioAPI (Keys.RdioKey,
			                       Keys.RdioSecret,
			                       prefs.AccessToken,
			                       prefs.AccessSecret,
			                       this, this);
			return rdioReady.Task;
		}

		public void OnRdioAuthorised (string accessToken, string accessSecret)
		{
			prefs.AccessToken = accessToken;
			prefs.AccessSecret = accessSecret;
			rdioReady.TrySetResult (true);
		}

		public void OnRdioReady ()
		{
			if (prefs.HasTokens)
				rdioReady.TrySetResult (true);
		}

		public void OnRdioUserAppApprovalNeeded (Intent authIntent)
		{
			try {
				StartActivityForResult (authIntent, 100);
			} catch (ActivityNotFoundException) {
				Log.Error ("Auth", "Rdio App not found");
			}
		}

		public void OnRdioUserPlayingElsewhere ()
		{

		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			if ((int)resultCode == RdioAPI.ResultAuthorisationRejected) {
				Finish ();
				return;
			} else if (data != null && (int)resultCode == RdioAPI.ResultAuthorisationAccepted) {
				rdioApi.SetTokenAndSecret (data);
			}
		}
	}
}


