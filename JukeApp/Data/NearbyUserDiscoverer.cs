using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

using Android.Content;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Java.Util;
using Android.OS;

namespace JukeApp
{
	public class NearbyUserDiscoverer: ScanCallback, IUserDiscoverer
	{
		public static readonly Android.OS.ParcelUuid ServiceUUid =
			new Android.OS.ParcelUuid (UUID.FromString ("00300508-0020-8000-8000-00805F9B34FB"));
		BluetoothAdapter adapter;
		Handler handler;

		List<IObserver<string>> observers = new List<IObserver<string>> ();

		public NearbyUserDiscoverer ()
		{
			adapter = BluetoothAdapter.DefaultAdapter;
			handler = new Handler ();
		}

		public async void StartDiscovery (CancellationToken token)
		{
			var advertiser = adapter.BluetoothLeAdvertiser;
			var scanner = adapter.BluetoothLeScanner;

			var callback = new FakeAdvertiseCallback ();
			var data = new AdvertiseData.Builder ()
				.AddServiceUuid (ServiceUUid)
				.Build ();
			var settings = new AdvertiseSettings.Builder ()
				.SetAdvertiseMode (AdvertiseMode.Balanced)
				.SetConnectable (false)
				.Build ();
			var scanFilter = new ScanFilter.Builder ()
				.SetServiceUuid (ServiceUUid)
				.Build ();
			var filters = new ScanFilter[] { scanFilter };
			var scanSettings = new ScanSettings.Builder ()
				.SetScanMode (Android.Bluetooth.LE.ScanMode.Balanced)
				.SetReportDelay (3000)
				.Build ();

			while (!token.IsCancellationRequested) {
				// First advertise data for a few seconds to wake up clients
				advertiser.StartAdvertising (settings, data, callback);
				await Task.Delay (TimeSpan.FromSeconds (5), token);
				advertiser.StopAdvertising (callback);

				// Then try to gather a round of users
				scanner.StartScan (filters, scanSettings, this);
				await Task.Delay (TimeSpan.FromSeconds (10), token);
				scanner.StopScan (this);
			}

			token.ThrowIfCancellationRequested ();
		}

		public override void OnBatchScanResults (System.Collections.Generic.IList<ScanResult> results)
		{
			foreach (var result in results)
				OnScanResult (ScanCallbackType.AllMatches, result);
		}

		public override void OnScanFailed (ScanFailure errorCode)
		{
			base.OnScanFailed (errorCode);
		}

		public override void OnScanResult (ScanCallbackType callbackType, ScanResult result)
		{
			var record = result.ScanRecord;
			if (record == null || !record.ServiceData.ContainsKey (ServiceUUid))
				return;
			var userKey = Encoding.UTF8.GetString (record.ServiceData [ServiceUUid]);
			handler.Post (() => {
				lock (observers)
					foreach (var observer in observers)
						observer.OnNext (userKey);
			});
		}

		public IDisposable Subscribe (IObserver<string> observer)
		{
			lock (observers)
				observers.Add (observer);
			return null;
		}

		class FakeAdvertiseCallback : AdvertiseCallback
		{

		}
	}
}

