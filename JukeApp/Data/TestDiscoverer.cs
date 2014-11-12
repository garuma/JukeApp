using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JukeApp
{
	public class TestDiscoverer : IUserDiscoverer
	{
		string[] users = new string[] {
			"s2435501", // Alan
			"s1947100", // Me
			"s1328" // Eric
		};

		List<IObserver<string>> observers = new List<IObserver<string>> ();

		public async void StartDiscovery (CancellationToken token)
		{
			token.ThrowIfCancellationRequested ();
			for (int i = 0; i < users.Length; i++) {
				await Task.Delay (3000, token);
				foreach (var o in observers)
					o.OnNext (users[i]);
				token.ThrowIfCancellationRequested ();
			}
			token.ThrowIfCancellationRequested ();
		}

		public IDisposable Subscribe (IObserver<string> observer)
		{
			observers.Add (observer);
			return null;
		}
	}
}

