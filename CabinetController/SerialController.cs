using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Storage.Streams;
using Microsoft.Extensions.DependencyInjection;

namespace ManagementController
{
    class SerialController
    {
        private SerialDevice serialPort = null;
        private DataWriter dataWriter = null;
        private DataReader dataReader = null;
        private bool serialInitialized = false;

        // Thread safety
        private static object readCancelLock = new object();
        private static object writeCancelLock = new object();

        private static object writeLock = new object();
        private static bool isWriting = false;

        private CancellationTokenSource readTokenSource;
        private CancellationTokenSource writeTokenSource;
        private static uint ReadBufferLength = 1024;

        public event SerialRecievedDelegate SerialRecieved;
        public delegate Task SerialRecievedDelegate(string text);

        public event SerialInitializedDelegate SerialInitialized;
        public delegate Task SerialInitializedDelegate();

        private IServiceProvider _serviceProvider;
        private ILogger<SerialController> logger;

        public bool CommandInProgress = false;

        public SerialController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            ILoggerFactory factory = serviceProvider.GetService<ILoggerFactory>();
            logger = factory.CreateLogger<SerialController>();
        }

        /// <summary>
        /// Attempts to initialize the serial port,
        /// and communications to the controller.
        /// </summary>
        /// <returns>The async Task.</returns>
        public async Task InitializeSerialAsync() {
            // Clear old values
            serialPort = null;
            serialInitialized = false;

            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector());
            foreach (DeviceInformation info in devices)
            {
                SerialDevice serialPortTemp = await SerialDevice.FromIdAsync(info.Id);
                if (serialPortTemp != null && !serialPortTemp.PortName.Contains("UART"))
                    serialPort = serialPortTemp;
            }

            if (serialPort == null)
                throw new Exception("Could not open the serial device. The device was invalid or not found.");

            serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.BaudRate = 9600;
            serialPort.Parity = SerialParity.None;
            serialPort.StopBits = SerialStopBitCount.One;
            serialPort.DataBits = 8;
            serialPort.Handshake = SerialHandshake.None;

            readTokenSource = new CancellationTokenSource();
            writeTokenSource = new CancellationTokenSource();

            serialInitialized = true;
            await Task.Run(async () => { await SerialInitialized.Invoke(); });
        }

        /// <summary>
        /// Attempts to recover the serial interface 
        /// after an initialization failure.
        /// </summary>
        public async Task RecoverSerialAsync() {
            while (!serialInitialized)
            {
                try
                {
                    logger.LogDebug("Attempting to recover the serial interface.");
                    await InitializeSerialAsync();
                }
                catch (Exception e)
                {
                    logger.LogDebug("Serial recovery failed due to an exception. Waiting for 5000ms.", e);
                    await Task.Delay(5000);
                }
            }
        }

        private void CloseSerial()
        {
            if (serialPort != null)
                serialPort.Dispose();

            serialPort = null;
            serialInitialized = false;
        }

        /// <summary>
        /// Starts listening on the serial port.
        /// Returns immediately, and will fire events.
        /// </summary>
        public async Task StartListeningAsync()
        {
            if (!serialInitialized)
                return;

            try
            {
                dataReader = new DataReader(serialPort.InputStream);

                while (true)
                {
                    string readString = await ReadStringAsync(readTokenSource.Token);

                    if (!String.IsNullOrEmpty(readString))
                    {
                        await SerialRecieved.Invoke(readString);
                        logger.LogDebug("Got string from serial: " + readString);
                    }
                }
            }
            catch (Exception e)
            {
                CloseSerial();
                logger.LogDebug("The system caught a critical exception while reading from the serial port. The port will now close and attempt recovery.", e);
                await Task.Run(RecoverSerialAsync);
            }
            finally
            {
                if (dataReader != null)
                {
                    dataReader.DetachStream();
                    dataReader = null;
                }
            }
        }

        /// <summary>
        /// Stops the application listening on the serial port.
        /// </summary>
        public void StopListening() {
            if (readTokenSource == null || readTokenSource.IsCancellationRequested)
                return;

            readTokenSource.Cancel();
        }

        /// <summary>
        /// Attempts to write a string to serial.
        /// </summary>
        public Task TryWriteString(string text)
        {
            lock(writeLock)
            {
                try
                {
                    while (isWriting || CommandInProgress)
                    {
                        writeTokenSource.Token.ThrowIfCancellationRequested();
                    }

                    isWriting = true;

                    dataWriter = new DataWriter(serialPort.OutputStream);
                    WriteString(text);
                }
                catch (Exception e)
                {
                    CloseSerial();
                    logger.LogDebug("The system caught a critical exception while writing to the serial port. The port will now close.", e);
                    Task.Run(RecoverSerialAsync);
                }
                finally
                {
                    dataWriter.DetachStream();
                    dataWriter = null;
                    isWriting = false;

                    logger.LogDebug("Wrote output to serial: " + text);
                }
            }

            return Task.CompletedTask;
        }

        private void WriteString(string text)
        {
            CancellationToken cancellationToken = writeTokenSource.Token;

            if (String.IsNullOrEmpty(text))
                throw new Exception("Invalid or malformed input.");

            Task<UInt32> taskWrite;

            char[] buffer = new char[text.Length];
            text.CopyTo(0, buffer, 0, text.Length);

            String inputString = new string(buffer);
            dataWriter.WriteString(inputString);

            lock (writeCancelLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                taskWrite = dataWriter.StoreAsync().AsTask(cancellationToken);
            }

            taskWrite.Wait();
        }

        private async Task<string> ReadStringAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> taskLoad;

            lock(readCancelLock)
            {
                cancellationToken.ThrowIfCancellationRequested();

                dataReader.InputStreamOptions = InputStreamOptions.Partial;
                taskLoad = dataReader.LoadAsync(ReadBufferLength).AsTask(cancellationToken);
            }

            UInt32 bytes = await taskLoad;
            cancellationToken.ThrowIfCancellationRequested();

            if (bytes > 0)
                return dataReader.ReadString(bytes);

            return null;
        }

        private void CancelReadString()
        {
            lock (readCancelLock)
            {
                if (readTokenSource == null || readTokenSource.IsCancellationRequested)
                    return;

                readTokenSource.Cancel();
            }
        }

        private void CancelWriteString()
        {
            lock (writeCancelLock)
            {
                if (writeTokenSource == null || writeTokenSource.IsCancellationRequested)
                    return;

                writeTokenSource.Cancel();
            }
        }

        public bool GetSerialInitialized() { return serialInitialized; }
    }
}
