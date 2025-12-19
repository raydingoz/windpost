using System;

namespace Windpost.Bootstrap
{
    public static class ModeSelector
    {
        private const string VrArg = "-vr";
        private const string NoVrArg = "-novr";

        public static GameMode DetermineMode()
        {
            var args = Environment.GetCommandLineArgs();

            if (HasArg(args, NoVrArg))
            {
                return GameMode.Desktop;
            }

            if (HasArg(args, VrArg))
            {
                return GameMode.VR;
            }

            return GameMode.Desktop;
        }

        public static bool IsNoVr()
        {
            var args = Environment.GetCommandLineArgs();
            return HasArg(args, NoVrArg);
        }

        private static bool HasArg(string[] args, string flag)
        {
            if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(flag))
            {
                return false;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

