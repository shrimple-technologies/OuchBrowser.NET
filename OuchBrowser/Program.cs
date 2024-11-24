using Gio;
using System;
using Application = Adw.Application;

namespace OuchBrowser;

internal class Program
{
	private static int Main(string[] args)
	{
		var app =
			Application.New(
				"site.srht.shrimple.ouch",
				ApplicationFlags.FlagsNone
			);
		var window = new Window();

		app.OnStartup += window.OnStartup;
		app.OnActivate += window.OnActivate;

		return app.RunWithSynchronizationContext(null);
	}
}
