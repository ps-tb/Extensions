// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Microsoft.JSInterop
{
    internal class DotNetObjectRefManager
    {
        private long _nextId = 0; // 0 signals no object, but we increment prior to assignment. The first tracked object should have id 1
        private readonly ConcurrentDictionary<long, object> _trackedRefsById = new ConcurrentDictionary<long, object>();

        public long TrackObject(object dotNetObjectRef)
        {
            var dotNetObjectId = Interlocked.Increment(ref _nextId);
            _trackedRefsById[dotNetObjectId] = dotNetObjectRef;

            return dotNetObjectId;
        }

        public object FindDotNetObject(long dotNetObjectId)
        {
            return _trackedRefsById.TryGetValue(dotNetObjectId, out var dotNetObjectRef)
                ? dotNetObjectRef
                : throw new ArgumentException($"There is no tracked object with id '{dotNetObjectId}'. Perhaps the DotNetObjectRef instance was already disposed.", nameof(dotNetObjectId));
            
        }

        /// <summary>
        /// Stops tracking the specified .NET object reference.
        /// This may be invoked either by disposing a DotNetObjectRef in .NET code, or via JS interop by calling "dispose" on the corresponding instance in JavaScript code
        /// </summary>
        /// <param name="dotNetObjectId">The ID of the <see cref="DotNetObjectRef{TValue}"/>.</param>
        public void ReleaseDotNetObject(long dotNetObjectId) => _trackedRefsById.TryRemove(dotNetObjectId, out _);
    }
}
