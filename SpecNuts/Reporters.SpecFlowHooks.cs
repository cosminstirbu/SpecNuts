﻿using System.Collections.Generic;
using System.Linq;
using SpecNuts.Model;
using TechTalk.SpecFlow;

namespace SpecNuts
{
	public static partial class Reporters
	{
		private static bool _testrunIsFirstFeature;

		[AfterFeature]
		internal static void AfterFeature()
		{
			foreach (var reporter in reporters)
			{
				var feature = reporter.CurrentFeature;

				var scenarioOutlineGroups = feature.Elements.GroupBy(scenario => scenario.Name)
					.Where((scenarioGrp, key) => scenarioGrp.Count() > 1)
					.Select((scenarioGrp, key) => scenarioGrp.ToList());

				foreach (var scenarioOutlineGroup in scenarioOutlineGroups)
				{
					for (int i = 0; i < scenarioOutlineGroup.Count(); i++)
					{
						scenarioOutlineGroup[i].Name = string.Format("{0} (example {1})", scenarioOutlineGroup[i].Name, i + 1);
					}
				}

				feature.Result = feature.Elements.Exists(o => o.Result == TestResult.failed)
					? TestResult.failed
					: TestResult.passed;

				feature.EndTime = CurrentRunTime;
				OnFinishedFeature(reporter);
				reporter.CurrentFeature = null;
			}
		}

		[AfterScenario]
		internal static void AfterScenario()
		{
			foreach (var reporter in reporters.ToArray())
			{
				var scenario = reporter.CurrentScenario;
				scenario.EndTime = CurrentRunTime;
				scenario.Result = scenario.Steps.Exists(o => o.Result.Status == TestResult.failed)
					? TestResult.failed
					: TestResult.passed;
				OnFinishedScenario(reporter);
				reporter.CurrentScenario = null;
			}
		}

		[AfterTestRun]
		internal static void AfterTestRun()
		{
			foreach (var reporter in reporters)
			{
				reporter.Report.EndTime = CurrentRunTime;
				OnFinishedReport(reporter);
			}
		}

		[BeforeFeature]
		internal static void BeforeFeature()
		{
			var starttime = CurrentRunTime;

			// Init reports when the first feature runs. This is intentionally not done in
			// BeforeTestRun(), to make sure other [BeforeTestRun] annotated methods can perform
			// initialization before the reports are created.
			if (_testrunIsFirstFeature)
			{
				foreach (var reporter in reporters)
				{
					reporter.Report = new Report
					{
						Features = new List<Feature>(),
						StartTime = starttime
					};

					OnStartedReport(reporter);
				}

				_testrunIsFirstFeature = false;
			}

			foreach (var reporter in reporters)
			{
				var featureId = FeatureContext.Current.FeatureInfo.Title.Replace(" ", "-").ToLower();
				var feature = new Feature
				{
					Tags = FeatureContext.Current.FeatureInfo.Tags.Select(tag => new Tag() { Name = "@" + tag }).ToList(),
					Elements = new List<Scenario>(),
					StartTime = starttime,
					Name = FeatureContext.Current.FeatureInfo.Title,
					Description = FeatureContext.Current.FeatureInfo.Description,
					Id = featureId,
					Uri = $"/{featureId}"
				};

				reporter.Report.Features.Add(feature);
				reporter.CurrentFeature = feature;

				OnStartedFeature(reporter);
			}
		}

		[BeforeScenario]
		internal static void BeforeScenario()
		{
			var starttime = CurrentRunTime;

			foreach (var reporter in reporters)
			{
				var scenario = new Scenario
				{
					Tags = ScenarioContext.Current.ScenarioInfo.Tags.Select(tag => new Tag() { Name = "@" + tag }).ToList(),
					StartTime = starttime,
					Name = ScenarioContext.Current.ScenarioInfo.Title,
					Steps = new List<Step>(),
					Description = ScenarioContext.Current.ScenarioInfo.Title
				};

				reporter.CurrentFeature.Elements.Add(scenario);
				reporter.CurrentScenario = scenario;

				OnStartedScenario(reporter);
			}
		}

		[BeforeTestRun]
		internal static void BeforeTestRun()
		{
			_testrunIsFirstFeature = true;
		}
	}
}