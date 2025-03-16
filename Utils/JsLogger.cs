using Microsoft.JSInterop;

namespace CycleCalculatorWeb.Utils
{
	public static class JsLogger
	{
		public static IJSRuntime JsRuntime;

		public static void Log(string message)
		{
			JsRuntime.InvokeVoidAsync(message);
		}
	}
}
