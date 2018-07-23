﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace NoOpsJp.CosmosDbScaler.ThroughputMonitor
{
    public class StreamlinedDocumentClient
    {
        public StreamlinedDocumentClient(Uri serviceEndpoint, string authKeyOrResourceToken, string databaseId, ThroughputMonitor monitor = null)
        {
            _documentClient = new DocumentClient(serviceEndpoint, authKeyOrResourceToken);
            _databaseId = databaseId;
            _monitor = monitor;
        }

        private readonly DocumentClient _documentClient;
        private readonly string _databaseId;
        private readonly ThroughputMonitor _monitor;

        public async Task<T> ReadDocumentAsync<T>(string collectionId, string documentId)
        {
            try
            {
                var response = await _documentClient.ReadDocumentAsync<T>(UriFactory.CreateDocumentUri(_databaseId, collectionId, documentId));

                // TODO: RU を送る
                await _monitor.TrackRequestChargeAsync(response.RequestCharge);

                return response.Document;
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode != null && (int)ex.StatusCode == 429)
                {
                    // TODO: RU を送る with TooMany
                    await _monitor.TrackRequestChargeAsync(ex.RequestCharge);
                    await _monitor.TrackTooManyRequestAsync();
                }

                throw;
            }
        }

        public async Task CreateDocumentAsync<T>(string collectionId, T document)
        {
            try
            {
                var response = await _documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, collectionId), document);
            }
            catch (DocumentClientException ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(IDocumentQuery<T> documentQuery)
        {
            var response = await documentQuery.ExecuteNextAsync<T>();

            //response.RequestCharge;

            return response;
        }
    }
}