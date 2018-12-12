﻿using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace XAMLator.SampleApp
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();
			MainPage = new PageWithCSS();
		}

		protected override void OnStart()
		{
			XAMLator.Server.UpdateServer.Run();
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}
