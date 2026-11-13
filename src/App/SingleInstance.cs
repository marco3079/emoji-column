// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace EmojiColumn.App
{
    /// <summary>
    /// When several files are selected, Explorer starts one process per file for
    /// a static context-menu verb. The first "pick" process becomes the primary,
    /// the others send it their file over a named pipe and exit, so the user gets
    /// a single picker window for the whole selection.
    /// </summary>
    internal sealed class SingleInstance : IDisposable
    {
        private static readonly string Suffix = Process.GetCurrentProcess().SessionId.ToString();
        private static readonly string MutexName = @"Local\EmojiColumn.Picker";
        private static readonly string PipeName = "EmojiColumn.Picker." + Suffix;

        private readonly Mutex _mutex;
        private volatile bool _stopped;

        private readonly object _lock = new object();
        private readonly List<string> _pending = new List<string>();
        private Action<string[]> _handler;

        /// <summary>
        /// Sets the callback (invoked on a worker thread) for files sent by other
        /// instances. Files that arrived before this call are delivered at once.
        /// </summary>
        public void OnFilesReceived(Action<string[]> handler)
        {
            string[] early;
            lock (_lock)
            {
                _handler = handler;
                early = _pending.ToArray();
                _pending.Clear();
            }
            if (early.Length > 0) handler(early);
        }

        private void Deliver(string[] files)
        {
            Action<string[]> handler;
            lock (_lock)
            {
                handler = _handler;
                if (handler == null) { _pending.AddRange(files); return; }
            }
            handler(files);
        }

        private SingleInstance(Mutex mutex)
        {
            _mutex = mutex;
        }

        /// <summary>
        /// Returns the primary instance, or null when the files were handed to an
        /// already running picker (the caller should then just exit).
        /// </summary>
        public static SingleInstance TryBecomePrimary(IList<string> files)
        {
            bool created;
            Mutex mutex = new Mutex(true, MutexName, out created);
            if (created)
            {
                SingleInstance primary = new SingleInstance(mutex);
                Thread t = new Thread(primary.Listen);
                t.IsBackground = true;
                t.Start();
                return primary;
            }
            mutex.Dispose();

            try
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(3000);
                    byte[] data = Encoding.UTF8.GetBytes(string.Join("\n", files));
                    client.Write(data, 0, data.Length);
                }
                return null;
            }
            catch (Exception)
            {
                // The primary is closing: show our own picker instead.
                return new SingleInstance(null);
            }
        }

        private void Listen()
        {
            while (!_stopped)
            {
                try
                {
                    using (NamedPipeServerStream server = new NamedPipeServerStream(PipeName, PipeDirection.In,
                               NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte))
                    {
                        server.WaitForConnection();
                        if (_stopped) return;
                        using (StreamReader reader = new StreamReader(server, Encoding.UTF8))
                        {
                            string[] files = reader.ReadToEnd().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            if (files.Length > 0) Deliver(files);
                        }
                    }
                }
                catch (Exception)
                {
                    Thread.Sleep(50);
                }
            }
        }

        public void Dispose()
        {
            _stopped = true;
            if (_mutex != null)
            {
                try { _mutex.ReleaseMutex(); } catch (Exception) { }
                _mutex.Dispose();
            }
        }
    }
}
