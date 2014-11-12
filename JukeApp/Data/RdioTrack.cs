using System;

namespace JukeApp
{
	public class RdioTrack
	{
		public RdioUser TrackFromUser { get; private set; }
		public string TrackKey { get; private set; }

		public RdioTrack (RdioUser fromUser, string trackKey)
		{
			TrackFromUser = fromUser;
			TrackKey = trackKey;
		}
	}
}

