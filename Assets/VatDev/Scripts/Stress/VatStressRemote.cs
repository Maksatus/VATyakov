using System.Text;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace VATyakov.Dev
{
    public sealed class VatStressRemote : MonoBehaviour
    {
        [SerializeField]
        private VatStress _stress;

        [SerializeField]
        private Camera _camera;

        private void OnEnable()
        {
            PlayerConnection.instance.Register(VatStressMessages.List, OnList);
            PlayerConnection.instance.Register(VatStressMessages.Select, OnSelect);
            PlayerConnection.instance.Register(VatStressMessages.Info, OnInfo);
        }

        private void OnDisable()
        {
            PlayerConnection.instance.Unregister(VatStressMessages.List, OnList);
            PlayerConnection.instance.Unregister(VatStressMessages.Select, OnSelect);
            PlayerConnection.instance.Unregister(VatStressMessages.Info, OnInfo);
        }

        private void OnList(MessageEventArgs args)
        {
            var names = new string[VatStressVariant.All.Length];
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = VatStressVariant.All[i].Name;
            }

            Reply(ReadRequest(args), Join(names));
        }

        private void OnSelect(MessageEventArgs args)
        {
            var request = ReadRequest(args);
            var variant = VatStressVariant.Find(request[1]);
            if (variant == null)
            {
                Send(request, $"unknown variant '{request[1]}'");
                return;
            }

            _stress.Select(variant);
            Reply(request, string.Empty);
        }

        private void OnInfo(MessageEventArgs args)
        {
            Reply(ReadRequest(args), Join(VatStressInfo.Describe(_stress, _camera).ToArray()));
        }

        private static string[] ReadRequest(MessageEventArgs args)
        {
            return Encoding.UTF8.GetString(args.data).Split(new[] { VatStressMessages.Separator }, 2);
        }

        private static string Join(string[] lines)
        {
            return string.Join(VatStressMessages.Separator.ToString(), lines);
        }

        private static void Reply(string[] request, string body)
        {
            Send(request, VatStressMessages.SuccessReply + VatStressMessages.Separator + body);
        }

        private static void Send(string[] request, string text)
        {
            PlayerConnection.instance.Send(VatStressMessages.Reply, Encoding.UTF8.GetBytes(request[0] + VatStressMessages.Separator + text));
        }
    }
}
