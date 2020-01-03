using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.App
{
    public class Options
    {
        [Option('t', "type", Required = true,
            HelpText = "Transfer Manager type.")]
        public string TransferManager { get; set; }

        [Option("tp", Required = true,
            HelpText = "Transfer Manager type parameter.")]
        public string TransferManagerParameter { get; set; }

        [Option('i', "inputPlugin", Required = true,
            HelpText = "Input Plugin")]
        public string InputPlugin { get; set; }

        [Option("ip", Required = true,
            HelpText = "Input plugin parameters.")]
        public string InputPluginParameters { get; set; }

        [Option('o', "outputPlugin", Required = true,
            HelpText = "Output Plugin.")]
        public string OutputPlugin { get; set; }

        [Option("op", Required = true,
            HelpText = "Output plugin parameters.")]
        public string OutputPluginParameters { get; set; }

        [Option('v', "verbose",
            HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

    }
}
