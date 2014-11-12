using System;
using Android.Content;

namespace JukeApp
{
	class PreferenceManager
	{
		Context context;

		public PreferenceManager (Context context)
		{
			this.context = context;
		}

		public ISharedPreferences Preferences {
			get { return context.GetSharedPreferences ("org.neteril.JukeApp", FileCreationMode.Private); }
		}

		public bool HasTokens {
			get {
				return AccessToken != null && AccessSecret != null;
			}
		}

		public string AccessToken {
			get {
				return Preferences.GetString ("accessToken", null);
			}
			set {
				var editor = Preferences.Edit ();
				editor.PutString ("accessToken", value);
				editor.Commit ();
			}
		}

		public string AccessSecret {
			get {
				return Preferences.GetString ("accessSecret", null);
			}
			set {
				var editor = Preferences.Edit ();
				editor.PutString ("accessSecret", value);
				editor.Commit ();
			}
		}

		public bool HasBeenSetup {
			get {
				return CurrentUserKey != null;
			}
		}

		public string CurrentUserKey {
			get {
				return Preferences.GetString ("currentUserKey", null);
			}
			set {
				var editor = Preferences.Edit ();
				editor.PutString ("currentUserKey", value);
				editor.Commit ();
			}
		}
	}
}

