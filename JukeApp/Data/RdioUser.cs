using System;

namespace JukeApp
{
	public class RdioUser: IEquatable<RdioUser>
	{
		public string Key { get; private set; }
		public string FirstName { get; private set; }
		public string LastName { get; private set; }
		public string AvatarUrl { get; private set; }
		public string CollectionStationKey { get; private set; }

		public static RdioUser FromJson (Org.Json.JSONObject json)
		{
			return new RdioUser {
				Key = json.GetString ("key"),
				FirstName = json.GetString ("firstName"),
				LastName = json.GetString ("lastName"),
				AvatarUrl = json.GetString ("icon500"),
				CollectionStationKey = json.Has ("collectionKey") ? json.GetString ("collectionKey") : null
			};
		}

		public override int GetHashCode ()
		{
			return Key.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			var other = obj as RdioUser;
			return other != null && Equals (other);
		}

		public bool Equals (RdioUser other)
		{
			return string.Equals (other.Key, Key, StringComparison.OrdinalIgnoreCase);
		}
	}
}

