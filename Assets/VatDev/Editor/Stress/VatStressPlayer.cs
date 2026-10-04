using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace VATyakov.Dev
{
    internal sealed class VatStressPlayer : ScriptableObject
    {
        private const int ReplyTimeoutMs = 30000;

        private int _lastRequestId;
        private string _pendingRequestId;
        private TaskCompletionSource<string> _reply;

        public static VatStressPlayer Create()
        {
            var player = CreateInstance<VatStressPlayer>();
            player.hideFlags = HideFlags.HideAndDontSave;
            return player;
        }

        public async Task<string[]> List(CancellationToken token)
        {
            return await Send(VatStressMessages.List, string.Empty, token);
        }

        public Task Select(string variant, CancellationToken token)
        {
            return Send(VatStressMessages.Select, variant, token);
        }

        public async Task<Dictionary<string, string>> Info(CancellationToken token)
        {
            var lines = await Send(VatStressMessages.Info, string.Empty, token);
            return lines.Select(line => line.Split(new[] { VatStressMessages.KeySeparator }, 2)).ToDictionary(pair => pair[0], pair => pair[1]);
        }

        private void OnEnable()
        {
            EditorConnection.instance.Initialize();
            EditorConnection.instance.Register(VatStressMessages.Reply, OnReply);
        }

        private void OnDisable()
        {
            EditorConnection.instance.Unregister(VatStressMessages.Reply, OnReply);
        }

        private async Task<string[]> Send(Guid message, string argument, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (EditorConnection.instance.ConnectedPlayers.Count == 0)
            {
                throw new InvalidOperationException("No player is connected: attach the Profiler to the device.");
            }

            _lastRequestId++;
            _pendingRequestId = _lastRequestId.ToString(CultureInfo.InvariantCulture);
            _reply = new TaskCompletionSource<string>();
            var reply = _reply.Task;
            EditorConnection.instance.Send(message, Encoding.UTF8.GetBytes(_pendingRequestId + VatStressMessages.Separator + argument));
            if (await Task.WhenAny(reply, Task.Delay(ReplyTimeoutMs, token)) != reply)
            {
                token.ThrowIfCancellationRequested();
                throw new TimeoutException("The player does not answer: open the 'stress' scene on the device.");
            }

            var lines = reply.Result.Split(VatStressMessages.Separator);
            if (lines[0] != VatStressMessages.SuccessReply)
            {
                throw new InvalidOperationException($"The player answered: {reply.Result}");
            }

            return lines.Skip(1).Where(line => line.Length > 0).ToArray();
        }

        private void OnReply(MessageEventArgs args)
        {
            var parts = Encoding.UTF8.GetString(args.data).Split(new[] { VatStressMessages.Separator }, 2);
            if (parts[0] == _pendingRequestId)
            {
                _reply.TrySetResult(parts[1]);
            }
        }
    }
}
