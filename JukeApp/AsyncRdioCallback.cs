using System;
using Rdio.Android.Sdk;
using Rdio.Android.Sdk.Services;
using System.Threading.Tasks;

namespace JukeApp
{
	class AsyncRdioCallback : RdioApiJsonResponse
	{
		TaskCompletionSource<Org.Json.JSONObject> completion = new TaskCompletionSource<Org.Json.JSONObject> ();

		public void Reset ()
		{
			completion = new TaskCompletionSource<Org.Json.JSONObject> ();
		}

		public Task<Org.Json.JSONObject> WaitForResultAsync ()
		{
			return completion.Task;
		}

		public override void OnApiSuccess (Org.Json.JSONObject jsonObject)
		{
			completion.TrySetResult (jsonObject);
		}

		public override void OnApiFailure (string message, Java.Lang.Exception exception)
		{
			Android.Util.Log.Error ("AsyncRdioCallback", exception.ToString ());
			completion.TrySetException (new RdioApiException (message));
		}
	}

	public class RdioApiException : Exception
	{
		public RdioApiException (string message) : base (message)
		{

		}
	}
}

