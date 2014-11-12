using System;
using System.Threading;

namespace JukeApp
{
	public interface IUserDiscoverer: IObservable<string>
	{
		void StartDiscovery (CancellationToken token);
	}
}

