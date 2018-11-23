﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms.Xaml;

namespace XAMLator.Server
{
	/// <summary>
	/// Loads XAML views live with requests from the IDE.
	/// </summary>
	public class VM
	{
		static MethodInfo loadXAML;
		static EvalRequest currentEvalRequest;
		readonly object mutex = new object();
		IEvaluator evaluator;

		static VM()
		{
			ResolveLoadMethod();
			ReplaceResourcesProvider();
		}

		public VM()
		{
			evaluator = new Evaluator();
		}

		/// <summary>
		/// Hook used by new instances to load their XAML instead of retrieving
		/// it from the assembly.
		/// </summary>
		/// <param name="view">View.</param>
		public static void LoadXaml(object view)
		{
			loadXAML.Invoke(null, new object[] { view, currentEvalRequest?.Xaml });
		}

		public Task<EvalResult> Eval(EvalRequest code, TaskScheduler mainScheduler, CancellationToken token)
		{
			var tcs = new TaskCompletionSource<EvalResult>();
			var r = new EvalResult();
			lock (mutex)
			{
				Task.Factory.StartNew(async () =>
				{
					try
					{
						r = await EvalOnMainThread(code, token);
						tcs.SetResult(r);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				}, token, TaskCreationOptions.None, mainScheduler).Wait();
				return tcs.Task;
			}
		}

		async Task<EvalResult> EvalOnMainThread(EvalRequest code, CancellationToken token)
		{
			EvalResult evalResult = new EvalResult();

			var sw = new System.Diagnostics.Stopwatch();

			Log.Debug($"Evaluation request {code}");

			sw.Start();

			currentEvalRequest = code;
			await evaluator.EvaluateExpression(code.NewTypeExpression,
											   code.NeedsRebuild ? code.Declarations : null,
											   evalResult);

			if (evalResult.Result != null)
			{
				LoadXAML(evalResult.Result, code.Xaml, evalResult);
			}
			sw.Stop();

			Log.Debug($"Evaluation ended with result  {evalResult.Result}");

			evalResult.Duration = sw.Elapsed;
			return evalResult;
		}


		bool LoadXAML(object view, string xaml, EvalResult result)
		{
			Log.Information($"Loading XAML for type  {view}");
			try
			{
				loadXAML.Invoke(null, new object[] { view, xaml });
				Log.Debug($"XAML loaded correctly for view {view}");
				return true;
			}
			catch (TargetInvocationException ex)
			{
				Log.Error($"Error loading XAML");
				result.Messages = new EvalMessage[] { new EvalMessage ("error", ex.ToString())
				};
			}
			return false;
		}

		static void ResolveLoadMethod()
		{
			var asms = AppDomain.CurrentDomain.GetAssemblies();
			var xamlAssembly = Assembly.Load(new AssemblyName("Xamarin.Forms.Xaml"));
			var xamlLoader = xamlAssembly.GetType("Xamarin.Forms.Xaml.XamlLoader");
			loadXAML = xamlLoader.GetRuntimeMethod("Load", new[] { typeof(object), typeof(string) });
		}

		static void ReplaceResourcesProvider()
		{
			var asms = AppDomain.CurrentDomain.GetAssemblies();
			var xamlAssembly = Assembly.Load(new AssemblyName("Xamarin.Forms.Core"));
			var xamlLoader = xamlAssembly.GetType("Xamarin.Forms.Internals.ResourceLoader");
			var providerField = (xamlLoader as TypeInfo).DeclaredFields.Single(f => f.Name == "resourceProvider");
			providerField.SetValue(null, (Func<AssemblyName, string, string>)LoadResource);
		}

		static string LoadResource(AssemblyName assemblyName, string name)
		{
			Log.Information($"Resolving resource {name}");
			if (name == currentEvalRequest?.XamlResourceName)
			{
				return currentEvalRequest?.Xaml;
			}
			if (name.EndsWith(".css"))
			{
				return currentEvalRequest?.StyleSheets[name];
			}
			return null;
		}

		internal static string GetResourceIdForPath(Assembly assembly, string path)
		{
			foreach (var xria in assembly.GetCustomAttributes<XamlResourceIdAttribute>())
			{
				if (xria.Path == path)
					return xria.ResourceId;
			}
			return null;
		}

	}
}

