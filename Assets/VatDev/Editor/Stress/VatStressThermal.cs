using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace VATyakov.Dev
{
    internal sealed class VatStressThermal
    {
        private const double PollSeconds = 5d;

        private readonly Action<string> _log;
        private readonly Stopwatch _sincePoll = new();

        private bool _isUnavailable;

        public VatStressThermal(Action<string> log)
        {
            _log = log;
        }

        public async Task<VatThermalState> Check(string where)
        {
            var state = await Read();
            _sincePoll.Restart();
            if (state.IsThrottled)
            {
                throw new VatThrottlingException(FormattableString.Invariant(
                    $"Thermal Status {state.Status} at {where} (battery {state.Battery:0.0} C, SoC {state.Soc:0.0} C, skin {state.Skin:0.0} C)"));
            }

            return state;
        }

        public async Task Poll(string where)
        {
            if (_sincePoll.IsRunning && _sincePoll.Elapsed.TotalSeconds < PollSeconds)
            {
                return;
            }

            await Check(where);
        }

        private async Task<VatThermalState> Read()
        {
            if (_isUnavailable)
            {
                return VatThermalState.Unknown;
            }

            try
            {
                return await VatAndroidThermal.Read();
            }
            catch (Exception exception)
            {
                _isUnavailable = true;
                _log($"Thermal Status is not watched: {exception.Message}");
                return VatThermalState.Unknown;
            }
        }
    }
}
