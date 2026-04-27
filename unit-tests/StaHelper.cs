using System.Runtime.ExceptionServices;

namespace unit_tests;

internal static class StaHelper {
	public static void RunSta(Action action) {
		ExceptionDispatchInfo? captured = null;
		Thread thread = new(() => {
			try {
				action();
			} catch (Exception e) {
				captured = ExceptionDispatchInfo.Capture(e);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Start();
		thread.Join();
		captured?.Throw();
	}
}
