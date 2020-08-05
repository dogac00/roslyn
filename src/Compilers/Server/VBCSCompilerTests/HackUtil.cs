// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Net.Sockets;
using Roslyn.Test.Utilities;
using Xunit;

#nullable enable

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    /// <summary>
    /// This is a HACK that allows you to get at the underlying Socket for a given NamedPipeServerStream 
    /// instance. It is very useful it proactively diagnosing bugs in the server code by letting us inspect
    /// the socket to see if it's disposed, not available, etc ... vs. experiencing flaky bugs that are 
    /// incredibly difficult to track down.
    ///
    /// Do NOT check this into production, it's a unit test utility only
    /// </summary>
    internal static class HackUtil
    {
        private static IDictionary GetSharedServersDictionary()
        {
            var sharedServerFullName = typeof(NamedPipeServerStream).FullName + "+SharedServer";
            var sharedServerType = typeof(NamedPipeServerStream).Assembly.GetType(sharedServerFullName);
            var serversField = sharedServerType?.GetField("s_servers", BindingFlags.NonPublic | BindingFlags.Static);
            var servers = (IDictionary?)serversField?.GetValue(null);
            if (servers is null)
            {
                throw new Exception("Cannot locate the SharedServer dictionary");
            }

            return servers;
        }

        private static Socket GetSocket(object sharedServer)
        {
            var listeningSocketProperty = sharedServer!.GetType()?.GetProperty("ListeningSocket", BindingFlags.NonPublic | BindingFlags.Instance);
            var socket = (Socket?)listeningSocketProperty?.GetValue(sharedServer, null);
            if (socket is null)
            {
                throw new Exception("Socket is unexpectedly null");
            }

            return socket;
        }

        private static Socket? GetSocketForPipeName(string pipeName)
        {
            if (!ExecutionConditionUtil.IsUnix || !ExecutionConditionUtil.IsCoreClr)
            {
                return null;
            }

            pipeName = "/tmp/" + pipeName;
            var servers = GetSharedServersDictionary();
            lock (servers)
            {
                if (!servers.Contains(pipeName))
                {
                    return null;
                }

                var sharedServer = servers[pipeName];
                Debug.Assert(sharedServer is object);
                return GetSocket(sharedServer);
            }
        }

        internal static void AssertPipeFullyClosed(string pipeName)
        {
            var socket = GetSocketForPipeName(pipeName);
            Assert.Null(socket);
        }

        internal static void DisposeAll()
        {
            if (!ExecutionConditionUtil.IsUnix || !ExecutionConditionUtil.IsCoreClr)
            {
                return;
            }

            var list = new List<(string PipeName, Socket Socket)>();
            var servers = GetSharedServersDictionary();

            lock (servers)
            {
                var e = servers.GetEnumerator();
                while (e.MoveNext())
                {
                    var sharedServer = e.Value!;
                    var socket = GetSocket(sharedServer);
                    socket.Dispose();
                    list.Add(((string)e.Key!, socket));
                }

                servers.Clear();
            }
        }
    }
}
