using CommandLine;
using CP.Lab.Transfer.Common;
using CP.Lab.Transfer.Common.Exceptions;
using CP.Lab.Transfer.IoC;
using Microsoft.Practices.Unity.Configuration;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using Unity;
using Unity.log4net;

namespace CP.Lab.Transfer.App
{
    public class Program
    {
        private static string _transferManager;
        private static string _transferManagerParameters;
        private static string _inputPlugin;
        private static string _inputPluginParameters;
        private static string _outputPlugin;
        private static string _outputPluginParameters;


        private static string GetHelp()
        {
            string helpMessage = "You didn't provide all or invalid required parameters. Manager and plugins need to have following format:" +
         " Transfer manager: -t e.g.\"DEFAULT_TRANSFER_MANAGER\" , Input plugin: -i e.g.\"CSV_INPUT_PLUGIN\", Output plugin: -o e.g. \"SQLSERVER_OUTPUT_PLUGIN\"\n" +
         "Now you can choose between following managers and plugins: DEFAULT_TRANSFER_MANAGER. CSV_INPUT_PLUGIN,CSV_OUTPUT_PLUGIN, SQLSERVER_INPUT_PLUGIN,SQLSERVER_OUTPUT_PLUGIN";
            return helpMessage;
        }

        private static void CheckIfRegistered(UnityContainer unityContainer)
        {
            log4net.Config.XmlConfigurator.Configure();
            var log = log4net.LogManager.GetLogger(typeof(Program));

            log.Info("Checking if manager and plugins are registered");
            if (!unityContainer.IsRegistered<ITransferManager>(_transferManager))
            {
                log.Error("Given transfer manager isn't registered");
                log.Info(GetHelp());
                throw new InvalidPluginException("Given Transfer Manager isn't registered.");
            }
            if (!unityContainer.IsRegistered<IInputPlugin>(_inputPlugin))
            {
                log.Error("Given Input Plugin isn't registered");
                log.Info(GetHelp());
                throw new InvalidPluginException("Given Input Plugin isn't registered."); 
            }
            if(!unityContainer.IsRegistered<IOutputPlugin>(_outputPlugin))
            {
                log.Error("Given Output Plugin isn't registered");
                log.Info(GetHelp());
                throw new InvalidPluginException("Given Output Plugin isn't registered.");
            }
            log.Info("Manager and plugins are registered");
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Initializing log");
            log4net.Config.XmlConfigurator.Configure();
            var log = log4net.LogManager.GetLogger(typeof(Program));
            log.InfoFormat("Log initialized");

            log.InfoFormat("Initializing IoC container");
            using (UnityContainer container = new UnityContainer())
            {
                log.InfoFormat("Loading IoC container configuration");
                container.LoadConfiguration();
                container.AddNewExtension<Log4NetExtension>();
                DependencyResolver.Container = container;
                log.InfoFormat("IoC container initialized");

                log.InfoFormat("Checking input arguments");
                Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       Program._transferManager = o.TransferManager;
                       _transferManagerParameters = o.TransferManagerParameter;
                       _inputPlugin = o.InputPlugin;
                       _inputPluginParameters = o.InputPluginParameters;
                       _outputPlugin = o.OutputPlugin;
                       _outputPluginParameters = o.OutputPluginParameters;

                       if (o.Verbose)
                       {
                           Console.WriteLine($"Current Arguments: -t {o.TransferManager} -tp {o.TransferManagerParameter} -i {o.InputPlugin} -ip {o.InputPluginParameters} " +
                               $"-o {o.OutputPlugin} -op {o.OutputPluginParameters}");
                           Console.WriteLine();
                       }
                       else
                       { }
                   });

                try
                {
                    CheckParameters();
                    CheckIfRegistered(container);
                    log.Info("Plugins and manager initializing started");
                    using (var inputPluginInstance = container.Resolve<IInputPlugin>(_inputPlugin))
                    {
                        try
                        {
                            inputPluginInstance.Init(_inputPluginParameters);
                        }
                        catch(Exception)
                        {
                            //log.Erro
                            log.Info(inputPluginInstance.GetHelp());
                            throw new InputParametersNotFoundException();
                        }

                        using (var outputPluginInstance = container.Resolve<IOutputPlugin>(_outputPlugin))
                        {
                            try
                            {
                                outputPluginInstance.Init(_outputPluginParameters);
                            }
                            catch(Exception)
                            {
                                log.Info(outputPluginInstance.GetHelp());
                                throw new InputParametersNotFoundException();
                            }

                            using (ITransferManager transferManagerInstance = container.Resolve<ITransferManager>(_transferManager))
                            {
                                try
                                {
                                    transferManagerInstance.Init(_transferManagerParameters);
                                }
                                catch(Exception)
                                {
                                    log.Info(transferManagerInstance.GetHelp());
                                    throw new InputParametersNotFoundException();
                                }
                                transferManagerInstance.Transfer(inputPluginInstance, outputPluginInstance);
                            }
                        }
                    }
                }
                catch (InputParametersNotFoundException e)
                {
                    log.Error(e.Message); 
                }
                catch (FilePathException e)
                {
                    log.Error(e.Message);
                }
                catch (SqlException e)
                {
                    log.Error(e.Message);
                }
                catch (InvalidConnectionStringException e)
                {
                    log.Error(e.Message);
                }
                catch (InvalidPluginException e)
                {
                    log.Error(e.Message);
                }
                catch (ArgumentNullException e)
                {
                    log.Error(e.Message);
                }
                catch (ArgumentException e)
                {
                    log.Error(e.Message);
                }
                catch(Exception e)
                {
                    log.Error(e.Message);
                }

                log.InfoFormat("Press any key to exit");
                Console.ReadKey();
            }
        }

        private static void CheckParameters()
        {
            log4net.Config.XmlConfigurator.Configure();
            var log = log4net.LogManager.GetLogger(typeof(Program));

            log.Info("Checking if manager and plugins name aren't null");
            if (String.IsNullOrEmpty(_transferManager))
            {
                log.Error("Transfer manager is null");
                log.Info("Transfer manager is null \n" + GetHelp());
                throw new ArgumentNullException(nameof(_transferManager));
            }
            if (String.IsNullOrEmpty(_inputPlugin))
            {
                log.Error("Input plugin is null");
                log.Info("Input plugin is null \n" + GetHelp());
                throw new ArgumentNullException(nameof(_inputPlugin));
            }
            if (String.IsNullOrEmpty(_outputPlugin))
            {
                log.Error("Output plugin is null");
                log.Info("Output plugin is null \n" + GetHelp());
                throw new ArgumentNullException(nameof(_outputPlugin));
            }
            log.Info("Manager and plugins name null checked");
        }
    }
}
