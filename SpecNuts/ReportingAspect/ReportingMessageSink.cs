using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

namespace SpecNuts.ReportingAspect
{
	internal class ReportingMessageSink : IMessageSink
	{
		public ReportingMessageSink(IMessageSink next)
		{
			NextSink = next;
		}

		public IMessageSink NextSink { get; private set; }

		public IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
		{
			var rtnMsgCtrl = NextSink.AsyncProcessMessage(msg, replySink);
			return rtnMsgCtrl;
		}

		public IMessage SyncProcessMessage(IMessage msg)
		{
			var methodMessage = new MethodCallMessageWrapper((IMethodCallMessage)msg);

			// Avoid reporting proxy method calls for fields injected via the constructor
			// in the step definition classes. This caused some steps to pe reported twice.
			if (methodMessage.MethodName == "FieldGetter")
			{
				return NextSink.SyncProcessMessage(msg);
			}

			IMethodReturnMessage mrm = null;
			var task = Reporters.ExecuteStep(
				() =>
				{
					mrm = (IMethodReturnMessage)NextSink.SyncProcessMessage(msg);

					if (mrm.Exception != null)
					{
						return Task.FromException(mrm.Exception);
					}

					return (mrm.ReturnValue as Task) ?? Task.CompletedTask;
				},
				methodMessage.MethodBase,
				methodMessage.Args
				);

			if (task.IsCompleted)
			{
				return mrm;
			}
			else
			{
				return new ReturnMessage(task, null, 0, mrm.LogicalCallContext, methodMessage);
			}
		}
	}
}