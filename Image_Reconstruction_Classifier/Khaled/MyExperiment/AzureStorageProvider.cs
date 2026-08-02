using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using MyCloudProject.Common;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyExperiment
{
    /// <summary>
    /// Implements IStorageProvider against Azure Queue, Blob, and Table Storage.
    /// Queue: triggers experiment runs. Blob: input dataset + output results. Table: experiment result records.
    /// </summary>
    public class AzureStorageProvider : IStorageProvider
    {
        private readonly MyConfig _config;
        private readonly QueueClient _queueClient;
        private readonly BlobContainerClient _trainingContainer;
        private readonly BlobContainerClient _resultContainer;
        private readonly TableClient _tableClient;
        private readonly string _localWorkFolder;

        public AzureStorageProvider(IConfigurationSection configSection)
        {
            _config = new MyConfig();
            configSection.Bind(_config);

            _queueClient = new QueueClient(_config.StorageConnectionString, _config.Queue);
            _queueClient.CreateIfNotExists();

            _trainingContainer = new BlobContainerClient(_config.StorageConnectionString, _config.TrainingContainer);
            _trainingContainer.CreateIfNotExists();

            _resultContainer = new BlobContainerClient(_config.StorageConnectionString, _config.ResultContainer);
            _resultContainer.CreateIfNotExists();

            _tableClient = new TableClient(_config.StorageConnectionString, _config.ResultTable);
            _tableClient.CreateIfNotExists();

            _localWorkFolder = Path.Combine(Path.GetTempPath(), "ImageReconExperiment");
            Directory.CreateDirectory(_localWorkFolder);
        }

        /// <summary>
        /// Polls the queue for the next message. Returns null if the queue is empty.
        /// Note: this method is synchronous by contract (see IStorageProvider), so we use
        /// the SDK's blocking ReceiveMessage rather than ReceiveMessageAsync.
        /// </summary>
        public IExerimentRequest ReceiveExperimentRequestAsync(CancellationToken token)
        {
            QueueMessage message = _queueClient.ReceiveMessage(visibilityTimeout: TimeSpan.FromMinutes(30), cancellationToken: token);

            if (message == null)
                return null;

            // Expected message body (JSON): { "ExperimentId": "...", "InputFile": "sample_dataset.zip", "Name": "...", "Description": "..." }
            ExerimentRequestMessage request = JsonSerializer.Deserialize<ExerimentRequestMessage>(
                message.MessageText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
                return null;

            // MessageId/MessageReceipt are needed later to delete the message (CommitRequestAsync).
            request.MessageId = message.MessageId;
            request.MessageReceipt = message.PopReceipt;

            return request;
        }

        /// <summary>
        /// Downloads the named blob from the training container to a local temp file.
        /// </summary>
        public async Task<string> DownloadInputAsync(string fileName)
        {
            var blobClient = _trainingContainer.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                throw new FileNotFoundException($"Blob '{fileName}' not found in container '{_config.TrainingContainer}'.");

            string localPath = Path.Combine(_localWorkFolder, fileName);
            await blobClient.DownloadToAsync(localPath);

            return localPath;
        }

        /// <summary>
        /// Uploads the experiment's output files to the result container and writes the
        /// result record into table storage.
        /// </summary>
        public async Task UploadResultAsync(string experimentName, IExperimentResult result)
        {
            // We need OutputFiles/Accuracy (concrete fields) and the ITableEntity keys,
            // so the concrete ExperimentResult type is required here, not just the interface.
            if (result is not ExperimentResult concreteResult)
                throw new InvalidOperationException("UploadResultAsync requires a concrete ExperimentResult instance.");

            if (concreteResult.OutputFiles != null)
            {
                foreach (var localFilePath in concreteResult.OutputFiles)
                {
                    if (!File.Exists(localFilePath))
                        continue;

                    string blobName = $"{experimentName}/{Path.GetFileName(localFilePath)}";
                    var blobClient = _resultContainer.GetBlobClient(blobName);
                    await blobClient.UploadAsync(localFilePath, overwrite: true);
                }
            }

            await _tableClient.UpsertEntityAsync(concreteResult);
        }

        /// <summary>
        /// Deletes the processed message from the queue so it isn't picked up again.
        /// </summary>
        public async Task CommitRequestAsync(IExerimentRequest request)
        {
            await _queueClient.DeleteMessageAsync(request.MessageId, request.MessageReceipt);
        }
    }
}