
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

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
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media;

using Android.Support.V7.Widget;
using Android.Support.V7.Palette;

using Rdio.TangoAndCache.Android.Collections;
using Rdio.TangoAndCache.Android.UI.Drawables;
using Rdio.Android.Sdk;
using System.Collections;
using Android.Support.V7.Graphics;

namespace JukeApp
{
	public class JukeFragment : Fragment, IObserver<string>, MediaPlayer.IOnPreparedListener
	{
		CheckedImageButton playPauseButton;
		ImageButton skipNextButton;
		TextView songName, songArtist;
		View playerFrame;
		ImageView albumCover, avatarIcon;

		IUserDiscoverer discoverer;
		CancellationTokenSource source;
		UserAdapter adapter;

		MediaPlayer player;
		List<RdioTrack> trackQueue = new List<RdioTrack> ();
		Random rnd = new Random ();

		public JukeFragment ()
		{
			EnterTransition = new Explode ();
			adapter = new UserAdapter ();
		}

		RdioAPI RdioApi {
			get {
				var activity = Activity;
				if (activity == null)
					return null;
				return ((MainActivity)activity).RdioApi;
			}
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			return inflater.Inflate (Resource.Layout.JukeLayout, container, false);
		}

		public override void OnViewCreated (View view, Bundle savedInstanceState)
		{
			base.OnViewCreated (view, savedInstanceState);
			var recycler = view.FindViewById<RecyclerView> (Resource.Id.userList);
			recycler.SetLayoutManager (new LinearLayoutManager (view.Context));
			recycler.SetAdapter (adapter);
			recycler.SetItemAnimator (new DefaultItemAnimator ());
			playPauseButton = view.FindViewById<CheckedImageButton> (Resource.Id.playPauseButton);
			playPauseButton.Click += HandlePlayPauseClick;
			skipNextButton = view.FindViewById<ImageButton> (Resource.Id.skipNextButton);
			skipNextButton.Click += HandleSkipClick;
			songName = view.FindViewById<TextView> (Resource.Id.songName);
			songArtist = view.FindViewById<TextView> (Resource.Id.songArtist);
			playerFrame = view.FindViewById (Resource.Id.playerFrame);
			albumCover = view.FindViewById<ImageView> (Resource.Id.albumCover);
			avatarIcon = view.FindViewById<ImageView> (Resource.Id.avatarIcon);
			avatarIcon.OutlineProvider = RoundOutlineProvider.Instance;
			avatarIcon.ClipToOutline = true;
			avatarIcon.SetImageResource (Resource.Color.loading_background);
			var toolbar = view.FindViewById<Toolbar> (Resource.Id.toolbar);
			Activity.SetActionBar (toolbar);
		}

		void HandleSkipClick (object sender, EventArgs e)
		{
			if (player != null) {
				player.Stop ();
				player.Release ();
				player = null;
				EnsurePlayerState ();
			}
		}

		void HandlePlayPauseClick (object sender, EventArgs e)
		{
			if (player != null) {
				if (player.IsPlaying)
					player.Pause ();
				else
					player.Start ();
			}
		}

		public override void OnStart ()
		{
			base.OnStart ();
			if (discoverer == null) {
				//discoverer = new NearbyUserDiscoverer ();
				discoverer = new TestDiscoverer ();
				discoverer.Subscribe (this);
			}
			source = new CancellationTokenSource ();
			discoverer.StartDiscovery (source.Token);
		}

		public override void OnStop ()
		{
			base.OnStop ();
			if (source != null) {
				source.Cancel ();
				source = null;
			}
		}

		public void OnCompleted ()
		{
		}

		public void OnError (Exception error)
		{
		}

		public async void OnNext (string userKey)
		{
			var api = RdioApi;
			if (api == null)
				return;
			var getUserQuery = new AsyncRdioCallback ();
			api.ApiCall ("get", new Dictionary<string, string> {
				{ "keys", userKey },
				{ "extras", "collectionKey" }
			}, getUserQuery);

			var obj = await getUserQuery.WaitForResultAsync ();
			if (obj.GetString ("status") != "ok" || !obj.Has ("result"))
				return;
			var user = RdioUser.FromJson (obj.GetJSONObject ("result").GetJSONObject (userKey));
			adapter.AddRdioUser (user);

			AddUserSongsToPlaylist (user);
		}

		async void AddUserSongsToPlaylist (RdioUser user)
		{
			var api = RdioApi;
			if (api == null || user.CollectionStationKey == null)
				return;

			// First gets the top song for the user
			var query = new AsyncRdioCallback ();
			api.ApiCall ("generateStation", new Dictionary<string, string> {
				{ "station_key", user.CollectionStationKey }
			}, query);
			var stationObj = await query.WaitForResultAsync ();
			var tracksArray = stationObj
				.GetJSONObject ("result")
				.GetJSONArray ("tracks");
			var trackIDs = Enumerable.Range (0, tracksArray.Length ())
				.Select (i => tracksArray.GetJSONObject (i))
				.Select (t => t.GetString ("key"))
				.ToList ();

			adapter.SetSongCountForUser (user, trackIDs.Count);

			foreach (var id in trackIDs)
				trackQueue.Add (new RdioTrack (user, id));

			EnsurePlayerState ();
		}

		void EnsurePlayerState ()
		{
			if (player != null && player.IsPlaying)
				return;
			var api = RdioApi;
			if (api == null)
				return;
			if (trackQueue.Count == 0)
				return;

			playPauseButton.Checked = true;
			var nextTrack = RandomDequeue ();
			player = api.GetPlayerForTrack (nextTrack.TrackKey, null, true);
			player.SetOnPreparedListener (this);
			player.Completion += HandlePlayerCompletion;
			player.PrepareAsync ();

			UpdateUserSongsCount ();

			SetupPlayerInfo (api, nextTrack);
		}

		RdioTrack RandomDequeue ()
		{
			var index = rnd.Next (0, trackQueue.Count);
			var track = trackQueue [index];
			trackQueue.RemoveAt (index);
			return track;
		}

		void UpdateUserSongsCount ()
		{
			var groups = trackQueue.GroupBy (t => t.TrackFromUser);
			foreach (var stash in groups)
				adapter.SetSongCountForUser (stash.Key, stash.Count ());
		}

		async void SetupPlayerInfo (RdioAPI api, RdioTrack currentTrack)
		{
			var query = new AsyncRdioCallback ();
			api.ApiCall ("get", new Dictionary<string, string> {
				{ "keys", currentTrack.TrackKey }
			}, query);
			var trackObj = await query.WaitForResultAsync ();
			var track = trackObj
				.GetJSONObject ("result")
				.GetJSONObject (currentTrack.TrackKey);

			songArtist.Text = track.GetString ("albumArtist");
			songName.Text = track.GetString ("name");

			var albumUrl = track.GetString ("icon");
			var client = new HttpClient ();
			var bytes = await client.GetByteArrayAsync (albumUrl);
			var bmp = await BitmapFactory.DecodeByteArrayAsync (bytes, 0, bytes.Length);

			var palette = await Task.Run (() => Palette.Generate (bmp));
			var foregroundLight = palette.GetVibrantColor (Color.White.ToArgb ());
			var foregroundMuted = palette.GetLightVibrantColor (Color.LightGray.ToArgb ());
			var background = palette.GetDarkMutedColor (0xAB0042);

			songName.SetTextColor (new Color (foregroundLight));
			songArtist.SetTextColor (new Color (foregroundMuted));
			((StateListDrawable)playPauseButton.Drawable)
				.SetTintList (Android.Content.Res.ColorStateList.ValueOf (new Color (foregroundLight)));
			playPauseButton.Invalidate ();
			skipNextButton.Drawable.SetTint (foregroundLight);
			playerFrame.SetBackgroundColor (new Color (background));
			albumCover.SetImageBitmap (bmp);

			var avatar = await AvatarLoader.FetchAvatarAsync (avatarIcon.Resources,
			                                                  currentTrack.TrackFromUser.AvatarUrl);
			avatarIcon.SetImageDrawable (avatar);
		}

		public void OnPrepared (MediaPlayer mp)
		{
			mp.Start ();
		}

		void HandlePlayerCompletion (object sender, EventArgs e)
		{
			player.Completion -= HandlePlayerCompletion;
			player = null;
			EnsurePlayerState ();
		}
	}

	class UserAdapter : RecyclerView.Adapter
	{
		LayoutInflater inflater;
		List<RdioUser> users = new List<RdioUser> ();
		Dictionary<RdioUser, int> countForUsers = new Dictionary<RdioUser, int> ();

		class ViewHolder : RecyclerView.ViewHolder
		{
			public ViewHolder (View view) : base (view)
			{
				Avatar = view.FindViewById<ImageView> (Resource.Id.avatarImage);
				Name = view.FindViewById<TextView> (Resource.Id.userName);
				TrackCount = view.FindViewById<TextView> (Resource.Id.trackNumber);
			}

			public ImageView Avatar {
				get;
				private set;
			}

			public TextView Name {
				get;
				private set;
			}

			public TextView TrackCount {
				get;
				private set;
			}
		}

		public UserAdapter ()
		{
			HasStableIds = true;
		}

		public void AddRdioUser (RdioUser user)
		{
			users.Add (user);
			NotifyItemInserted (users.Count - 1);
		}

		public void SetSongCountForUser (RdioUser user, int count)
		{
			int previousCount;
			if (countForUsers.TryGetValue (user, out previousCount)
			    && previousCount == count)
				return;

			countForUsers [user] = count;
			var position = users.IndexOf (user);
			NotifyItemChanged (position);
		}

		public override long GetItemId (int p0)
		{
			return users [p0].Key.GetHashCode ();
		}

		public override void OnBindViewHolder (RecyclerView.ViewHolder viewHolder, int index)
		{
			var user = users [index];
			var holder = viewHolder as ViewHolder;
			if (holder == null)
				return;

			holder.Name.Text = user.FirstName + " " + user.LastName;

			int count = 0;
			if (!countForUsers.TryGetValue (user, out count))
				holder.TrackCount.Text = "-";
			else
				holder.TrackCount.Text = count.ToString ();

			SelfDisposingBitmapDrawable avatar;
			if (AvatarLoader.TryGrabAvatar (user.AvatarUrl, out avatar))
				holder.Avatar.SetImageDrawable (avatar);
			else {
				var context = viewHolder.ItemView.Context;
				holder.Avatar.SetImageDrawable (context.GetDrawable (Android.Resource.Color.Transparent));
				FetchAvatarForHolder (holder);
			}
		}

		async void FetchAvatarForHolder (ViewHolder holder)
		{
			var resources = holder.ItemView.Resources;
			var position = holder.Position;
			var user = users [position];
			var url = user.AvatarUrl;

			var avatar = await AvatarLoader.FetchAvatarAsync (resources, url);

			if (holder.Position == position)
				holder.Avatar.SetImageDrawable (avatar);
			else
				NotifyItemChanged (position);
		}

		public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int position)
		{
			if (inflater == null)
				inflater = LayoutInflater.From (parent.Context);
			var holder = new ViewHolder (inflater.Inflate (Resource.Layout.JukeUserLayout, parent, false));
			holder.Avatar.OutlineProvider = RoundOutlineProvider.Instance;
			holder.Avatar.ClipToOutline = true;
			holder.Avatar.SetImageResource (Resource.Color.loading_background);
			return holder;
		}

		public override int ItemCount {
			get {
				return users.Count;
			}
		}
	}
}

