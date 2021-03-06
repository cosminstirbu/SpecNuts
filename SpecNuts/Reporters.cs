﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SpecNuts.Model;
using TechTalk.SpecFlow;

namespace SpecNuts
{
	[Binding]
	public static partial class Reporters
	{
		#region Private/Internal

		private static readonly List<Reporter> reporters = new List<Reporter>();

		/// <summary>
		///     Returns the current date/time which is used during the test run. It can set to a fixed
		///     datetime by <see cref="FixedRunTime" />
		/// </summary>
		internal static DateTime CurrentRunTime
		{
			get
			{
				if (FixedRunTime.HasValue)
				{
					return FixedRunTime.Value;
				}
				return DateTime.Now;
			}
		}

		internal static Step CreateStep(DateTime starttime, MethodBase method, params object[] args)
		{
			var methodName = method.Name;

			var step = new Step
			{
				Name = ScenarioStepContext.Current.StepInfo.Text,
				StartTime = starttime,
                Keyword = ScenarioContext.Current.CurrentScenarioBlock + " ",
                Id = ScenarioStepContext.Current.StepInfo.Text.Replace(" ", "-").ToLower()
            };

			var attr = method.GetCustomAttributes(true).OfType<StepDefinitionBaseAttribute>().FirstOrDefault();
			if (attr != null)
			{
				// Handle regex style
				if (!String.IsNullOrEmpty(attr.Regex))
				{
					step.Name = attr.Regex;

					for (var i = 0; i < args.Length; i++)
					{
						var arg = args[i];
						var table = arg as Table;
						if (table != null)
						{

						    step.Rows = new List<Row> {new Row() {Cells = table.Header.ToList()}};

						    foreach (var tableRow in table.Rows)
						    {
                                step.Rows.Add(new Row() { Cells = tableRow.Select(x => x.Value).ToList()
                                });
                            }
						}
						else
						{
							var titleRegex = new Regex(step.Name);
							var match = titleRegex.Match(step.Name);
							if (match.Groups.Count > 1)
							{
								step.Name = step.Name.ReplaceFirst(match.Groups[1].Value, args[i].ToString());
							}
							else
							{
								step.MultiLineParameter = args[i].ToString();
							}
						}
					}
				}
				else
				{
					if (methodName.Contains('_'))
					{
						// underscore style
						step.Name = methodName.Replace("_", " ");
						step.Name = step.Name.Substring(step.Name.IndexOf(' ') + 1);

						var methodInfo = method as MethodInfo;
						for (var i = 0; i < args.Length; i++)
						{
							var arg = args[i];
							var table = arg as Table;
							if (table != null)
							{
							    step.Rows = new List<Row> {new Row() {Cells = table.Header.ToList()}};

                                foreach (var tableRow in table.Rows)
                                {
                                    step.Rows.Add(new Row()
                                    {
                                        Cells = tableRow.Select(x => x.Value).ToList()
                                    });
                                }
							}
							else
							{
								var name = methodInfo.GetParamName(i).ToUpper();
								var value = arg.ToString();
								if (step.Name.Contains(name + " "))
								{
									step.Name = step.Name.ReplaceFirst(name + " ", value + " ");
								}
								else
								{
									step.Name = step.Name.ReplaceFirst(" " + name, " " + value);
								}
							}
						}
					}
					else
					{
						// pascal naming style
						throw new NotSupportedException("Pascal naming style not supported yet");
					}
				}
			}

		    step.Name = ScenarioStepContext.Current.StepInfo.Text;

			return step;
		}

		internal static void ExecuteStep(Action action, params object[] args)
		{
			ExecuteStep(action, null, args);
		}

		internal static void ExecuteStep(Action action, MethodBase methodBase, params object[] args)
		{
			methodBase = methodBase ?? action.Method;

			var currentSteps = new Dictionary<Reporter, Step>();

			var starttime = CurrentRunTime;
			foreach (var reporter in GetAll())
			{
				currentSteps.Add(reporter, reporter.CurrentStep);

				var step = CreateStep(starttime, methodBase, args);

				var stepContainer = reporter.CurrentScenario;
				stepContainer.Steps.Add(step);
				reporter.CurrentStep = step;
				OnStartedStep(reporter);
			}

			Exception actionException = null;
			try
			{
				if (!action.Method.GetParameters().Any())
				{
					action.Method.Invoke(action.Target, null);
				}
				else
				{
					action.Method.Invoke(action.Target, args);
				}
			}
			catch (Exception ex)
			{
				if (ex is TargetInvocationException && ex.InnerException != null)
				{
					// Exceptions thrown by ReportingMessageSink are wrapped in a TargetInvocationException
					actionException = ex.InnerException;
				}
				else
				{
					actionException = ex;
				}
			}
			finally
			{
				var endtime = CurrentRunTime;

				TestResult testResult;
				if (actionException is PendingStepException)
				{
					testResult = TestResult.pending;
				}
				else if (actionException != null)
				{
					testResult = TestResult.failed;
				}
				else
				{
					testResult = TestResult.passed;
				}


                foreach (var reporter in GetAll())
				{
					reporter.CurrentStep.EndTime = endtime;
				    reporter.CurrentStep.Result = new StepResult
				    {
				        Duration =
				            (long) ((endtime - reporter.CurrentStep.StartTime).TotalMilliseconds*1000000),
				        Status = testResult,
				        Error = actionException != null ? actionException.ToExceptionInfo().Message : string.Empty
				    };

					OnFinishedStep(reporter);

					reporter.CurrentStep = currentSteps[reporter];
				}
			}
		}

		#endregion Private/Internal

		#region Public

		/// <summary>
		///     Set fixed start and end times. Usefull for automated tests.
		/// </summary>
		public static DateTime? FixedRunTime { get; set; }

		public static Reporter Add(Reporter reporter)
		{
			reporters.Add(reporter);
			return reporter;
		}

		public static IEnumerable<Reporter> GetAll()
		{
			return reporters;
		}

		#endregion Public
	}
}