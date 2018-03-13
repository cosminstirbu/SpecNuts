using System;
using System.Threading.Tasks;
using SpecNuts.ReportingAspect;

namespace SpecNuts
{
	[Reporting]
	public abstract class ReportingStepDefinitions : ContextBoundObject
	{
		public async Task ReportStep(Func<Task> stepFunc, params object[] args)
		{
			await Reporters.ExecuteStep(stepFunc, args);
		}
	}
}