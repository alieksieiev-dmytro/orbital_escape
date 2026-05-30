using System;
using UnityEngine;

namespace OrbitalEscape.Input
{
    public sealed class ExitCommand : Command
    {
        public override IConvertible Type => CommandType.Exit;

        public override void Execute()
        {
            Application.Quit();
        }
    }
}
