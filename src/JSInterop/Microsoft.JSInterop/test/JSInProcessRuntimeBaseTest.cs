// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.JSInterop.Tests
{
    public class JSInProcessRuntimeBaseTest
    {
        [Fact]
        public void DispatchesSyncCallsAndDeserializesResults()
        {
            // Arrange
            var runtime = new TestJSInProcessRuntime
            {
                NextResultJson = "{\"intValue\":123,\"stringValue\":\"Hello\"}"
            };
            JSRuntime.SetCurrentJSRuntime(runtime);

            // Act
            var syncResult = runtime.Invoke<TestDTO>("test identifier 1", "arg1", 123, true);
            var call = runtime.InvokeCalls.Single();

            // Assert
            Assert.Equal(123, syncResult.IntValue);
            Assert.Equal("Hello", syncResult.StringValue);
            Assert.Equal("test identifier 1", call.Identifier);
            Assert.Equal("[\"arg1\",123,true]", call.ArgsJson);
        }

        [Fact]
        public void SerializesDotNetObjectWrappersInKnownFormat()
        {
            // Arrange
            var runtime = new TestJSInProcessRuntime { NextResultJson = null };
            JSRuntime.SetCurrentJSRuntime(runtime);
            var obj1 = new object();
            var obj2 = new object();
            var obj3 = new object();

            // Act
            // Showing we can pass the DotNetObject either as top-level args or nested
            var syncResult = runtime.Invoke<DotNetObjectRef<object>>("test identifier",
                DotNetObjectRef.Create(obj1),
                new Dictionary<string, object>
                {
                    { "obj2",  DotNetObjectRef.Create(obj2) },
                    { "obj3",  DotNetObjectRef.Create(obj3) },
                });

            // Assert: Handles null result string
            Assert.Null(syncResult);

            // Assert: Serialized as expected
            var call = runtime.InvokeCalls.Single();
            Assert.Equal("test identifier", call.Identifier);
            Assert.Equal("[{\"__dotNetObject\":1},{\"obj2\":{\"__dotNetObject\":2},\"obj3\":{\"__dotNetObject\":3}}]", call.ArgsJson);

            // Assert: Objects were tracked
            Assert.Same(obj1, runtime.ObjectRefManager.FindDotNetObject(1));
            Assert.Same(obj2, runtime.ObjectRefManager.FindDotNetObject(2));
            Assert.Same(obj3, runtime.ObjectRefManager.FindDotNetObject(3));
        }

        [Fact]
        public void SyncCallResultCanIncludeDotNetObjects()
        {
            // Arrange
            var runtime = new TestJSInProcessRuntime
            {
                NextResultJson = "[{\"__dotNetObject\":2},{\"__dotNetObject\":1}]"
            };
            JSRuntime.SetCurrentJSRuntime(runtime);
            var obj1 = new object();
            var obj2 = new object();

            // Act
            var syncResult = runtime.Invoke<DotNetObjectRef<object>[]>(
                "test identifier",
                DotNetObjectRef.Create(obj1),
                "some other arg",
                DotNetObjectRef.Create(obj2));
            var call = runtime.InvokeCalls.Single();

            // Assert
            Assert.Equal(new[] { obj2, obj1 }, syncResult.Select(r => r.Value));
        }

        class TestDTO
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        class TestJSInProcessRuntime : JSInProcessRuntimeBase
        {
            public List<InvokeArgs> InvokeCalls { get; set; } = new List<InvokeArgs>();

            public string NextResultJson { get; set; }

            protected override string InvokeJS(string identifier, string argsJson)
            {
                InvokeCalls.Add(new InvokeArgs { Identifier = identifier, ArgsJson = argsJson });
                return NextResultJson;
            }

            public class InvokeArgs
            {
                public string Identifier { get; set; }
                public string ArgsJson { get; set; }
            }

            protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
                => throw new NotImplementedException("This test only covers sync calls");

            protected internal override void EndInvokeDotNet(string callId, bool success, object resultOrError, string assemblyName, string methodIdentifier, long dotNetObjectId) =>
                throw new NotImplementedException("This test only covers sync calls");
        }
    }
}
