// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Wraps a JS interop argument, indicating that the value should not be serialized as JSON
    /// but instead should be passed as a reference.
    ///
    /// To avoid leaking memory, the reference must later be disposed by JS code or by .NET code.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to wrap.</typeparam>
    [JsonConverter(typeof(DotNetObjectReferenceJsonConverterFactory))]
    public sealed class DotNetObjectRef<TValue> : IDotNetObjectRef, IDisposable where TValue : class
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DotNetObjectRef{TValue}" />.
        /// </summary>
        /// <param name="value">The value to pass by reference.</param>
        internal DotNetObjectRef(TValue value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the object instance represented by this wrapper.
        /// </summary>
        public TValue Value { get; }

        internal JSRuntimeBase JSRuntime { get; private set; }

        private long ObjectId { get; set; }

        internal long TrackUsing(JSRuntimeBase jsRuntime)
        {
            if (JSRuntime != null)
            {
                if (!ReferenceEquals(JSRuntime, jsRuntime))
                {
                    throw new InvalidOperationException("DotnetObjectRef is already being tracked by a different JSRuntime instance.");
                }

                Debug.Assert(ObjectId != 0, "Object must already be tracked");
                return ObjectId;
            }

            JSRuntime = jsRuntime;
            ObjectId = jsRuntime.ObjectRefManager.TrackObject(Value);

            return ObjectId;
        }
            
        /// <summary>
        /// Stops tracking this object reference, allowing it to be garbage collected
        /// (if there are no other references to it). Once the instance is disposed, it
        /// can no longer be used in interop calls from JavaScript code.
        /// </summary>
        public void Dispose()
        {
            if (JSRuntime != null)
            {
                JSRuntime.ObjectRefManager.ReleaseDotNetObject(ObjectId);

                JSRuntime = null;
                ObjectId = 0;
            }
        }
    }
}
