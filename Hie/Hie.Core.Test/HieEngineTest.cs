﻿using Hie.Core.Mocks;
using Hie.Core.Model;
using Hie.Core.Modules.JavaScript;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hie.Core
{
	[TestClass]
	public class HieEngineTest
	{
		[TestMethod]
		public void BasicRoutingFilteringTransformationTest()
		{
			// A new application
			Application application = new Application();

			// Add endpoints
			IEndpoint endpoint = new EndpointMock();
			application.Endpoints.Add(endpoint);

			IEndpoint sendEndpoint = new EndpointMock();
			application.Endpoints.Add(sendEndpoint);

			// Add a channel
			Channel channel = new Channel();
			application.Channels.Add(channel);

			// Source setup
			Source source = new Source();
			channel.Source = source;
			source.Filters.Add(new DelegateFilter((src, message) => true));
			source.Filters.Add(new JavaScriptFilter {Script = "true"});
			source.Transformers.Add(new DelegateTransformer());
			source.Transformers.Add(new DelegateTransformer((src, message) => { }));
			source.Transformers.Add(new DelegateTransformer((src, message) => { message.Value = message.Value; }));

			{
				Destination destination = new Destination();
				destination.Target = sendEndpoint;
				destination.Filters.Add(new DelegateFilter((src, message) => true));
				destination.Filters.Add(new JavaScriptFilter {Script = "true"});
				destination.Transformers.Add(new DelegateTransformer((src, message) => { }));
				destination.Transformers.Add(new DelegateTransformer((src, message) => { message.Value = message.Value; }));
				channel.Destinations.Add(destination);
			}
			{
				// This destination will filter out the message
				Destination destination = new Destination();
				destination.Target = sendEndpoint;
				destination.Filters.Add(new DelegateFilter((src, message) => false));
				channel.Destinations.Add(destination);
			}

			{
				// This destination will transform the message
				Destination destination = new Destination();
				destination.Target = sendEndpoint;
				destination.Filters.Add(new DelegateFilter((src, message) => true));
				destination.Transformers.Add(new DelegateTransformer((src, message) => { message.Value = message.Value + "test"; }));
				channel.Destinations.Add(destination);
			}

			// Host
			ApplicationHost applicationHost = new ApplicationHost();
			Assert.IsNotNull(applicationHost.Applications);
			applicationHost.Deploy(application);

			// Start the processing

			Message testMessage = new Message("text/json") {Value = TestUtils.BuildHl7JsonString()};
			// Mock method for sending a test message
			((EndpointMock) endpoint).SendTestMessage(testMessage);

			// Check that endpoint received the message
			EndpointMock endpointMock = sendEndpoint as EndpointMock;
			Assert.IsNotNull(endpointMock);
			Assert.IsNotNull(endpointMock.Messages);
			Assert.AreEqual(2, endpointMock.Messages.Count);
			foreach (Message message in endpointMock.Messages)
			{
				Assert.AreNotSame(testMessage, message);
				Assert.AreNotEqual(testMessage.Id, message.Id);
				if (message.Value.EndsWith("test"))
				{
					Assert.AreEqual(testMessage.Value + "test", message.Value);
				}
				else
				{
					Assert.AreEqual(testMessage.Value, message.Value);
				}
			}
		}
	}
}
