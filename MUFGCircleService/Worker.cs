using System;
using System.Threading;
using System.Threading.Tasks;
using MUFGCircleService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace MUFGCircleService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MUFG circle Service started at: {time}", DateTimeOffset.Now);
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MUFG circle Service Stopped at: {time}", DateTimeOffset.Now);
            return base.StopAsync(cancellationToken);
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DateTime eodMUFGCircleServiceStartTime = DateTime.Now;
            _logger.LogInformation("MUFG circle Service Started @ " + eodMUFGCircleServiceStartTime.ToString("dd-MM-yyyy hh:mm:ss"));

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    DateTime triggerTimeMUFG = eodMUFGCircleServiceStartTime.AddMinutes(int.Parse(MUFGTriggerDetails.TradeFileInterval));

                  if (DateTime.Now.ToString("hh:mm:tt") == triggerTimeMUFG.ToString("hh:mm:tt"))
                    {
                        _logger.LogInformation("MUFG Circle Adaptor - get triggered");
                        eodMUFGCircleServiceStartTime = DateTime.Now;
                        try
                        {

                            _logger.LogInformation("Entered into try block, Calling constructor for MUFG circle FX.");
                            new SFTPMUFGFxFiles(_logger);
                            _logger.LogInformation("Completed iteration for FX, Calling constructor for MUFG circle bond");
                            new SFTPMUFGBondFiles(_logger);
                            _logger.LogInformation("Completed iteration for Bonds,  Calling hit for Verification report.");

                            new VerificationMatch(_logger);
                            _logger.LogInformation("Verification report module hits successfully.");
                            _logger.LogInformation("MUFG Circle SFTP Transfer Success and completed the iteration. Waiting for next Hit in minutes=> ." +MUFGTriggerDetails.TradeFileInterval);
                            
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Exception Occur while generating MUFG TradeFiles " + ex.ToString());
                            
                        }
                    }

                }
                await Task.Delay(1000, stoppingToken);
            }
        }



        public bool GetResponse()
        {
            return true;
        }
    }
}

